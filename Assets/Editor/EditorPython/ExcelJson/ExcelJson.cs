using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using LitJson;

public static class ExcelJson
{
    private static SynchronizationContext _mainThreadContext;

    [MenuItem("Tools/PyFunctions/ExcelJson")]
    public static void Run()
    {
        // 捕获主线程上下文
        _mainThreadContext = SynchronizationContext.Current;
        ExcelJsonParameterWindow.ShowWindow();
    }

public static void Execute(string excelFolder, string jsonFolder, string conversionMode)
{
    // 确保有主线程上下文
    if (_mainThreadContext == null)
    {
        _mainThreadContext = SynchronizationContext.Current;
    }

    string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ExcelJson", "excel_json.py");
    scriptPath = Path.GetFullPath(scriptPath);

    try
    {
        PyCaller pyCaller = new PyCaller();
        excelFolder=ResolveDir(excelFolder);
        jsonFolder = ResolveDir(jsonFolder);
        string[] args = new string[]
        {
            excelFolder,
            jsonFolder,
            conversionMode,
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
            Debug.Log("ExcelJson Python 脚本执行成功！");
        }
        else
        {
            Debug.LogWarning($"ExcelJson Python 脚本执行完成，但退出码不为 0: {exitCode}");
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

public class ExcelJsonParameterWindow : EditorWindow
{
    private string excelFolder = "EditorPath.ConfigExcelPath";
    private string jsonFolder = "EditorPath.ConfigJsonPath";
    private int conversionModeIndex = 0;
    private readonly string[] conversionModes = { "Excel → JSON", "JSON → Excel", "双向转换" };

    public static void ShowWindow()
    {
        ExcelJsonParameterWindow window = GetWindow<ExcelJsonParameterWindow>("ExcelJson 参数");
        window.Show();
    }

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ExcelJson", "parameters.json");
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
                    if (paramDict.ContainsKey("jsonFolder"))
                        jsonFolder = paramDict["jsonFolder"];
                    if (paramDict.ContainsKey("conversionMode"))
                    {
                        string mode = paramDict["conversionMode"];
                        for (int i = 0; i < conversionModes.Length; i++)
                        {
                            if (conversionModes[i] == mode)
                            {
                                conversionModeIndex = i;
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
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ExcelJson", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["excelFolder"] = excelFolder;
            paramDict["jsonFolder"] = jsonFolder;
            paramDict["conversionMode"] = conversionModes[conversionModeIndex];
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

        excelFolder = EditorGUILayout.TextField("Excel 文件夹", excelFolder);
        jsonFolder = EditorGUILayout.TextField("JSON 文件夹", jsonFolder);
        conversionModeIndex = EditorGUILayout.Popup("转换方向", conversionModeIndex, conversionModes);

        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            SaveParameters();
            ExcelJson.Execute(excelFolder, jsonFolder, conversionModes[conversionModeIndex]);
            Close();
        }
    }
    

}
