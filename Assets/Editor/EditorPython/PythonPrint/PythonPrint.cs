using System.IO;
using UnityEditor;
using UnityEngine;

public static class PythonPrint
{
    [MenuItem("Tools/PyFunctions/PythonPrint")]
    public static void Run()
    {
        PythonPrintParameterWindow.ShowWindow();
    }

    public static void Execute(string name)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "PythonPrint", "python_print.py");
        scriptPath = Path.GetFullPath(scriptPath);

        try
        {
            PyCaller pyCaller = new PyCaller();

            string[] args = new string[]
            {
                name,
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

public class PythonPrintParameterWindow : EditorWindow
{
    public static void ShowWindow()
    {
        PythonPrintParameterWindow window = GetWindow<PythonPrintParameterWindow>("PythonPrint 参数");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("PythonPrint 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        name = EditorGUILayout.TextField("name", name);
        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            PythonPrint.Execute(name);
            Close();
        }
    }
}
