using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScanScripts
{
    [MenuItem("Tools/PyFunctions/ScanScripts")]
    public static void Run()
    {
        ScanScriptsParameterWindow.ShowWindow();
    }

    public static void Execute(string path)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanScripts", "scan_scripts.py");
        scriptPath = Path.GetFullPath(scriptPath);

        try
        {
            PyCaller pyCaller = new PyCaller();

            string[] args = new string[]
            {
                path,
            };

            pyCaller.RunPythonScript(scriptPath, args, OnPyDone);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        if (exitCode == 0)
        {
            Debug.Log("Python 脚本执行成功！");
        }
        else
        {
            Debug.LogWarning($"Python 脚本执行完成，但退出码不为 0: {exitCode}");
        }
    }
}

public class ScanScriptsParameterWindow : EditorWindow
{
    private string path = "";

    public static void ShowWindow()
    {
        ScanScriptsParameterWindow window = GetWindow<ScanScriptsParameterWindow>("ScanScripts 参数");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("ScanScripts 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        path = EditorGUILayout.TextField("path", path);
        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(path))
                path = System.IO.Path.Combine(Application.dataPath, "Scripts");
            
            ScanScripts.Execute(path);
            Close();
        }
    }
}
