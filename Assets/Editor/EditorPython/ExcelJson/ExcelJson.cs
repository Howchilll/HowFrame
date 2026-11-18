using UnityEditor;
using UnityEngine;

/// <summary>
/// Python 版 Excel ↔ JSON 转换。目录来自 HowFrame 设置（configExcelDir / configJsonDir）。
/// </summary>
public static class ExcelJson
{
    [MenuItem("Tools/PyFunctions/ExcelJson")]
    public static void Run()
    {
        ExcelJsonParameterWindow.ShowWindow();
    }

    public static void Execute(string conversionMode)
    {
        var settings = HowFrameSettings.instance;
        string scriptPath = HowFrameSettings.BuiltinPythonScript("ExcelJson", "excel_json.py");

        try
        {
            new PyCaller().RunPythonScript(scriptPath, new[]
            {
                HowFrameSettings.ToFullPath(settings.configExcelDir),
                HowFrameSettings.ToFullPath(settings.configJsonDir),
                conversionMode,
            }, OnPyDone);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        // 进程回调不在主线程，AssetDatabase 操作需切回主线程
        EditorApplication.delayCall += () =>
        {
            if (exitCode == 0) Debug.Log("ExcelJson Python 脚本执行成功！");
            else Debug.LogWarning($"ExcelJson Python 脚本执行完成，但退出码不为 0: {exitCode}");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        };
    }
}

public class ExcelJsonParameterWindow : EditorWindow
{
    private const string ModePrefsKey = "HowFrame.ExcelJson.Mode";
    private readonly string[] conversionModes = { "Excel → JSON", "JSON → Excel", "双向转换" };
    private int conversionModeIndex;

    public static void ShowWindow()
    {
        GetWindow<ExcelJsonParameterWindow>("ExcelJson 参数").Show();
    }

    private void OnEnable()
    {
        conversionModeIndex = Mathf.Clamp(EditorPrefs.GetInt(ModePrefsKey, 0), 0, conversionModes.Length - 1);
    }

    private void OnGUI()
    {
        GUILayout.Label("ExcelJson 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "功能说明：\n" +
            "1. Excel → JSON：将 Excel 文件夹中的 xlsx 文件转换为 JSON 文件夹中的 json 文件\n" +
            "2. JSON → Excel：将 JSON 文件夹中的 json 文件转换为 Excel 文件夹中的 xlsx 文件\n" +
            "3. 双向转换：同时执行上述两个方向的转换\n" +
            "注意：会自动保持子文件夹结构，序列化逻辑由 Python 脚本完成",
            MessageType.Info
        );
        GUILayout.Space(10);

        HowFrameEditorGUI.PathInfo("Excel 文件夹", HowFrameSettings.instance.configExcelDir);
        HowFrameEditorGUI.PathInfo("JSON 文件夹", HowFrameSettings.instance.configJsonDir);
        HowFrameEditorGUI.SettingsButton();
        conversionModeIndex = EditorGUILayout.Popup("转换方向", conversionModeIndex, conversionModes);

        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            EditorPrefs.SetInt(ModePrefsKey, conversionModeIndex);
            ExcelJson.Execute(conversionModes[conversionModeIndex]);
            Close();
        }
    }
}
