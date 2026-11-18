using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HowFrame 全局编辑器配置：框架所有编辑器工具的路径依赖都集中在这里。
/// 保存在 ProjectSettings/HowFrameSettings.asset（纳入版本管理，团队共享），
/// 不依赖 Assets 下的任何位置，框架以 UPM 包形式引入时同样可用。
/// 入口：Edit > Project Settings > HowFrame，或菜单 Tools/HowFrame/Settings。
///
/// 路径约定：相对路径均相对于工程根目录（Assets 的上一级），也可以填绝对路径。
/// </summary>
[FilePath("ProjectSettings/HowFrameSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public class HowFrameSettings : ScriptableSingleton<HowFrameSettings>
{
    /// <summary>默认值（字段初始值与「恢复默认值」共用这一份）</summary>
    private static class Defaults
    {
        public const string PyToolOutputDir = "Assets/Editor/EditorPython";
        public const string EnumJsonDir = "Assets/Editor/HowEditorConfig/EnumConfig";
        public const string EnumCsDir = "Assets/HowFrame/HowEnum/Enums";
        public const string EnumNamespace = "HowEnum";
        public const string PathJsonDir = "Assets/Editor/HowEditorConfig/PathConfig";
        public const string PathCsDir = "Assets/HowFrame/HowPath/Paths";
        public const string ConfigExcelDir = "GameTable/Config";
        public const string ConfigJsonDir = "Assets/StreamingAssets/Config";
        public const string LanguageExcelDir = "GameTable/Languages";
        public const string LanguageJsonDir = "Assets/StreamingAssets/Languages";
        public const string UIScriptDir = "Assets/Scripts/UI";
        public const string AddressableRootDir = "Assets/GameRes";
        public const string KillAssetTargetDir = "Assets/Art";
        public static List<string> KillAssetEntryDirs() => new List<string> { "Assets/Scenes", "Assets/Resources", "Assets/GameRes" };
        public static List<string> KillAssetIgnoreRules() => new List<string> { "*.mat", "*.meta" };
    }

    // ---------------- Python ----------------
    [Tooltip("Python 可执行文件。留空则自动在 PATH 中查找 python3 / python")]
    public string pythonPath = "";

    [Tooltip("Python 工具模板生成器（PyTemplateGenerator）的输出目录")]
    public string pyToolOutputDir = Defaults.PyToolOutputDir;

    // ---------------- 枚举 ----------------
    public string enumJsonDir = Defaults.EnumJsonDir;
    public string enumCsDir = Defaults.EnumCsDir;
    public string enumNamespace = Defaults.EnumNamespace;
    public bool enumGenerateConvert = true;
    public bool enumGenerateGetAll = true;

    // ---------------- 路径常量 ----------------
    public string pathJsonDir = Defaults.PathJsonDir;
    public string pathCsDir = Defaults.PathCsDir;

    // ---------------- 配置表 ----------------
    public string configExcelDir = Defaults.ConfigExcelDir;
    public string configJsonDir = Defaults.ConfigJsonDir;

    // ---------------- 多语言 ----------------
    public string languageExcelDir = Defaults.LanguageExcelDir;
    public string languageJsonDir = Defaults.LanguageJsonDir;

    // ---------------- UI ----------------
    [Tooltip("AutoUI / UIAutoBind 生成与查找 Panel 脚本的根目录")]
    public string uiScriptDir = Defaults.UIScriptDir;

    [Tooltip("AutoUI 模板目录。留空则使用框架自带模板")]
    public string autoUITemplateDir = "";

    // ---------------- Addressables ----------------
    [Tooltip("该目录下的资源会被自动加入 Addressables，按一级子目录分组")]
    public string addressableRootDir = Defaults.AddressableRootDir;

    // ---------------- 启动场景 ----------------
    [Tooltip("「从启动场景运行」使用的场景（放 GameInit 的场景）。留空则使用 Build Settings 中第一个启用的场景")]
    public string bootScene = "";

    // ---------------- KillAsset ----------------
    [Tooltip("依赖扫描的起点目录；为空则扫描整个 Assets")]
    public List<string> killAssetEntryDirs = Defaults.KillAssetEntryDirs();

    [Tooltip("要清理的目标目录：只会删除这里面未被引用的资源")]
    public string killAssetTargetDir = Defaults.KillAssetTargetDir;

    [Tooltip("忽略规则，支持通配符，如 *.mat、Textures/Common/*")]
    public List<string> killAssetIgnoreRules = Defaults.KillAssetIgnoreRules();

    // ====================================================================

    public static HowFrameSettings Settings => instance;

    public void Save() => Save(true);

    /// <summary>
    /// 恢复默认值并保存。（ScriptableSingleton 不能再 CreateInstance 第二个实例来拷贝默认值，所以显式赋值）
    /// </summary>
    public void ResetToDefaults()
    {
        pythonPath = "";
        pyToolOutputDir = Defaults.PyToolOutputDir;
        enumJsonDir = Defaults.EnumJsonDir;
        enumCsDir = Defaults.EnumCsDir;
        enumNamespace = Defaults.EnumNamespace;
        enumGenerateConvert = true;
        enumGenerateGetAll = true;
        pathJsonDir = Defaults.PathJsonDir;
        pathCsDir = Defaults.PathCsDir;
        configExcelDir = Defaults.ConfigExcelDir;
        configJsonDir = Defaults.ConfigJsonDir;
        languageExcelDir = Defaults.LanguageExcelDir;
        languageJsonDir = Defaults.LanguageJsonDir;
        uiScriptDir = Defaults.UIScriptDir;
        autoUITemplateDir = "";
        addressableRootDir = Defaults.AddressableRootDir;
        bootScene = "";
        killAssetEntryDirs = Defaults.KillAssetEntryDirs();
        killAssetTargetDir = Defaults.KillAssetTargetDir;
        killAssetIgnoreRules = Defaults.KillAssetIgnoreRules();
        Save();
    }

    /// <summary>工程根目录（Assets 的上一级）</summary>
    public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    /// <summary>把配置里的路径（相对工程根目录或绝对路径）转成绝对路径</summary>
    public static string ToFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return ProjectRoot;
        path = path.Trim();
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        // Packages/<name>/... 可能实际位于 Library/PackageCache，交给 Unity 的 Path.GetFullPath 解析虚拟包路径
        if (path.StartsWith("Packages/", StringComparison.Ordinal) || path.StartsWith("Packages\\", StringComparison.Ordinal))
            return Path.GetFullPath(path);
        return Path.GetFullPath(Path.Combine(ProjectRoot, path));
    }

    /// <summary>把绝对路径转回相对工程根目录的路径（不在工程内则原样返回），统一用 /</summary>
    public static string ToProjectRelative(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return fullPath;
        string root = ProjectRoot.Replace('\\', '/').TrimEnd('/') + "/";
        string p = Path.GetFullPath(fullPath).Replace('\\', '/');
        return p.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? p.Substring(root.Length) : p;
    }

    // ---------------- 框架自身文件定位 ----------------

    /// <summary>
    /// 按脚本文件名定位其所在目录（返回 Assets/... 或 Packages/... 形式）。
    /// 框架内置资源（模板、Python 脚本）都靠它定位，因此框架放在 Assets 还是 Packages 都能找到。
    /// </summary>
    public static string GetScriptDir(string scriptName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{scriptName} t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == scriptName)
                return Path.GetDirectoryName(path)?.Replace('\\', '/');
        }
        Debug.LogError($"[HowFrame] 找不到脚本 {scriptName}.cs 的位置");
        return null;
    }

    /// <summary>框架内置 EditorPython 目录（PyCaller.cs 所在目录）</summary>
    public static string BuiltinPythonDir => GetScriptDir("PyCaller");

    /// <summary>某个内置 Python 工具脚本的绝对路径，如 BuiltinPythonScript("ExcelJson", "excel_json.py")</summary>
    public static string BuiltinPythonScript(string toolFolder, string scriptFile)
        => ToFullPath(Path.Combine(BuiltinPythonDir ?? "", toolFolder, scriptFile));

    /// <summary>AutoUI 模板目录：优先用配置，留空则用框架自带</summary>
    public string ResolvedAutoUITemplateDir =>
        string.IsNullOrWhiteSpace(autoUITemplateDir) ? GetScriptDir("AutoUIWindow") : autoUITemplateDir;

    // ---------------- Python 解释器 ----------------

    /// <summary>解析 Python 可执行文件：优先用配置，留空则在 PATH 中查找</summary>
    public string ResolvePythonExecutable()
    {
        if (!string.IsNullOrWhiteSpace(pythonPath))
            return pythonPath.Trim();

        bool windows = Application.platform == RuntimePlatform.WindowsEditor;
        string[] names = windows ? new[] { "python.exe", "python3.exe" } : new[] { "python3", "python" };
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator)
            .Where(d => !string.IsNullOrWhiteSpace(d));

        foreach (string dir in dirs)
        {
            // 跳过 Windows 应用商店的 python 占位程序（运行会打开商店而不是 Python）
            if (windows && dir.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            foreach (string name in names)
            {
                string candidate = Path.Combine(dir.Trim(), name);
                if (File.Exists(candidate)) return candidate;
            }
        }

        if (!windows)
        {
            foreach (string candidate in new[] { "/usr/local/bin/python3", "/opt/homebrew/bin/python3", "/usr/bin/python3" })
                if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
