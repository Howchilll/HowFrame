using UnityEditor;
using UnityEngine;

/// <summary>
/// Python 版多语言配置器。目录来自 HowFrame 设置（languageExcelDir / languageJsonDir）。
/// </summary>
public static class LanguageConfigerPy
{
    [MenuItem("Tools/PyFunctions/Language Configer")]
    public static void Run()
    {
        LanguageConfigerParameterWindow.ShowWindow();
    }

    public static void Execute(string operationMode)
    {
        var settings = HowFrameSettings.instance;
        string scriptPath = HowFrameSettings.BuiltinPythonScript("LanguageConfiger", "language_configer.py");

        try
        {
            new PyCaller().RunPythonScript(scriptPath, new[]
            {
                HowFrameSettings.ToFullPath(settings.languageExcelDir),
                HowFrameSettings.ToFullPath(settings.languageJsonDir),
                operationMode,
            }, OnPyDone);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 语言配置脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        // 进程回调不在主线程，AssetDatabase 操作需切回主线程
        EditorApplication.delayCall += () =>
        {
            if (exitCode == 0) Debug.Log("Python 语言配置脚本执行成功！");
            else Debug.LogWarning($"Python 语言配置脚本执行完成，但退出码不为 0: {exitCode}");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        };
    }
}

public class LanguageConfigerParameterWindow : EditorWindow
{
    private const string ModePrefsKey = "HowFrame.LanguageConfigerPy.Mode";
    private readonly string[] operationModes = { "扫描并更新 Excel", "转换所有语言配置文件生成 JSON" };
    private int operationModeIndex;

    public static void ShowWindow()
    {
        GetWindow<LanguageConfigerParameterWindow>("Python 语言配置器").Show();
    }

    private void OnEnable()
    {
        operationModeIndex = Mathf.Clamp(EditorPrefs.GetInt(ModePrefsKey, 0), 0, operationModes.Length - 1);
    }

    private void OnGUI()
    {
        GUILayout.Label("Python 语言配置器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "功能说明：\n" +
            "1. 扫描并更新 Excel：扫描项目并更新 Excel 文件\n" +
            "2. 转换所有语言配置文件生成 JSON：将 Excel 文件转换为 JSON 文件\n" +
            "注意：这是 LanguageConfiger 的 Python 版本实现，序列化逻辑由 Python 脚本完成",
            MessageType.Info
        );
        GUILayout.Space(10);

        HowFrameEditorGUI.PathInfo("Excel 文件夹", HowFrameSettings.instance.languageExcelDir);
        HowFrameEditorGUI.PathInfo("JSON 输出文件夹", HowFrameSettings.instance.languageJsonDir);
        HowFrameEditorGUI.SettingsButton();
        operationModeIndex = EditorGUILayout.Popup("操作模式", operationModeIndex, operationModes);

        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            EditorPrefs.SetInt(ModePrefsKey, operationModeIndex);
            LanguageConfigerPy.Execute(operationModes[operationModeIndex]);
            Close();
        }
    }
}
