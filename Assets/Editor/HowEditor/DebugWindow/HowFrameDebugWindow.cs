using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HowEnum;
using HowFrame;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// HowFrame 运行时调试窗口（Tools/HowFrame/Runtime Debugger），仅 Play 模式下有数据：
/// 初始化状态、语言切换与缺失 key、Addressables 缓存、UI 面板与导航栈、全局事件。
/// </summary>
public class HowFrameDebugWindow : EditorWindow
{
    private enum Tab { 概览, 语言, 资源, UI, 事件 }

    private Tab _tab;
    private Vector2 _scroll;
    private string _search = "";
    private int _langIndex;
    private double _nextRepaint;

    [MenuItem("Tools/HowFrame/Runtime Debugger (运行时调试)", priority = 2)]
    public static void Open()
    {
        var window = GetWindow<HowFrameDebugWindow>("HowFrame Debug");
        window.minSize = new Vector2(420, 300);
    }

    private void OnEnable() => EditorApplication.update += AutoRepaint;
    private void OnDisable() => EditorApplication.update -= AutoRepaint;

    private void AutoRepaint()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _nextRepaint) return;
        _nextRepaint = EditorApplication.timeSinceStartup + 0.5;
        Repaint();
    }

    private void OnGUI()
    {
        _tab = (Tab)GUILayout.Toolbar((int)_tab, Enum.GetNames(typeof(Tab)));
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后显示运行时数据", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        try
        {
            switch (_tab)
            {
                case Tab.概览: DrawOverview(); break;
                case Tab.语言: DrawLanguage(); break;
                case Tab.资源: DrawAssets(); break;
                case Tab.UI: DrawUI(); break;
                case Tab.事件: DrawEvents(); break;
            }
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    #region 概览

    private static void DrawOverview()
    {
        EditorGUILayout.LabelField("HowInit", EditorStyles.boldLabel);
        Row("状态", HowInit.IsReady ? "已就绪" : HowInit.IsStarted ? "初始化中..." : "未开始");
        if (HowInit.FailedModules > 0)
            EditorGUILayout.HelpBox("Wake 失败的模块: " + string.Join(", ", HowInit.FailedModuleNames), MessageType.Error);

        // GameInit 在 Assembly-CSharp 中，编辑器程序集无法直接引用，用反射读取
        var gameInit = FindType("GameInit");
        if (gameInit != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("GameInit", EditorStyles.boldLabel);
            bool ready = GetStatic<bool>(gameInit, "IsReady");
            float progress = GetStatic<float>(gameInit, "Progress");
            Row("状态", ready ? "已就绪" : "加载中...");
            Rect r = EditorGUILayout.GetControlRect(false, 18);
            EditorGUI.ProgressBar(r, progress, $"资源预加载 {progress:P0}");
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("统计", EditorStyles.boldLabel);
        Row("当前语言", LangManager.CurrentLanguage.Value?.name ?? "未加载");
        Row("缺失的语言 key", LangManager.MissingCount.ToString());
        Row("已加载 Label", string.Join(", ", AssetAssistant.LoadedLabels));
        Row("缓存资源数", AssetAssistant.CachedCount.ToString());
        Row("已创建面板", UIManager.Panels().Count().ToString());
        Row("导航栈", UIManager.StackCount == 0 ? "空" : string.Join(" → ", UIManager.StackNames));
        Row("全局事件", EventAssistant.EventDictionary.Count.ToString());
    }

    #endregion

    #region 语言

    private void DrawLanguage()
    {
        var langs = SafeGetAll(LangTypeEnum.GetAll);
        EditorGUILayout.LabelField("切换语言", EditorStyles.boldLabel);
        Row("当前语言", LangManager.CurrentLanguage.Value?.name ?? "未加载");
        if (langs.Count > 0)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _langIndex = Mathf.Clamp(_langIndex, 0, langs.Count - 1);
                _langIndex = EditorGUILayout.Popup(_langIndex, langs.Select(l => l.name).ToArray());
                if (GUILayout.Button("切换", GUILayout.Width(60)))
                    _ = LangManager.SetLanguage(langs[_langIndex]);
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"运行中缺失的 key（{LangManager.MissingCount}）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("GetLangContent 取不到的 key 会记录在这里（切换语言不清空）。" +
                                "「追加到 Excel」会把它们追加到所有语种对应模块的语言表末尾（content 留空，不改动已有行），之后用 Language Configer 填写并重新生成 JSON。",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(LangManager.MissingCount == 0))
            {
                if (GUILayout.Button("追加到 Excel")) ExportMissingKeys(langs);
                if (GUILayout.Button("复制到剪贴板"))
                    EditorGUIUtility.systemCopyBuffer = string.Join("\n",
                        LangManager.MissingKeys.SelectMany(p => p.Value.Select(k => $"{p.Key}\t{k}")));
                if (GUILayout.Button("清空记录")) LangManager.ClearMissingKeys();
            }
        }

        foreach (var pair in LangManager.MissingKeys.OrderBy(p => p.Key))
        {
            EditorGUILayout.LabelField(string.IsNullOrEmpty(pair.Key) ? "(未知模块)" : pair.Key, EditorStyles.miniBoldLabel);
            foreach (var key in pair.Value.OrderBy(k => k))
                EditorGUILayout.SelectableLabel(key, GUILayout.Height(16));
        }
    }

    private static void ExportMissingKeys(List<EnumKey<LangTypeEnum.Tag>> langs)
    {
        string dir = HowFrameSettings.ToFullPath(HowFrameSettings.instance.languageExcelDir);
        var modules = new HashSet<string>(SafeGetAll(LangModuleEnum.GetAll).Select(m => m.name));
        int added = 0, failed = 0;
        var skipped = new List<string>();

        foreach (var pair in LangManager.MissingKeys)
        {
            if (!modules.Contains(pair.Key))
            {
                skipped.Add(string.IsNullOrEmpty(pair.Key) ? "(未知模块)" : pair.Key);
                continue;
            }
            foreach (var lang in langs)
            {
                int n = LanguageExcelSerializer.AppendKeys(Path.Combine(dir, $"{lang.name}_{pair.Key}_Lang.xlsx"), pair.Value);
                if (n < 0) failed++;
                else added += n;
            }
        }

        string msg = $"共追加 {added} 行到 {dir}";
        if (failed > 0) msg += $"\n{failed} 个文件写入失败（可能正被 Excel 打开），详见 Console";
        if (skipped.Count > 0) msg += $"\n跳过不在 LangModuleEnum 中的模块: {string.Join(", ", skipped)}";
        EditorUtility.DisplayDialog("追加缺失 key", msg, "好");
    }

    #endregion

    #region 资源

    private void DrawAssets()
    {
        Row("已加载 Label", string.Join(", ", AssetAssistant.LoadedLabels));
        var loading = AssetAssistant.LoadingLabels.ToList();
        if (loading.Count > 0) Row("加载中", string.Join(", ", loading));
        Row("缓存资源数", AssetAssistant.CachedCount.ToString());

        _search = EditorGUILayout.TextField("搜索", _search);
        EditorGUILayout.Space(2);
        foreach (var (key, asset, labels) in AssetAssistant.CacheEntries().OrderBy(e => e.key))
        {
            if (!string.IsNullOrEmpty(_search) && key.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(key, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField(asset != null ? asset.GetType().Name : "(已销毁)", GUILayout.Width(110));
                EditorGUILayout.LabelField(string.Join(",", labels), GUILayout.Width(120));
                if (asset != null && GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40)))
                    Ping(asset);
            }
        }
    }

    #endregion

    #region UI

    private static void DrawUI()
    {
        EditorGUILayout.LabelField("导航栈（栈底 → 栈顶）", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(UIManager.StackCount == 0 ? "空" : string.Join(" → ", UIManager.StackNames));
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(UIManager.StackCount == 0))
            {
                if (GUILayout.Button("Back")) UIManager.Back();
                if (GUILayout.Button("清空栈")) UIManager.ClearStack();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("已创建面板", EditorStyles.boldLabel);
        foreach (var (name, panel, visible) in UIManager.Panels().OrderBy(p => p.name).ToList())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(name, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField(panel == null ? "(已销毁)" : visible ? "显示" : "隐藏", GUILayout.Width(60));
                using (new EditorGUI.DisabledScope(panel == null))
                {
                    if (GUILayout.Button(visible ? "隐藏" : "显示", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        if (visible) UIManager.Hide(name);
                        else UIManager.Show(name);
                    }
                    if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40))) Ping(panel.gameObject);
                }
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("打开面板（UINameEnum）", EditorStyles.boldLabel);
        foreach (var key in HowFrameDoctor.EnumKeysOf<UINameEnum.Tag>(typeof(UINameEnum)))
        {
            if (UIManager.Check(key.name)) continue;
            if (GUILayout.Button("Show " + key.name, EditorStyles.miniButton)) UIManager.Show(key.name);
        }
    }

    #endregion

    #region 事件

    private void DrawEvents()
    {
        _search = EditorGUILayout.TextField("搜索", _search);
        foreach (var pair in EventAssistant.EventDictionary.OrderBy(p => p.Key).ToList())
        {
            if (!string.IsNullOrEmpty(_search) && pair.Key.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            int count = pair.Value?.GetInvocationList().Length ?? 0;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(pair.Key, GUILayout.MinWidth(140));
                EditorGUILayout.LabelField($"{count} 个订阅", GUILayout.Width(70));
                if (GUILayout.Button("触发", EditorStyles.miniButton, GUILayout.Width(40)))
                    EventAssistant.Invoke(pair.Key);
            }
            if (pair.Value == null) continue;
            foreach (var d in pair.Value.GetInvocationList())
                EditorGUILayout.LabelField("    " + (d.Target != null ? d.Target.GetType().Name : "static") + "." + d.Method.Name, EditorStyles.miniLabel);
        }
    }

    #endregion

    #region 工具

    private static void Row(string label, string value) => EditorGUILayout.LabelField(label, value);

    private static void Ping(Object obj)
    {
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }

    private static List<EnumKey<T>> SafeGetAll<T>(Func<List<EnumKey<T>>> getAll)
    {
        try { return getAll() ?? new List<EnumKey<T>>(); }
        catch { return new List<EnumKey<T>>(); }
    }

    private static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        return null;
    }

    private static T GetStatic<T>(Type type, string property)
    {
        var p = type.GetProperty(property, BindingFlags.Public | BindingFlags.Static);
        return p != null && p.GetValue(null) is T value ? value : default;
    }

    #endregion
}
