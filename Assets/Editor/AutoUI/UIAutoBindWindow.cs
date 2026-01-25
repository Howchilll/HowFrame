#define EDITOR
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Text;

public class UIAutoBindWindow : EditorWindow
{
    private GameObject prefab;
    private string scriptParentFolder = "";
    private Vector2 scrollPos;
    
    private const string ScriptParentFolderKey = "UIAutoBind_ScriptParentFolder";
    
    // 命名规范映射
    private static readonly Dictionary<string, Type> NamingRules = new Dictionary<string, Type>
    {
        { "btn", typeof(UnityEngine.UI.Button) },
        { "button", typeof(UnityEngine.UI.Button) },
        { "txt", typeof(TMPro.TextMeshProUGUI) },
        { "ttxt", typeof(TMPro.TextMeshProUGUI) },
        { "img", typeof(UnityEngine.UI.Image) },
        { "Ipt", typeof(UnityEngine.UI.InputField) },
        { "ani", typeof(Animator) },
        { "tog", typeof(UnityEngine.UI.Toggle) },
        { "scb", typeof(UnityEngine.UI.Scrollbar) },
        { "scr", typeof(UnityEngine.UI.ScrollRect) },
        { "par", typeof(ParticleSystem) },
        { "sli", typeof(UnityEngine.UI.Slider) }
    };
    
