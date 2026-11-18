using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HowEnum;
using HowFrame;
using LitJson;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// HowFrame 健康检查：一次性检查框架约定是否满足（路径、Python、Instance 资源、UI 预设体、语言文件、枚举同步、资源重名）。
/// 检查逻辑与界面分离，便于测试：每个 Check* 返回一组 Issue。
/// </summary>
public static class HowFrameDoctor
{
    public enum Level { Ok, Warning, Error }

    public class Issue
    {
        public Level Level;
        public string Category;
        public string Message;
        /// <summary>可选：点击后 Ping 的资源路径（Assets/...）</summary>
        public string AssetPath;

        public Issue(Level level, string category, string message, string assetPath = null)
        {
            Level = level;
            Category = category;
            Message = message;
            AssetPath = assetPath;
        }

        public override string ToString() => $"[{Level}] {Category}: {Message}";
    }

    // 框架运行时按资源名取用的 Instance 资源：名字 → 是否必需
    internal static readonly (string name, bool required, string usage)[] InstanceAssets =
    {
        ("Canvas", true, "UIManager 创建各层 Canvas"),
        ("HowInputActions", true, "InputAssistant 输入"),
        ("AudioMixer", false, "AudioManager 音量控制"),
        ("EventSystem", false, "UIManager（缺失时自动创建默认 EventSystem）"),
    };

    public static List<Issue> RunAll()
    {
        var issues = new List<Issue>();
        issues.AddRange(CheckSettingsPaths());
        issues.AddRange(CheckPython());

        var entries = CollectAddressableEntries(out var addressableIssue);
        if (addressableIssue != null) issues.Add(addressableIssue);
        else
        {
            issues.AddRange(CheckInstanceAssets(entries));
            issues.AddRange(CheckUIPrefabs(entries, EnumKeysOf<UINameEnum.Tag>(typeof(UINameEnum)).Select(k => k.name)));
            issues.AddRange(CheckDuplicateNames(entries));
        }

        var s = HowFrameSettings.instance;
        issues.AddRange(CheckLanguageFiles(HowFrameSettings.ToFullPath(s.languageJsonDir),
            SafeNames(LangTypeEnum.GetAll), SafeNames(LangModuleEnum.GetAll)));
        issues.AddRange(CheckEnumSync(HowFrameSettings.ToFullPath(s.enumJsonDir), HowFrameSettings.ToFullPath(s.enumCsDir)));
        return issues;
    }

