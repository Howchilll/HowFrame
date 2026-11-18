using UnityEditor;
using UnityEngine;

/// <summary>
/// 统计指定目录下 C# 脚本数量 / 行数 / 方法数（Python 实现）
/// </summary>
public static class ScanScripts
{
    [MenuItem("Tools/PyFunctions/ScanScripts")]
    public static void Run()
    {
        ScanScriptsParameterWindow.ShowWindow();
    }

    public static void Execute(string path)
    {
        string scriptPath = HowFrameSettings.BuiltinPythonScript("ScanScripts", "scan_scripts.py");

        try
        {
            new PyCaller().RunPythonScript(scriptPath, new[] { HowFrameSettings.ToFullPath(path) }, OnPyDone);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        // 进程回调不在主线程，切回主线程再输出 / 刷新
        EditorApplication.delayCall += () =>
        {
            if (exitCode == 0) Debug.Log("Python 脚本执行成功！");
            else Debug.LogWarning($"Python 脚本执行完成，但退出码不为 0: {exitCode}");
        };
    }
}

public class ScanScriptsParameterWindow : EditorWindow
{
    private const string PathPrefsKey = "HowFrame.ScanScripts.Path";
    private string path = "Assets";

    public static void ShowWindow()
    {
        GetWindow<ScanScriptsParameterWindow>("ScanScripts 参数").Show();
    }

    private void OnEnable()
    {
        path = EditorPrefs.GetString(PathPrefsKey, "Assets");
    }

    private void OnGUI()
    {
        GUILayout.Label("ScanScripts 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        path = EditorGUILayout.TextField("扫描目录", path);
        if (GUILayout.Button("选择", GUILayout.Width(44)))
        {
            string picked = EditorUtility.OpenFolderPanel("选择扫描目录", HowFrameSettings.ToFullPath(path), "");
            if (!string.IsNullOrEmpty(picked)) path = HowFrameSettings.ToProjectRelative(picked);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            EditorPrefs.SetString(PathPrefsKey, path);
            ScanScripts.Execute(path);
            Close();
        }
    }
}
