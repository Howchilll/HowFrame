using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LitJson;

public static class ScanAssets
{
    [MenuItem("Tools/PyFunctions/ScanAssets")]
    public static void Run()
    {
        ScanAssetsParameterWindow.ShowWindow();
    }

    public static void Execute(string scenePath, string resourcesPath, string streamingAssetsPath, string addressablePath, string artAssetPath)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanAssets", "scan_assets.py");
        scriptPath = Path.GetFullPath(scriptPath);

        try
        {
            PyCaller pyCaller = new PyCaller();

            string[] args = new string[]
            {
                scenePath,
                resourcesPath,
                streamingAssetsPath,
                addressablePath,
                artAssetPath,
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

public class ScanAssetsParameterWindow : EditorWindow
{
    private string scenePath = "";
    private string resourcesPath = "";
    private string streamingAssetsPath = "";
    private string addressablePath = "";
    private string artAssetPath = "";

    public static void ShowWindow()
    {
        ScanAssetsParameterWindow window = GetWindow<ScanAssetsParameterWindow>("ScanAssets 参数");
        window.Show();
    }

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanAssets", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var paramDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonContent);
                if (paramDict != null)
                {
                    if (paramDict.ContainsKey("scenePath"))
                        scenePath = paramDict["scenePath"];
                    if (paramDict.ContainsKey("resourcesPath"))
                        resourcesPath = paramDict["resourcesPath"];
                    if (paramDict.ContainsKey("streamingAssetsPath"))
                        streamingAssetsPath = paramDict["streamingAssetsPath"];
                    if (paramDict.ContainsKey("addressablePath"))
                        addressablePath = paramDict["addressablePath"];
                    if (paramDict.ContainsKey("artAssetPath"))
                        artAssetPath = paramDict["artAssetPath"];
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
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanAssets", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["scenePath"] = scenePath;
            paramDict["resourcesPath"] = resourcesPath;
            paramDict["streamingAssetsPath"] = streamingAssetsPath;
            paramDict["addressablePath"] = addressablePath;
            paramDict["artAssetPath"] = artAssetPath;
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
        GUILayout.Label("ScanAssets 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        scenePath = EditorGUILayout.TextField("scenePath", scenePath);
        resourcesPath = EditorGUILayout.TextField("resourcesPath", resourcesPath);
        streamingAssetsPath = EditorGUILayout.TextField("streamingAssetsPath", streamingAssetsPath);
        addressablePath = EditorGUILayout.TextField("addressablePath", addressablePath);
        artAssetPath = EditorGUILayout.TextField("artAssetPath", artAssetPath);
        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            SaveParameters();
            ScanAssets.Execute(scenePath, resourcesPath, streamingAssetsPath, addressablePath, artAssetPath);
            Close();
        }
    }
}
