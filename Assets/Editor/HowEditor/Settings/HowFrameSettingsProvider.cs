using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Project Settings > HowFrame 配置页
/// </summary>
public class HowFrameSettingsProvider : SettingsProvider
{
    public const string SettingsPath = "Project/HowFrame";

    private SerializedObject _so;
    private Vector2 _scroll;

    private HowFrameSettingsProvider()
        : base(SettingsPath, SettingsScope.Project, new HashSet<string>
        {
            "HowFrame", "Python", "Enum", "Path", "Excel", "Language", "AutoUI", "Addressable", "KillAsset"
        })
    { }

    [SettingsProvider]
    public static SettingsProvider Create() => new HowFrameSettingsProvider();

    [MenuItem("Tools/HowFrame/Settings", false, 0)]
    public static void Open() => SettingsService.OpenProjectSettings(SettingsPath);

    public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
    {
        var settings = HowFrameSettings.instance;
        // ScriptableSingleton 默认带 NotEditable，去掉才能在面板里编辑
        settings.hideFlags &= ~HideFlags.NotEditable;
        _so = new SerializedObject(settings);
    }

    public override void OnGUI(string searchContext)
    {
        if (_so == null || _so.targetObject == null) OnActivate(searchContext, null);
        _so.Update();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUIUtility.labelWidth = 170;

        EditorGUILayout.HelpBox(
            "HowFrame 所有编辑器工具的路径都在这里统一配置，保存在 ProjectSettings/HowFrameSettings.asset。\n" +
            "相对路径相对于工程根目录（Assets 的上一级），也可以填绝对路径。",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();

        Section("Python");
        PythonField();
        DirField("pyToolOutputDir", "Py 工具生成目录");
        ReadOnlyPath("内置 Python 工具目录", HowFrameSettings.BuiltinPythonDir);

        Section("枚举（EnumCreator）");
        DirField("enumJsonDir", "枚举 JSON 目录");
        DirField("enumCsDir", "枚举 C# 输出目录");
        Prop("enumNamespace", "命名空间");
        Prop("enumGenerateConvert", "生成 Convert 方法");
        Prop("enumGenerateGetAll", "生成 GetAll 方法");

        Section("路径常量（PathCreator）");
        DirField("pathJsonDir", "路径 JSON 目录");
        DirField("pathCsDir", "路径 C# 输出目录");

        Section("配置表（ConfigConverter / ExcelJson）");
        DirField("configExcelDir", "Excel 目录");
        DirField("configJsonDir", "JSON 目录");

        Section("多语言（LanguageConfiger）");
        DirField("languageExcelDir", "语言 Excel 目录");
        DirField("languageJsonDir", "语言 JSON 目录");

        Section("UI（AutoUI / UIAutoBind）");
        DirField("uiScriptDir", "Panel 脚本根目录");
        DirField("autoUITemplateDir", "AutoUI 模板目录", allowEmpty: true);
        if (string.IsNullOrWhiteSpace(HowFrameSettings.instance.autoUITemplateDir))
            ReadOnlyPath("　└ 当前使用内置模板", HowFrameSettings.instance.ResolvedAutoUITemplateDir);

        Section("Addressables");
        DirField("addressableRootDir", "自动收录根目录");

        Section("启动场景（从启动场景运行）");
        BootSceneField();

        Section("KillAsset（未引用资源清理）");
        DirField("killAssetTargetDir", "清理目标目录");
        Prop("killAssetEntryDirs", "依赖扫描起点目录");
        Prop("killAssetIgnoreRules", "忽略规则");

        bool changed = EditorGUI.EndChangeCheck();
        _so.ApplyModifiedPropertiesWithoutUndo();
        if (changed)
        {
            HowFrameSettings.instance.Save();
            HowFramePlayFromBoot.Refresh();
        }

        GUILayout.Space(12);
        if (GUILayout.Button("恢复默认值", GUILayout.Width(120)) &&
            EditorUtility.DisplayDialog("恢复默认值", "确定把 HowFrame 的所有配置恢复为默认值？", "恢复", "取消"))
        {
            HowFrameSettings.instance.ResetToDefaults();
            _so.Update();
        }

        EditorGUILayout.EndScrollView();
    }

    // ---------------- 绘制辅助 ----------------

    private static void Section(string title)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private void Prop(string name, string label)
    {
        EditorGUILayout.PropertyField(_so.FindProperty(name), new GUIContent(label), true);
    }

    private void BootSceneField()
    {
        var prop = _so.FindProperty("bootScene");
        var current = string.IsNullOrEmpty(prop.stringValue) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(prop.stringValue);
        var picked = (SceneAsset)EditorGUILayout.ObjectField(new GUIContent("启动场景", prop.tooltip), current, typeof(SceneAsset), false);
        if (picked != current) prop.stringValue = picked == null ? "" : AssetDatabase.GetAssetPath(picked);
        if (picked == null)
            ReadOnlyPath("　└ 当前使用 Build Settings 第一个场景", HowFramePlayFromBoot.BootScenePath ?? "（Build Settings 中没有启用的场景）");
    }

    private void DirField(string name, string label, bool allowEmpty = false)
    {
        var prop = _so.FindProperty(name);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop, new GUIContent(label, prop.tooltip));

        bool empty = string.IsNullOrWhiteSpace(prop.stringValue);
        bool ok = (empty && allowEmpty) || (!empty && Directory.Exists(HowFrameSettings.ToFullPath(prop.stringValue)));
        StatusIcon(ok, empty ? "" : HowFrameSettings.ToFullPath(prop.stringValue));

        if (GUILayout.Button("选择", GUILayout.Width(44)))
        {
            string start = empty ? HowFrameSettings.ProjectRoot : HowFrameSettings.ToFullPath(prop.stringValue);
            string picked = EditorUtility.OpenFolderPanel(label, start, "");
            if (!string.IsNullOrEmpty(picked))
            {
                prop.stringValue = HowFrameSettings.ToProjectRelative(picked);
                GUI.changed = true;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void PythonField()
    {
        var prop = _so.FindProperty("pythonPath");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop, new GUIContent("Python 可执行文件", prop.tooltip));
        string resolved = HowFrameSettings.instance.ResolvePythonExecutable();
        StatusIcon(!string.IsNullOrEmpty(resolved) && File.Exists(resolved), resolved ?? "未找到 Python");
        if (GUILayout.Button("选择", GUILayout.Width(44)))
        {
            string picked = EditorUtility.OpenFilePanel("选择 Python 可执行文件", "", "");
            if (!string.IsNullOrEmpty(picked))
            {
                prop.stringValue = picked;
                GUI.changed = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (string.IsNullOrWhiteSpace(prop.stringValue))
            ReadOnlyPath("　└ 自动检测到", resolved ?? "（未找到，请手动指定）");
    }

    private static void ReadOnlyPath(string label, string value)
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField(label, value ?? "（未找到）");
    }

    private static void StatusIcon(bool ok, string tooltip)
    {
        var icon = EditorGUIUtility.IconContent(ok ? "TestPassed" : "console.warnicon.sml");
        icon.tooltip = ok ? tooltip : $"目录不存在：{tooltip}";
        GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
    }
}
