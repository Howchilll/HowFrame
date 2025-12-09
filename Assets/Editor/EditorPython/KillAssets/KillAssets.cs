using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LitJson;

public static class KillAssets
{
    [MenuItem("Tools/PyFunctions/KillAssets")]
    public static void Run()
    {
        KillAssetsParameterWindow.ShowWindow();
    }

    public static void Execute(string aimFolder, string killList, string kignore)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "KillAssets", "kill_assets.py");
        scriptPath = Path.GetFullPath(scriptPath);

        try
        {
            PyCaller pyCaller = new PyCaller();

            string[] args = new string[]
            {
                aimFolder,
                killList,
                kignore,
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

public class KillAssetsParameterWindow : EditorWindow
{
    private string aimFolder = "";
    private string killList = "";
    private string kignore = "";

    public static void ShowWindow()
    {
        KillAssetsParameterWindow window = GetWindow<KillAssetsParameterWindow>("KillAssets 参数");
        window.Show();
    }

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "KillAssets", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var paramDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonContent);
                if (paramDict != null)
                {
                    if (paramDict.ContainsKey("aimFolder"))
                        aimFolder = paramDict["aimFolder"];
                    if (paramDict.ContainsKey("killList"))
                        killList = paramDict["killList"];
                    if (paramDict.ContainsKey("kignore"))
                        kignore = paramDict["kignore"];
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
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "KillAssets", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["aimFolder"] = aimFolder;
            paramDict["killList"] = killList;
            paramDict["kignore"] = kignore;
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
        GUILayout.Label("KillAssets 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        aimFolder = EditorGUILayout.TextField("aimFolder", aimFolder);
        killList = EditorGUILayout.TextField("killList", killList);
        kignore = EditorGUILayout.TextField("kignore", kignore);
        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            SaveParameters();
            KillAssets.Execute(aimFolder, killList, kignore);
            Close();
        }
    }
}
