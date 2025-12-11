using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LitJson;

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
        // 将回调调度到主线程，因为 AssetDatabase.Refresh 只能在主线程调用
        EditorApplication.delayCall += () =>
        {
            if (exitCode == 0)
            {
                Debug.Log("Python 脚本执行成功！");
            }
            else
            {
                Debug.LogWarning($"Python 脚本执行完成，但退出码不为 0: {exitCode}");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        };
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

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanScripts", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var paramDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonContent);
                if (paramDict != null)
                {
                    if (paramDict.ContainsKey("path"))
                        path = paramDict["path"];
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"读取参数配置文件失败: {e.Message}");
            }
        }
    }

    private void SaveParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanScripts", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["path"] = path;
            string jsonContent = JsonMapper.ToJson(paramDict);
            File.WriteAllText(jsonPath, jsonContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存参数配置文件失败: {e.Message}");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("ScanScripts 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        path = EditorGUILayout.TextField("path", path);
        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            SaveParameters();
            ScanScripts.Execute(path);
            Close();
        }
    }
}