    /// <summary>反射取枚举类里所有 EnumKey 静态字段（不依赖生成时是否勾选了 GetAll）</summary>
    public static List<EnumKey<T>> EnumKeysOf<T>(Type holder)
    {
        return holder.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(EnumKey<T>))
            .Select(f => f.GetValue(null) as EnumKey<T>)
            .Where(k => k != null)
            .ToList();
    }

    /// <summary>
    /// 命令行 / CI 入口：Unity -batchmode -quit -executeMethod HowFrameDoctor.RunFromCommandLine
    /// 有错误时以退出码 1 结束。
    /// </summary>
    public static void RunFromCommandLine()
    {
        var issues = RunAll();
        foreach (var issue in issues) Debug.Log("[HowFrameDoctor] " + issue);
        int errors = issues.Count(i => i.Level == Level.Error);
        Debug.Log($"[HowFrameDoctor] 完成：{errors} 个错误，{issues.Count(i => i.Level == Level.Warning)} 个警告");
        if (Application.isBatchMode) EditorApplication.Exit(errors > 0 ? 1 : 0);
    }

    private static List<string> SafeNames<T>(Func<List<EnumKey<T>>> getAll)
    {
        try { return getAll()?.Select(k => k.name).ToList() ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    #region 路径 / Python

    public static IEnumerable<Issue> CheckSettingsPaths()
    {
        const string cat = "路径设置";
        var s = HowFrameSettings.instance;
        var dirs = new (string label, string path)[]
        {
            ("枚举 JSON", s.enumJsonDir), ("枚举代码", s.enumCsDir),
            ("路径 JSON", s.pathJsonDir), ("路径代码", s.pathCsDir),
            ("配置 Excel", s.configExcelDir), ("配置 JSON", s.configJsonDir),
            ("语言 Excel", s.languageExcelDir), ("语言 JSON", s.languageJsonDir),
            ("UI 脚本", s.uiScriptDir), ("Addressables 根目录", s.addressableRootDir),
        };

        bool allOk = true;
        foreach (var (label, path) in dirs)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                allOk = false;
                yield return new Issue(Level.Warning, cat, $"{label} 未设置");
            }
            else if (!Directory.Exists(HowFrameSettings.ToFullPath(path)))
            {
                allOk = false;
                yield return new Issue(Level.Warning, cat, $"{label} 目录不存在: {path}");
            }
        }
        if (allOk) yield return new Issue(Level.Ok, cat, "所有配置目录都存在");
    }

    public static IEnumerable<Issue> CheckPython()
    {
        const string cat = "Python";
        string python = HowFrameSettings.instance.ResolvePythonExecutable();
        if (string.IsNullOrEmpty(python))
            yield return new Issue(Level.Warning, cat, "没有找到 Python（Excel/语言表的 Python 工具不可用），可在 HowFrame 设置中指定");
        else
            yield return new Issue(Level.Ok, cat, python);
    }

    #endregion

    #region Addressables

    /// <summary>一个可寻址资源：路径、文件名（= 运行时缓存 key）、生效的 Label</summary>
    public readonly struct AddressableItem
    {
        public readonly string AssetPath;
        public readonly string Name;
        public readonly HashSet<string> Labels;

        public AddressableItem(string assetPath, IEnumerable<string> labels)
        {
            AssetPath = assetPath;
            Name = Path.GetFileNameWithoutExtension(assetPath);
            Labels = new HashSet<string>(labels ?? Enumerable.Empty<string>());
        }
    }

    public static List<AddressableItem> CollectAddressableEntries(out Issue error)
    {
        error = null;
        var result = new List<AddressableItem>();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            error = new Issue(Level.Error, "Addressables", "没有 Addressable Settings，请先在 Window > Asset Management > Addressables > Groups 中创建");
            return result;
        }

        var entries = new List<AddressableAssetEntry>();
        settings.GetAllAssets(entries, false); // 会展开文件夹条目
        var seen = new HashSet<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.AssetPath) || AssetDatabase.IsValidFolder(entry.AssetPath)) continue;
            if (!seen.Add(entry.AssetPath)) continue;

            // 文件夹里的资源继承文件夹条目的 Label
            var labels = new HashSet<string>(entry.labels);
            for (var parent = entry.ParentEntry; parent != null; parent = parent.ParentEntry)
                labels.UnionWith(parent.labels);
            result.Add(new AddressableItem(entry.AssetPath, labels));
        }
        return result;
    }

    public static IEnumerable<Issue> CheckInstanceAssets(List<AddressableItem> items)
    {
        const string cat = "Instance 资源";
        foreach (var (name, required, usage) in InstanceAssets)
        {
            var found = items.Where(i => i.Name == name).ToList();
            var tagged = found.FirstOrDefault(i => i.Labels.Contains(HowInit.InstanceLabel));
            if (tagged.AssetPath != null)
                yield return new Issue(Level.Ok, cat, $"{name}（{usage}）", tagged.AssetPath);
            else if (found.Count > 0)
                yield return new Issue(required ? Level.Error : Level.Warning, cat,
                    $"{name} 在 Addressables 中但没有 \"{HowInit.InstanceLabel}\" 标签，框架初始化时不会加载（{usage}）", found[0].AssetPath);
            else
                yield return new Issue(required ? Level.Error : Level.Warning, cat,
                    $"缺少 {name}：需要一个名为 {name} 且带 \"{HowInit.InstanceLabel}\" 标签的可寻址资源（{usage}）");
        }
    }

    public static IEnumerable<Issue> CheckUIPrefabs(List<AddressableItem> items, IEnumerable<string> panelNames)
    {
        const string cat = "UI 预设体";
        var panelTypes = TypeCache.GetTypesDerivedFrom<PanelBase>();
        int ok = 0;
        foreach (var panelName in panelNames)
        {
            var item = items.FirstOrDefault(i => i.Name == panelName && i.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase));
            if (item.AssetPath == null)
            {
                yield return new Issue(Level.Error, cat, $"UINameEnum.{panelName} 没有对应的可寻址预设体（文件名需为 {panelName}.prefab）");
                continue;
            }
            if (item.Labels.Count == 0)
            {
                yield return new Issue(Level.Warning, cat, $"{panelName}.prefab 没有任何 Label，不会被批量加载进缓存，UIManager.Show 时取不到", item.AssetPath);
                continue;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.AssetPath);
            bool hasPanel = prefab != null && prefab.GetComponent<PanelBase>() != null;
            bool hasType = panelTypes.Any(t => t.Name == panelName && !t.IsAbstract);
            if (!hasPanel && !hasType)
            {
                yield return new Issue(Level.Error, cat, $"{panelName}.prefab 上没有 PanelBase，项目里也没有名为 {panelName} 的 PanelBase 子类", item.AssetPath);
                continue;
            }
            ok++;
        }
        if (ok > 0) yield return new Issue(Level.Ok, cat, $"{ok} 个面板预设体正常");
    }

    /// <summary>运行时缓存按资源名存：共享 Label 的同名资源会互相覆盖（只能取到先加载的那个）</summary>
    public static IEnumerable<Issue> CheckDuplicateNames(List<AddressableItem> items)
    {
        const string cat = "资源重名";
        bool any = false;
        foreach (var group in items.Where(i => i.Labels.Count > 0).GroupBy(i => i.Name))
        {
            var list = group.ToList();
            for (int a = 0; a < list.Count; a++)
            for (int b = a + 1; b < list.Count; b++)
            {
                var shared = list[a].Labels.Intersect(list[b].Labels).ToList();
                if (shared.Count == 0) continue;
                any = true;
                yield return new Issue(Level.Warning, cat,
                    $"\"{group.Key}\" 重名（同在 Label {string.Join(",", shared)}）：{list[a].AssetPath} 与 {list[b].AssetPath}，按名字取资源只会拿到其中一个",
                    list[b].AssetPath);
            }
        }
        if (!any) yield return new Issue(Level.Ok, cat, "同一 Label 内没有重名资源");
    }

    #endregion

    #region 语言 / 枚举

    /// <summary>检查每个 语种 × 模块 的语言 JSON 是否存在，以及同一模块下各语种的 key 是否一致</summary>
    public static IEnumerable<Issue> CheckLanguageFiles(string jsonDir, IList<string> languages, IList<string> modules)
    {
        const string cat = "语言文件";
        var issues = new List<Issue>();
        if (languages.Count == 0 || modules.Count == 0)
        {
            issues.Add(new Issue(Level.Warning, cat, "LangTypeEnum / LangModuleEnum 为空"));
            return issues;
        }

        foreach (var module in modules)
        {
            var keysByLang = new Dictionary<string, HashSet<string>>();
            foreach (var lang in languages)
            {
                string file = Path.Combine(jsonDir, $"{lang}_{module}_Lang.json");
                if (!File.Exists(file))
                {
                    issues.Add(new Issue(Level.Error, cat, $"缺少 {lang}_{module}_Lang.json（{jsonDir}）"));
                    continue;
                }
                try
                {
                    var dict = JsonMapper.ToObject<Dictionary<string, string>>(File.ReadAllText(file));
                    keysByLang[lang] = new HashSet<string>(dict?.Keys ?? Enumerable.Empty<string>());
                }
                catch (Exception e)
                {
                    issues.Add(new Issue(Level.Error, cat, $"{lang}_{module}_Lang.json 解析失败: {e.Message}", HowFrameSettings.ToProjectRelative(file)));
                }
            }

            if (keysByLang.Count < 2) continue;
            var all = new HashSet<string>(keysByLang.Values.SelectMany(k => k));
            foreach (var pair in keysByLang)
            {
                var missing = all.Where(k => !pair.Value.Contains(k)).OrderBy(k => k).ToList();
                if (missing.Count == 0) continue;
                string sample = string.Join(", ", missing.Take(5)) + (missing.Count > 5 ? " ..." : "");
                issues.Add(new Issue(Level.Warning, cat, $"{pair.Key}_{module} 比其他语种少 {missing.Count} 个 key: {sample}"));
            }
        }

        if (issues.Count == 0) issues.Add(new Issue(Level.Ok, cat, $"{languages.Count} 个语种 × {modules.Count} 个模块，文件齐全且 key 一致"));
        return issues;
    }

    /// <summary>展开枚举 JSON 的元素：字符串为普通项，{groupName, children} 为分组，生成值为 "组.子项"</summary>
    internal static void FlattenEnumElements(JsonData elements, string prefix, List<string> result)
    {
        if (elements == null || !elements.IsArray) return;
        foreach (JsonData elem in elements)
        {
            if (elem.IsString)
            {
                result.Add(string.IsNullOrEmpty(prefix) ? (string)elem : prefix + "." + (string)elem);
            }
            else if (elem.IsObject && elem.ContainsKey("groupName"))
            {
                string group = (string)elem["groupName"];
                string path = string.IsNullOrEmpty(prefix) ? group : prefix + "." + group;
                if (elem.ContainsKey("children")) FlattenEnumElements(elem["children"], path, result);
            }
        }
    }

    private static readonly Regex EnumElementRegex = new Regex(@"EnumKey<Tag>\s+\w+\s*=\s*new\s*\(\s*""([^""]*)""\s*\)");

    /// <summary>检查枚举 JSON 与生成的 .cs 是否一致（JSON 改了但没重新生成）</summary>
    public static IEnumerable<Issue> CheckEnumSync(string jsonDir, string csDir)
    {
        const string cat = "枚举同步";
        var issues = new List<Issue>();
        if (!Directory.Exists(jsonDir))
        {
            issues.Add(new Issue(Level.Warning, cat, $"枚举 JSON 目录不存在: {jsonDir}"));
            return issues;
        }

        int ok = 0;
        foreach (var file in Directory.GetFiles(jsonDir, "*.json"))
        {
            string name;
            var expected = new List<string>();
            try
            {
                var data = JsonMapper.ToObject(File.ReadAllText(file));
                name = data.ContainsKey("collectionName") ? (string)data["collectionName"] : null;
                if (data.ContainsKey("elements")) FlattenEnumElements(data["elements"], "", expected);
            }
            catch (Exception e)
            {
                issues.Add(new Issue(Level.Error, cat, $"{Path.GetFileName(file)} 解析失败: {e.Message}", HowFrameSettings.ToProjectRelative(file)));
                continue;
            }

            if (string.IsNullOrEmpty(name)) name = Path.GetFileNameWithoutExtension(file);
            string cs = Path.Combine(csDir, name + ".cs");
            if (!File.Exists(cs))
            {
                issues.Add(new Issue(Level.Warning, cat, $"{name}: 有 JSON 但没有生成代码，请在 EnumCreator 中生成", HowFrameSettings.ToProjectRelative(file)));
                continue;
            }

            var actual = EnumElementRegex.Matches(File.ReadAllText(cs)).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            var added = expected.Except(actual).ToList();
            var removed = actual.Except(expected).ToList();
            if (added.Count == 0 && removed.Count == 0)
            {
                ok++;
                continue;
            }

            var parts = new List<string>();
            if (added.Count > 0) parts.Add("代码里缺少 " + string.Join(", ", added));
            if (removed.Count > 0) parts.Add("代码里多出 " + string.Join(", ", removed));
            issues.Add(new Issue(Level.Warning, cat, $"{name} 与 JSON 不一致（{string.Join("；", parts)}），请重新生成", HowFrameSettings.ToProjectRelative(cs)));
        }

        if (ok > 0) issues.Add(new Issue(Level.Ok, cat, $"{ok} 个枚举与 JSON 一致"));
        return issues;
    }

    #endregion
}
