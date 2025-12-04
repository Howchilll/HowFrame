#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using Object = UnityEngine.Object;

public class AutoUIWindow : EditorWindow
{
    private string panelName = "Menu";
    private string scriptOutputPath = "Assets/HowFrameExample";
    private string prefabOutputPath = "Assets/GameRes/UI";
    private Object prefabReference;
    private string defaultPrefabPath = "Assets/Editor/AutoUI/DefaultPanel.prefab";
    
    private Vector2 scrollPos;
    private const float MAX_WAIT_TIME = 10f; // 最大等待时间（秒）
    
    [MenuItem("Tools/Auto UI")]
    public static void ShowWindow()
    {
        var window = GetWindow<AutoUIWindow>("Auto UI");
        window.minSize = new Vector2(400, 300);
        
        // 加载保存的设置
        window.LoadSettings();
    }
    
    private void OnDisable()
    {
        SaveSettings();
    }
    
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        EditorGUILayout.LabelField("UI 自动化生成工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 面板名称
        panelName = EditorGUILayout.TextField("面板名称", panelName);
        if (string.IsNullOrEmpty(panelName))
        {
            EditorGUILayout.HelpBox("请输入面板名称", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        
        // 脚本输出路径
        EditorGUILayout.BeginHorizontal();
        scriptOutputPath = EditorGUILayout.TextField("脚本输出路径", scriptOutputPath);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.SaveFolderPanel("选择脚本输出路径", scriptOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    scriptOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请选择 Assets 文件夹内的路径", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 预制体输出路径
        EditorGUILayout.BeginHorizontal();
        prefabOutputPath = EditorGUILayout.TextField("预制体输出路径", prefabOutputPath);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.SaveFolderPanel("选择预制体输出路径", prefabOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    prefabOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请选择 Assets 文件夹内的路径", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 预制体参考
        prefabReference = EditorGUILayout.ObjectField("预制体参考", prefabReference, typeof(GameObject), false);
        
        EditorGUILayout.Space(5);
        
        // 默认预制体路径（如果参考为空，使用这个或面板名称）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("默认预制体路径", GUILayout.Width(120));
        defaultPrefabPath = EditorGUILayout.TextField(defaultPrefabPath);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择默认预制体", defaultPrefabPath, "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    defaultPrefabPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("如果未选择预制体参考，将使用默认预制体路径或根据面板名称查找", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // 生成按钮
        GUI.enabled = !string.IsNullOrEmpty(panelName);
        if (GUILayout.Button("生成", GUILayout.Height(30)))
        {
            Generate();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndScrollView();
    }
    
    private void Generate()
    {
        try
        {
            // 1. 生成脚本
            string panelGuid = GenerateScripts();
            
            if (string.IsNullOrEmpty(panelGuid))
            {
                EditorUtility.DisplayDialog("错误", "脚本生成失败或无法获取 Panel 脚本的 GUID", "确定");
                return;
            }
            
            // 2. 处理预制体
            ProcessPrefab();
            
            EditorUtility.DisplayDialog("成功", $"面板 {panelName}Panel 生成成功！", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}\n{e.StackTrace}", "确定");
            Debug.LogError($"AutoUI 生成失败: {e}");
        }
    }
    
    private string GenerateScripts()
    {
        string className = panelName + "Panel";
        
        // 创建目录结构：目标目录/xxxPanel/
        string panelDir = Path.Combine(scriptOutputPath, className);
        
        // 确保目录存在
        if (!Directory.Exists(panelDir))
        {
            Directory.CreateDirectory(panelDir);
        }
        
        // 生成主脚本 Panel.cs（只声明类名，不做实现）
        string panelContent = GeneratePanelScript();
        string panelPath = Path.Combine(panelDir, className + ".cs");
        File.WriteAllText(panelPath, panelContent, System.Text.Encoding.UTF8);
        
        // 生成 View 脚本
        string viewContent = GenerateViewScript();
        string viewPath = Path.Combine(panelDir, className + "View.cs");
        File.WriteAllText(viewPath, viewContent, System.Text.Encoding.UTF8);
        
        // 生成 Model 脚本
        string modelContent = GenerateModelScript();
        string modelPath = Path.Combine(panelDir, className + "Model.cs");
        File.WriteAllText(modelPath, modelContent, System.Text.Encoding.UTF8);
        
        // 刷新资源数据库
        AssetDatabase.Refresh();
        
        // 等待 meta 文件生成并获取 Panel 主脚本的 GUID（用于关联预制体）
        return WaitForMetaAndGetGuid(panelPath);
    }
    
    private string GeneratePanelScript()
    {
        string templatePath = "Assets/Editor/AutoUI/Template_Panel.cs.txt";
        return LoadTemplateAndReplace(templatePath);
    }
    
    private string GenerateViewScript()
    {
        string templatePath = "Assets/Editor/AutoUI/Template_View.cs.txt";
        return LoadTemplateAndReplace(templatePath);
    }
    
    private string GenerateModelScript()
    {
        string templatePath = "Assets/Editor/AutoUI/Template_Model.cs.txt";
        return LoadTemplateAndReplace(templatePath);
    }
    
    private string LoadTemplateAndReplace(string templatePath)
    {
        // 将 Assets 路径转换为完整文件系统路径
        string fullPath = Path.Combine(Application.dataPath, "..", templatePath);
        fullPath = Path.GetFullPath(fullPath);
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"模板文件不存在: {templatePath} (完整路径: {fullPath})");
            return $"// 错误：模板文件不存在 {templatePath}";
        }
        
        try
        {
            string templateContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            // 替换占位符 {PANEL_NAME} 为面板名称
            templateContent = templateContent.Replace("{PANEL_NAME}", panelName);
            return templateContent;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取模板文件失败: {e.Message}");
            return $"// 错误：读取模板文件失败 {e.Message}";
        }
    }
    
    private string WaitForMetaAndGetGuid(string scriptPath)
    {
        string metaPath = scriptPath + ".meta";
        float startTime = (float)EditorApplication.timeSinceStartup;
        
        // 强制刷新资源数据库
        AssetDatabase.Refresh();
        
        // 轮询等待 meta 文件生成
        int attempts = 0;
        const int maxAttempts = (int)(MAX_WAIT_TIME * 10); // 每 100ms 检查一次
        
        while (attempts < maxAttempts)
        {
            if (File.Exists(metaPath))
            {
                // 读取 meta 文件获取 GUID
                try
                {
                    string[] lines = File.ReadAllLines(metaPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("guid:"))
                        {
                            string guid = line.Substring("guid:".Length).Trim();
                            Debug.Log($"找到 View 脚本 GUID: {guid}");
                            return guid;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"读取 meta 文件时出错: {e.Message}");
                }
            }
            
            // 每 10 次尝试刷新一次资源数据库
            if (attempts % 10 == 0)
            {
                AssetDatabase.Refresh();
            }
            
            System.Threading.Thread.Sleep(100);
            attempts++;
        }
        
        Debug.LogWarning($"等待 meta 文件超时: {metaPath}");
        return null;
    }
    
    private void ProcessPrefab()
    {
        // 确定源预制体路径
        string sourcePrefabPath = GetSourcePrefabPath();
        
        if (string.IsNullOrEmpty(sourcePrefabPath) || !File.Exists(sourcePrefabPath))
        {
            Debug.LogWarning($"未找到源预制体: {sourcePrefabPath}，跳过预制体处理");
            return;
        }
        
        // 确保输出目录存在
        if (!Directory.Exists(prefabOutputPath))
        {
            Directory.CreateDirectory(prefabOutputPath);
        }
        
        // 目标预制体路径（文件名已经是新名称）
        string targetPrefabPath = Path.Combine(prefabOutputPath, panelName + "Panel.prefab");
        
        // 直接复制文件（文件名本身就已经是新名称）
        File.Copy(sourcePrefabPath, targetPrefabPath, true);
        
        // 刷新资源数据库
        AssetDatabase.Refresh();
    }
    
    private string GetSourcePrefabPath()
    {
        // 1. 如果选择了参考预制体，使用它
        if (prefabReference != null)
        {
            string path = AssetDatabase.GetAssetPath(prefabReference);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
        }
        
        // 2. 如果设置了默认预制体路径，使用它
        if (!string.IsNullOrEmpty(defaultPrefabPath) && File.Exists(defaultPrefabPath))
        {
            return defaultPrefabPath;
        }
        
        // 3. 根据面板名称查找
        string nameBasedPath = $"Assets/Editor/AutoUI/{panelName}Panel.prefab";
        if (File.Exists(nameBasedPath))
        {
            return nameBasedPath;
        }
        
        // 4. 查找默认预制体
        string defaultPath = "Assets/Editor/AutoUI/DefaultPanel.prefab";
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }
        
        return null;
    }
    
    private void RemovePanelBaseAndSave(string sourcePath, string targetPath, string className)
    {
        string content = File.ReadAllText(sourcePath);
        int removedCount = 0;
        HashSet<string> removedComponentIds = new HashSet<string>();
        
        // 1. 移除所有 PanelBase 及其子类的 MonoBehaviour 组件
        // 通过 m_EditorClassIdentifier 识别：Assembly-CSharp::*Panel（包括 MenuPanel, ExamplePanel 等）
        string panelClassPattern = @"(^--- !u!114 &(\d+)\s+MonoBehaviour:.*?m_EditorClassIdentifier: Assembly-CSharp::\w*Panel(\s|$).*?)(?=\n---|\Z)";
        MatchCollection panelMatches = Regex.Matches(content, panelClassPattern, RegexOptions.Singleline | RegexOptions.Multiline);
        
        foreach (Match match in panelMatches)
        {
            string componentId = match.Groups[2].Value;
            if (!removedComponentIds.Contains(componentId))
            {
                content = content.Replace(match.Value, "");
                
                // 从所有 GameObject 的 m_Component 列表中移除该组件引用
                string componentRefPattern = @"^\s+- component: \{fileID: " + Regex.Escape(componentId) + @"\}\s*$";
                content = Regex.Replace(content, componentRefPattern, "", RegexOptions.Multiline);
                removedComponentIds.Add(componentId);
                removedCount++;
            }
        }
        
        // 2. 也移除直接引用 PanelBase GUID 的组件（以防万一）
        string panelBaseGuid = GetPanelBaseGuid();
        if (!string.IsNullOrEmpty(panelBaseGuid))
        {
            string monoBehaviourPattern = @"(^--- !u!114 &(\d+)\s+MonoBehaviour:.*?m_Script: \{fileID: 11500000, guid: " + Regex.Escape(panelBaseGuid) + @", type: 3\}.*?)(?=\n---|\Z)";
            MatchCollection baseMatches = Regex.Matches(content, monoBehaviourPattern, RegexOptions.Singleline | RegexOptions.Multiline);
            
            foreach (Match match in baseMatches)
            {
                string componentId = match.Groups[2].Value;
                if (!removedComponentIds.Contains(componentId))
                {
                    content = content.Replace(match.Value, "");
                    
                    string componentRefPattern = @"^\s+- component: \{fileID: " + Regex.Escape(componentId) + @"\}\s*$";
                    content = Regex.Replace(content, componentRefPattern, "", RegexOptions.Multiline);
                    removedComponentIds.Add(componentId);
                    removedCount++;
                }
            }
        }
        
        // 3. 保存到目标路径
        File.WriteAllText(targetPath, content);
        if (removedCount > 0)
        {
            Debug.Log($"已从预制体中移除 {removedCount} 个 PanelBase 及其子类组件");
        }
        else
        {
            Debug.Log("源预制体中未找到 PanelBase 组件，直接复制");
        }
    }
    
    private string GetPanelBaseGuid()
    {
        string metaPath = "Assets/HowFrame/HowTools/HowUI/PanelBase.cs.meta";
        if (File.Exists(metaPath))
        {
            string[] lines = File.ReadAllLines(metaPath);
            foreach (string line in lines)
            {
                if (line.StartsWith("guid:"))
                {
                    return line.Substring("guid:".Length).Trim();
                }
            }
        }
        return null;
    }
    
    private string GetSourceRootObjectName(string sourcePrefabPath)
    {
        string content = File.ReadAllText(sourcePrefabPath);
        
        // 查找所有 RectTransform 或 Transform，找到 m_Father 为 0 的（根对象）
        // 匹配 RectTransform 或 Transform 块，查找 m_Father: {fileID: 0}
        string transformPattern = @"^--- !u!(?:224|4) &(\d+)\s+(?:RectTransform|Transform):[\s\S]*?m_GameObject: \{fileID: (\d+)\}[\s\S]*?m_Father: \{fileID: 0\}";
        MatchCollection transformMatches = Regex.Matches(content, transformPattern, RegexOptions.Multiline);
        
        foreach (Match transformMatch in transformMatches)
        {
            string gameObjectId = transformMatch.Groups[2].Value;
            
            // 找到对应的 GameObject，获取其 m_Name
            string gameObjectPattern = @"^--- !u!1 &" + Regex.Escape(gameObjectId) + @"\s+GameObject:[\s\S]*?m_Name: (.+?)\s";
            Match gameObjectMatch = Regex.Match(content, gameObjectPattern, RegexOptions.Multiline);
            
            if (gameObjectMatch.Success)
            {
                return gameObjectMatch.Groups[1].Value;
            }
        }
        
        return null;
    }
    
    private void RenameRootObject(string prefabPath, string sourceRootName, string newName)
    {
        string content = File.ReadAllText(prefabPath);
        
        // 找到所有根对象（m_Father: {fileID: 0} 的 Transform 对应的 GameObject），然后在其中找到 m_Name 匹配源名称的
        string transformPattern = @"^--- !u!(?:224|4) &(\d+)\s+(?:RectTransform|Transform):[\s\S]*?m_GameObject: \{fileID: (\d+)\}[\s\S]*?m_Father: \{fileID: 0\}";
        MatchCollection transformMatches = Regex.Matches(content, transformPattern, RegexOptions.Multiline);
        
        string gameObjectId = null;
        foreach (Match transformMatch in transformMatches)
        {
            string candidateGameObjectId = transformMatch.Groups[2].Value;
            
            // 检查这个 GameObject 的 m_Name 是否匹配源名称
            string gameObjectPattern = @"^--- !u!1 &" + Regex.Escape(candidateGameObjectId) + @"\s+GameObject:[\s\S]*?m_Name: " + Regex.Escape(sourceRootName) + @"\s";
            Match gameObjectMatch = Regex.Match(content, gameObjectPattern, RegexOptions.Multiline);
            
            if (gameObjectMatch.Success)
            {
                gameObjectId = candidateGameObjectId;
                break;
            }
        }
        
        if (string.IsNullOrEmpty(gameObjectId))
        {
            Debug.LogError($"无法找到根对象 (m_Name: {sourceRootName})");
            return;
        }
        
        // 将根对象的 m_Name 改为新的名称
        string namePattern = @"(^--- !u!1 &" + Regex.Escape(gameObjectId) + @"\s+GameObject:[\s\S]*?m_Name: )" + Regex.Escape(sourceRootName) + @"(\s)";
        content = Regex.Replace(content, namePattern, "$1" + newName + "$2", RegexOptions.Multiline);
        
        File.WriteAllText(prefabPath, content);
        Debug.Log($"成功将根对象名称从 {sourceRootName} 改为 {newName}");
    }
    
    private void LoadSettings()
    {
        panelName = EditorPrefs.GetString("AutoUI_PanelName", "Menu");
        scriptOutputPath = EditorPrefs.GetString("AutoUI_ScriptOutputPath", "Assets/HowFrameExample");
        prefabOutputPath = EditorPrefs.GetString("AutoUI_PrefabOutputPath", "Assets/GameRes/UI");
        
        // 加载默认预制体路径，如果为空或未设置，使用默认值
        string savedDefaultPath = EditorPrefs.GetString("AutoUI_DefaultPrefabPath", "");
        if (string.IsNullOrEmpty(savedDefaultPath))
        {
            defaultPrefabPath = "Assets/Editor/AutoUI/DefaultPanel.prefab";
        }
        else
        {
            defaultPrefabPath = savedDefaultPath;
        }
        
        // 加载预制体引用
        string prefabPath = EditorPrefs.GetString("AutoUI_PrefabReference", "");
        if (!string.IsNullOrEmpty(prefabPath))
        {
            prefabReference = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
    }
    
    private void SaveSettings()
    {
        EditorPrefs.SetString("AutoUI_PanelName", panelName);
        EditorPrefs.SetString("AutoUI_ScriptOutputPath", scriptOutputPath);
        EditorPrefs.SetString("AutoUI_PrefabOutputPath", prefabOutputPath);
        EditorPrefs.SetString("AutoUI_DefaultPrefabPath", defaultPrefabPath);
        
        if (prefabReference != null)
        {
            string path = AssetDatabase.GetAssetPath(prefabReference);
            EditorPrefs.SetString("AutoUI_PrefabReference", path);
        }
        else
        {
            EditorPrefs.DeleteKey("AutoUI_PrefabReference");
        }
    }
}
#endif

