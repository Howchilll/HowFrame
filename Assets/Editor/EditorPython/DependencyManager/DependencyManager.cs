#define EDITOR
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using LitJson;

public static class DependencyManager
{
    private static SynchronizationContext _mainThreadContext;

    [MenuItem("Tools/PyFunctions/Dependency Manager")]
    public static void Run()
    {
        // 捕获主线程上下文
        _mainThreadContext = SynchronizationContext.Current;
        DependencyManagerParameterWindow.ShowWindow();
    }

    public static void Execute(string pythonPath, bool upgradePackages, bool forceReinstall)
    {
        // 确保有主线程上下文
        if (_mainThreadContext == null)
        {
            _mainThreadContext = SynchronizationContext.Current;
        }

        string scriptPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "DependencyManager", "dependency_manager.py");
        scriptPath = Path.GetFullPath(scriptPath);

        try
        {
            PyCaller pyCaller = new PyCaller();

            // 构建参数
            var args = new List<string>();
            if (!string.IsNullOrEmpty(pythonPath))
            {
                args.Add(pythonPath);
            }
            if (upgradePackages)
            {
                args.Add("--upgrade");
            }
            if (forceReinstall)
            {
                args.Add("--force-reinstall");
            }

            pyCaller.RunPythonScript(scriptPath, args.ToArray(), OnPyDone);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 依赖管理脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        if (exitCode == 0)
        {
            Debug.Log("Python 依赖管理脚本执行成功！");
        }
        else
        {
            Debug.LogWarning($"Python 依赖管理脚本执行完成，但退出码不为 0: {exitCode}");
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

public class DependencyManagerParameterWindow : EditorWindow
{
    private string pythonPath = "";
    private bool upgradePackages = false;
    private bool forceReinstall = false;

    public static void ShowWindow()
    {
        DependencyManagerParameterWindow window = GetWindow<DependencyManagerParameterWindow>("Python 依赖管理");
        window.Show();
    }

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "DependencyManager", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var paramDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonContent);
                if (paramDict != null)
                {
                    if (paramDict.ContainsKey("pythonPath"))
                        pythonPath = paramDict["pythonPath"];
                    if (paramDict.ContainsKey("upgradePackages"))
                        bool.TryParse(paramDict["upgradePackages"], out upgradePackages);
                    if (paramDict.ContainsKey("forceReinstall"))
                        bool.TryParse(paramDict["forceReinstall"], out forceReinstall);
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
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "DependencyManager", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["pythonPath"] = pythonPath;
            paramDict["upgradePackages"] = upgradePackages.ToString();
            paramDict["forceReinstall"] = forceReinstall.ToString();
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
        GUILayout.Label("Python 依赖管理器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "功能说明：\n" +
            "1. 自动扫描所有 EditorPython 子目录中的 requirements.txt 文件\n" +
            "2. 批量安装所需的 Python 包依赖\n" +
            "3. 建议在换电脑或首次使用时先运行此工具\n" +
            "注意：需要确保 Python 环境已正确配置",
            MessageType.Info
        );
        GUILayout.Space(10);

        pythonPath = EditorGUILayout.TextField("Python 路径（可选）", pythonPath);
        upgradePackages = EditorGUILayout.Toggle("升级已安装的包", upgradePackages);
        forceReinstall = EditorGUILayout.Toggle("强制重新安装", forceReinstall);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("将扫描并安装以下依赖：", EditorStyles.boldLabel);

        // 扫描并显示requirements.txt文件
        string editorPythonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython");
        var requirementsFiles = ScanRequirementsFiles(editorPythonPath);

        if (requirementsFiles.Count > 0)
        {
            foreach (var reqFile in requirementsFiles)
            {
                string relativePath = Path.GetRelativePath(editorPythonPath, reqFile);
                string extensionName = Path.GetFileName(Path.GetDirectoryName(reqFile));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{extensionName}:", GUILayout.Width(120));
                EditorGUILayout.LabelField(relativePath);
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("未找到任何 requirements.txt 文件", MessageType.Warning);
        }

        GUILayout.Space(20);
        if (GUILayout.Button("安装依赖", GUILayout.Height(30)))
        {
            if (requirementsFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("警告", "未找到任何 requirements.txt 文件", "确定");
                return;
            }

            SaveParameters();
            DependencyManager.Execute(pythonPath, upgradePackages, forceReinstall);
            Close();
        }
    }

    private List<string> ScanRequirementsFiles(string rootPath)
    {
        var requirementsFiles = new List<string>();

        if (Directory.Exists(rootPath))
        {
            // 扫描所有子目录
            foreach (var dir in Directory.GetDirectories(rootPath))
            {
                string reqFile = Path.Combine(dir, "requirements.txt");
                if (File.Exists(reqFile))
                {
                    requirementsFiles.Add(reqFile);
                }
            }
        }

        return requirementsFiles;
    }
}
