#define EDITOR
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using LitJson;

public static class LanguageConfigerPy
{
    private static SynchronizationContext _mainThreadContext;

    [MenuItem("Tools/PyFunctions/Language Configer")]
    public static void Run()
    {
        // 捕获主线程上下文
        _mainThreadContext = SynchronizationContext.Current;
        LanguageConfigerParameterWindow.ShowWindow();
    }

    public static void Execute(string excelFolder, string jsonOutputFolder, string operationMode)
    {
        // 确保有主线程上下文
        if (_mainThreadContext == null)
        {
            _mainThreadContext = SynchronizationContext.Current;
        }

        string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "LanguageConfiger", "language_configer.py");
        scriptPath = Path.GetFullPath(scriptPath);

        try
        {
            PyCaller pyCaller = new PyCaller();

            string[] args = new string[]
            {
                ResolveDir(excelFolder),
                ResolveDir(jsonOutputFolder),
                operationMode,
            };

            pyCaller.RunPythonScript(scriptPath, args, OnPyDone);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 语言配置脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        if (exitCode == 0)
        {
            Debug.Log("Python 语言配置脚本执行成功！");
        }
        else
        {
            Debug.LogWarning($"Python 语言配置脚本执行完成，但退出码不为 0: {exitCode}");
        }

        // 使用SynchronizationContext确保在主线程中执行AssetDatabase操作
        if (_mainThreadContext != null)
        {
            _mainThreadContext.Post(_ =>
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }, null);
        }
        else
        {
            // 降级到delayCall作为备选方案
            UnityEditor.EditorApplication.delayCall += () =>
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            };
        }
    }

    private static string ResolveDir(string dir)
    {
        if (!string.IsNullOrEmpty(dir) && dir.Contains("."))
        {
            string resolved = PathEditor.FindPath(dir);
            if (string.IsNullOrEmpty(resolved))
            {
                Debug.LogError($"目录索引解析失败: {dir}");
                return dir;
            }
            return resolved;
        }
        return dir;
    }
        

}

public class LanguageConfigerParameterWindow : EditorWindow
{
    private string excelFolder = "EditorPath.LanguageExcelPath";
    private string jsonOutputFolder = "EditorPath.LanguageJsonPath";
    private int operationModeIndex = 0;
    private readonly string[] operationModes = { "扫描并更新 Excel", "转换所有语言配置文件生成 JSON" };

    public static void ShowWindow()
    {
        LanguageConfigerParameterWindow window = GetWindow<LanguageConfigerParameterWindow>("Python 语言配置器");
        window.Show();
    }

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "LanguageConfiger", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var paramDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonContent);
                if (paramDict != null)
                {
                    if (paramDict.ContainsKey("excelFolder"))
                        excelFolder = paramDict["excelFolder"];
                    if (paramDict.ContainsKey("jsonOutputFolder"))
                        jsonOutputFolder = paramDict["jsonOutputFolder"];
                    if (paramDict.ContainsKey("operationMode"))
                    {
                        string mode = paramDict["operationMode"];
                        for (int i = 0; i < operationModes.Length; i++)
                        {
                            if (operationModes[i] == mode)
                            {
                                operationModeIndex = i;
                                break;
                            }
                        }
                    }
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
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "LanguageConfiger", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["excelFolder"] = excelFolder;
            paramDict["jsonOutputFolder"] = jsonOutputFolder;
            paramDict["operationMode"] = operationModes[operationModeIndex];
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

        excelFolder = EditorGUILayout.TextField("Excel 文件夹路径", excelFolder);
        jsonOutputFolder = EditorGUILayout.TextField("JSON 输出文件夹", jsonOutputFolder);
        operationModeIndex = EditorGUILayout.Popup("操作模式", operationModeIndex, operationModes);

        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            SaveParameters();
            LanguageConfigerPy.Execute(excelFolder, jsonOutputFolder, operationModes[operationModeIndex]);
            Close();
        }
    }
}