    [MenuItem("Assets/Auto Bind UI", true)]
    public static bool ValidateAutoBind()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null) return false;
        
        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) return false;
        
        // 检查是否是 PanelBase 或其子类
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
        if (prefabInstance == null) return false;
        
        // 检查所有组件，看是否有 PanelBase 或其子类
        var components = prefabInstance.GetComponents<Component>();
        bool hasPanelBase = false;
        foreach (var comp in components)
        {
            if (comp != null && typeof(HowFrame.PanelBase).IsAssignableFrom(comp.GetType()))
            {
                hasPanelBase = true;
                break;
            }
        }
        
        PrefabUtility.UnloadPrefabContents(prefabInstance);
        
        return hasPanelBase;
    }
    
    [MenuItem("Assets/Auto Bind UI")]
    public static void ShowWindow()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择一个预制体", "确定");
            return;
        }
        
        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
        {
            EditorUtility.DisplayDialog("错误", "请选择一个预制体文件", "确定");
            return;
        }
        
        // 检查是否是 PanelBase 或其子类
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
        if (prefabInstance == null)
        {
            EditorUtility.DisplayDialog("错误", "无法加载预制体", "确定");
            return;
        }
        
        // 查找 PanelBase 或其子类组件
        HowFrame.PanelBase panelBase = null;
        var components = prefabInstance.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp != null && typeof(HowFrame.PanelBase).IsAssignableFrom(comp.GetType()))
            {
                panelBase = comp as HowFrame.PanelBase;
                break;
            }
        }
        
        if (panelBase == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            EditorUtility.DisplayDialog("错误", "该预制体不包含 PanelBase 或其子类组件", "确定");
            return;
        }
        
        PrefabUtility.UnloadPrefabContents(prefabInstance);
        
        var window = GetWindow<UIAutoBindWindow>("UI Auto Bind");
        window.prefab = selected;
        window.minSize = new Vector2(400, 200);
        
        // 从 EditorPrefs 加载路径
        window.scriptParentFolder = EditorPrefs.GetString(ScriptParentFolderKey, "");
    }
    
    private void OnDisable()
    {
        // 窗口关闭时保存路径
        EditorPrefs.SetString(ScriptParentFolderKey, scriptParentFolder);
    }
    
    private void OnGUI()
    {
        if (prefab == null)
        {
            EditorGUILayout.HelpBox("请选择一个包含 PanelBase 的预制体", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField("UI 自动绑定工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("预制体:", prefab.name);
        EditorGUILayout.Space(5);
        
        // 脚本父文件夹选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("脚本文件夹的父文件夹", GUILayout.Width(150));
        
        EditorGUI.BeginChangeCheck();
        scriptParentFolder = EditorGUILayout.TextField(scriptParentFolder);
        if (EditorGUI.EndChangeCheck())
        {
            // TextField 值改变时保存
            EditorPrefs.SetString(ScriptParentFolderKey, scriptParentFolder);
        }
        
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择脚本文件夹的父文件夹", scriptParentFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    scriptParentFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    // 选择路径后立即保存
                    EditorPrefs.SetString(ScriptParentFolderKey, scriptParentFolder);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请选择 Assets 文件夹内的路径", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("确定", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(scriptParentFolder))
            {
                EditorUtility.DisplayDialog("错误", "请选择脚本文件夹的父文件夹", "确定");
                return;
            }
            
            // 保存路径到 EditorPrefs
            EditorPrefs.SetString(ScriptParentFolderKey, scriptParentFolder);
            
            AutoBind();
        }
    }
    
    private void AutoBind()
    {
        try
        {
            // 1. 加载预制体并扫描所有对象
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            if (prefabInstance == null)
            {
                EditorUtility.DisplayDialog("错误", "无法加载预制体", "确定");
                return;
            }
            
            // 获取 PanelBase 或其子类组件以确定类名
            HowFrame.PanelBase panelBase = null;
            var components = prefabInstance.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null && typeof(HowFrame.PanelBase).IsAssignableFrom(comp.GetType()))
                {
                    panelBase = comp as HowFrame.PanelBase;
                    break;
                }
            }
            
            if (panelBase == null)
            {
                PrefabUtility.UnloadPrefabContents(prefabInstance);
                EditorUtility.DisplayDialog("错误", "预制体不包含 PanelBase 或其子类组件", "确定");
                return;
            }
            
            string className = panelBase.GetType().Name;
            string panelName = className.Replace("Panel", "");
            
            // 打印处理信息
            Debug.Log($"现在在处理{className}的脚本绑定");
            
            // 2. 扫描所有子对象
            List<BindInfo> bindInfos = ScanPrefab(prefabInstance);
            
            if (bindInfos.Count == 0)
            {
                PrefabUtility.UnloadPrefabContents(prefabInstance);
                EditorUtility.DisplayDialog("提示", "未找到需要绑定的UI对象。\n\n请确保对象名称符合命名规范（例如：start_btn, title_txt等）", "确定");
                return;
            }
            
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            // 3. 查找 View 和 Model 脚本
            string viewScriptPath = FindScript(scriptParentFolder, className + "View.cs");
            string modelScriptPath = FindScript(scriptParentFolder, className + "Model.cs");
            
            if (string.IsNullOrEmpty(viewScriptPath))
            {
                EditorUtility.DisplayDialog("错误", $"未找到 {className}View.cs 脚本\n\n请检查：\n1. 脚本是否存在于 {scriptParentFolder} 及其子文件夹中\n2. 脚本名称是否正确（应该是 {className}View.cs）", "确定");
                return;
            }
            
            // 4. 更新 View 脚本
            UpdateViewScript(viewScriptPath, bindInfos, className);
            
            // 5. 更新 Model 脚本
            if (!string.IsNullOrEmpty(modelScriptPath))
            {
                UpdateModelScript(modelScriptPath, bindInfos, className);
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("成功", $"UI 自动绑定完成！\n\n已绑定 {bindInfos.Count} 个对象\nView 脚本已更新\n{(string.IsNullOrEmpty(modelScriptPath) ? "未找到 Model 脚本" : "Model 脚本已更新")}", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"绑定失败：{e.Message}\n{e.StackTrace}", "确定");
            Debug.LogError($"UI Auto Bind 失败: {e}");
        }
    }
    
    private List<BindInfo> ScanPrefab(GameObject root)
    {
        List<BindInfo> bindInfos = new List<BindInfo>();
        ScanGameObject(root, root.transform, bindInfos);
        return bindInfos;
    }
    
    private void ScanGameObject(GameObject root, Transform obj, List<BindInfo> bindInfos)
    {
        string objName = obj.name;
        
        // 检查对象名是否有下划线
        if (objName.Contains("_"))
        {
            // 按下划线分割
            string[] parts = objName.Split('_');
            if (parts.Length >= 2)
            {
                string baseName = parts[0];
                
                // 从第二个部分开始，每个都是标签
                for (int i = 1; i < parts.Length; i++)
                {
                    string tag = parts[i];
                    
                    // 检查标签是否匹配命名规范
                    foreach (var rule in NamingRules)
                    {
                        if (tag.Equals(rule.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            BindInfo info = new BindInfo
                            {
                                GameObjectName = objName,
                                BaseName = baseName,
                                ComponentType = rule.Value,
                                Suffix = rule.Key,
                                FullPath = GetFullPath(root.transform, obj)
                            };
                            bindInfos.Add(info);
                            break; // 一个标签只匹配一个规则
                        }
                    }
                }
            }
        }
        
        // 递归扫描子对象
        foreach (Transform child in obj)
        {
            ScanGameObject(root, child, bindInfos);
        }
    }
    
    private string GetFullPath(Transform root, Transform target)
    {
        List<string> path = new List<string>();
        Transform current = target;
        while (current != root && current != null)
        {
            path.Insert(0, current.name);
            current = current.parent;
        }
        return string.Join("/", path);
    }
    
    private string FindScript(string parentFolder, string scriptName)
    {
        // 标准化路径格式（统一使用正斜杠，去除尾部斜杠）
        string normalizedParentFolder = parentFolder.Replace('\\', '/').TrimEnd('/');
        if (!normalizedParentFolder.StartsWith("Assets/"))
        {
            normalizedParentFolder = "Assets/" + normalizedParentFolder.TrimStart('/');
        }
        
        string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(scriptName));
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string normalizedPath = path.Replace('\\', '/');
            
            if (normalizedPath.EndsWith("/" + scriptName) || normalizedPath.EndsWith(scriptName))
            {
                // 检查路径是否在父文件夹或其子文件夹中
                if (normalizedPath.StartsWith(normalizedParentFolder + "/") || normalizedPath == normalizedParentFolder)
                {
                    return path;
                }
            }
        }
        
        return null;
    }
    
    private void UpdateViewScript(string scriptPath, List<BindInfo> bindInfos, string className)
    {
        string content = File.ReadAllText(scriptPath);
        
        // 1. 更新 Define 区域
        UpdateDefineSection(ref content, bindInfos);
        
        // 2. 更新 Init 区域
        UpdateInitSection(ref content, bindInfos, className);
        
        // 3. 更新 Show 区域（如果有 ttxt）
        bool hasTtxt = bindInfos.Any(b => b.Suffix == "ttxt");
        if (hasTtxt)
        {
            UpdateShowSection(ref content, bindInfos);
        }
        
        File.WriteAllText(scriptPath, content, System.Text.Encoding.UTF8);
    }
    
    private void UpdateDefineSection(ref string content, List<BindInfo> bindInfos)
    {
        string defineStart = "//Define";
        string defineEnd = "//end Define";
        
        int startIndex = content.IndexOf(defineStart);
        int endIndex = content.IndexOf(defineEnd);
        
        if (startIndex == -1 || endIndex == -1)
        {
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.Append(defineStart);
        sb.Append("\n");
        
        foreach (var info in bindInfos)
        {
            string typeName = GetTypeName(info.ComponentType);
            string fieldName = GetFieldName(info);
            sb.Append($"    [SerializeField] private {typeName} {fieldName};\n");
        }
        
        // 如果有 ttxt，添加 TransContent
        bool hasTtxt = bindInfos.Any(b => b.Suffix == "ttxt");
        if (hasTtxt)
        {
            sb.Append("    private string[] TransContent;\n");
        }
        
        sb.Append("\n    ");
        sb.Append(defineEnd);
        
        string before = content.Substring(0, startIndex);
        string after = content.Substring(endIndex + defineEnd.Length);
        content = before + sb.ToString() + after;
    }
    
    private void UpdateInitSection(ref string content, List<BindInfo> bindInfos, string className)
    {
        string initStart = "//Init";
        string initEnd = "//end Init";
        
        int startIndex = content.IndexOf(initStart);
        int endIndex = content.IndexOf(initEnd);
        
        if (startIndex == -1 || endIndex == -1)
        {
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.Append(initStart);
        sb.Append("\n");
        
        // 初始化组件引用
        foreach (var info in bindInfos)
        {
            string fieldName = GetFieldName(info);
            string typeName = GetTypeName(info.ComponentType);
            sb.Append($"        {fieldName} = transform.Find(\"{info.FullPath}\").GetComponent<{typeName}>();\n");
        }
        
        sb.Append("\n");
        
        // 注册回调
        foreach (var info in bindInfos)
        {
            if (ShouldRegisterCallback(info.Suffix))
            {
                string fieldName = GetFieldName(info);
                string callbackName = GetCallbackName(info);
                
                if (info.Suffix == "btn" || info.Suffix == "button")
                {
                    sb.Append($"        {fieldName}.onClick.AddListener({callbackName});\n");
                }
                else if (info.Suffix == "tog")
                {
                    sb.Append($"        {fieldName}.onValueChanged.AddListener((value) => {callbackName}(value));\n");
                }
                else if (info.Suffix == "sli")
                {
                    sb.Append($"        {fieldName}.onValueChanged.AddListener((value) => {callbackName}(value));\n");
                }
                else if (info.Suffix == "Ipt")
                {
                    sb.Append($"        {fieldName}.onEndEdit.AddListener((value) => {callbackName}(value));\n");
                }
            }
        }
        
        sb.Append("\n        ");
        sb.Append(initEnd);
        
        string before = content.Substring(0, startIndex);
        string after = content.Substring(endIndex + initEnd.Length);
        content = before + sb.ToString() + after;
    }
    
    private void UpdateShowSection(ref string content, List<BindInfo> bindInfos)
    {
        string showStart = "//Show";
        string showEnd = "//end Show";
        
        int startIndex = content.IndexOf(showStart);
        int endIndex = content.IndexOf(showEnd);
        
        if (startIndex == -1 || endIndex == -1)
        {
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.Append(showStart);
        sb.Append("\n");
        sb.Append("\n");
        
        // 获取多语言内容
        var ttxtInfos = bindInfos.Where(b => b.Suffix == "ttxt").ToList();
        if (ttxtInfos.Count > 0)
        {
            sb.Append("        var rawContent = LangManager.GetLangContent(LangModuleEnum.UI, \"XXXContent\");\n");
            sb.Append("        TransContent = rawContent.Split(\",\");\n");
            
            int index = 0;
            foreach (var info in ttxtInfos)
            {
                string fieldName = GetFieldName(info);
                sb.Append($"        {fieldName}.text = TransContent[{index}];\n");
                index++;
            }
            
            sb.Append("\n        //告诉开发者赋值的顺序: ");
            List<string> order = new List<string>();
            foreach (var info in ttxtInfos)
            {
                order.Add(info.GameObjectName);
            }
            sb.Append(string.Join(", ", order));
            sb.Append("\n");
        }
        
        sb.Append("\n        ");
        sb.Append(showEnd);
        
        string before = content.Substring(0, startIndex);
        string after = content.Substring(endIndex + showEnd.Length);
        content = before + sb.ToString() + after;
    }
    
    private void UpdateModelScript(string scriptPath, List<BindInfo> bindInfos, string className)
    {
        string content = File.ReadAllText(scriptPath);
        
        // 检查并添加回调方法
        foreach (var info in bindInfos)
        {
            if (ShouldRegisterCallback(info.Suffix))
            {
                string callbackName = GetCallbackName(info);
                
                // 检查方法是否已存在
                if (!content.Contains(callbackName))
                {
                    // 在文件末尾（最后一个 } 之前）添加方法
                    int lastBraceIndex = content.LastIndexOf('}');
                    if (lastBraceIndex > 0)
                    {
                        string method = GenerateCallbackMethod(info, callbackName);
                        content = content.Insert(lastBraceIndex, method);
                    }
                }
            }
        }
        
        File.WriteAllText(scriptPath, content, System.Text.Encoding.UTF8);
    }
    
    private string GenerateCallbackMethod(BindInfo info, string callbackName)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("\n    ");
        
        if (info.Suffix == "btn" || info.Suffix == "button")
        {
            sb.Append($"private void {callbackName}()\n");
        }
        else if (info.Suffix == "tog")
        {
            sb.Append($"private void {callbackName}(bool value)\n");
        }
        else if (info.Suffix == "sli")
        {
            sb.Append($"private void {callbackName}(float value)\n");
        }
        else if (info.Suffix == "Ipt")
        {
            sb.Append($"private void {callbackName}(string value)\n");
        }
        else
        {
            sb.Append($"private void {callbackName}()\n");
        }
        
        sb.Append("    {\n");
        sb.Append("        \n");
        sb.Append("    }\n");
        
        return sb.ToString();
    }
    
    private string GetTypeName(Type type)
    {
        if (type == typeof(UnityEngine.UI.Button)) return "Button";
        if (type == typeof(TMPro.TextMeshProUGUI)) return "TextMeshProUGUI";
        if (type == typeof(UnityEngine.UI.Image)) return "Image";
        if (type == typeof(UnityEngine.UI.InputField)) return "InputField";
        if (type == typeof(Animator)) return "Animator";
        if (type == typeof(UnityEngine.UI.Toggle)) return "Toggle";
        if (type == typeof(UnityEngine.UI.Scrollbar)) return "Scrollbar";
        if (type == typeof(UnityEngine.UI.ScrollRect)) return "ScrollRect";
        if (type == typeof(ParticleSystem)) return "ParticleSystem";
        if (type == typeof(UnityEngine.UI.Slider)) return "Slider";
        return type.Name;
    }
    
    private string GetFieldName(BindInfo info)
    {
        // 字段名格式：基础名_标签（如 start_btn, start_img）
        return info.BaseName + "_" + info.Suffix;
    }
    
    private string GetCallbackName(BindInfo info)
    {
        string suffixName = info.Suffix == "btn" || info.Suffix == "button" ? "BtnClick" :
                           info.Suffix == "tog" ? "ToggleChange" :
                           info.Suffix == "sli" ? "SliderChange" :
                           info.Suffix == "Ipt" ? "InputChange" : "Change";
        // 回调方法名格式：On基础名+后缀（如 OnToggle3ToggleChange，去掉下划线和标签）
        return "On" + info.BaseName + suffixName;
    }
    
    private bool ShouldRegisterCallback(string suffix)
    {
        return suffix == "btn" || suffix == "button" || suffix == "tog" || 
               suffix == "sli" || suffix == "Ipt";
    }
    
    private class BindInfo
    {
        public string GameObjectName;  // 完整的对象名
        public string BaseName;         // 基础名（下划线前的部分）
        public Type ComponentType;
        public string Suffix;          // 标签（如 btn, img, ttxt）
        public string FullPath;        // 完整路径
    }
}
#endif

