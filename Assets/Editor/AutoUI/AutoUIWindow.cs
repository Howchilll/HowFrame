#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using Object = UnityEngine.Object;

// 组件信息类
public class ComponentInfo
{
    public string gameObjectName;
    public string relativePath; // 从根对象到该对象的相对路径
    public string componentType;
    public string variableName;
    public bool hasCallback; // 是否需要回调（如Button需要OnClick）
    
    public ComponentInfo(string name, string path, string type, string varName, bool callback = false)
    {
        gameObjectName = name;
        relativePath = path;
        componentType = type;
        variableName = varName;
        hasCallback = callback;
    }
}

public class AutoUIWindow : EditorWindow
{
    private string scriptOutputPath = "Assets/Scripts/DMT_ACT_Implement/UI";
    private Object prefabReference;
    private string panelName; // 从预制体名称自动提取
    
    private Vector2 scrollPos;
    
    // 后缀到组件类型的映射
    private Dictionary<string, string> suffixToComponentType = new Dictionary<string, string>
    {
        { "_Btn", "Button" },
        { "_Button", "Button" },
        { "_Text", "Text" },
        { "_Txt", "TextMeshProUGUI" },
        { "_Img", "Image" },
        { "_Image", "Image" },
        { "_RImg", "RawImage" },
        { "_Input", "TMP_InputField" },
        { "_InputField", "TMP_InputField" },
        { "_Toggle", "Toggle" },
        { "_Tog", "Toggle" },
        { "_Slider", "Slider" },
        { "_Sli", "Slider" },
        { "_Scroll", "ScrollRect" },
        { "_ScrollRect", "ScrollRect" },
        { "_Scr", "ScrollRect" },
        { "_Dropdown", "TMP_Dropdown" },
        { "_Drop", "TMP_Dropdown" },
        { "_Obj", "GameObject" },
    };
    
    [MenuItem("Assets/Create/MVC scripts", true)]
    public static bool ValidateCreateMVCScripts()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null) return false;
        
        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) return false;
        
        // 检查预制体名称是否以"Panel"结尾
        string prefabName = Path.GetFileNameWithoutExtension(path);
        return prefabName.EndsWith("Panel", StringComparison.OrdinalIgnoreCase);
    }
    
    [MenuItem("Assets/Create/MVC scripts", false, 1)]
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
        
        string prefabName = Path.GetFileNameWithoutExtension(path);
        if (!prefabName.EndsWith("Panel", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("错误", "预制体名称必须以\"Panel\"结尾", "确定");
            return;
        }
        
        var window = GetWindow<AutoUIWindow>("MVC Scripts Generator");
        window.minSize = new Vector2(400, 200);
        
        // 设置预制体引用
        window.prefabReference = selected;
        
        // 使用完整的预制体名称作为面板名称（包含Panel后缀）
        window.panelName = prefabName;
        
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
        
        EditorGUILayout.LabelField("MVC Scripts Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 显示当前选择的预制体信息
        if (prefabReference != null)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefabReference);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            EditorGUILayout.HelpBox($"预制体: {prefabName}\n面板名称: {panelName}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("未选择预制体", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }
        
        EditorGUILayout.Space(10);
        
        // 脚本输出路径
        EditorGUILayout.LabelField("脚本输出路径", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        scriptOutputPath = EditorGUILayout.TextField(scriptOutputPath);
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
        
        EditorGUILayout.Space(10);
        
        // 生成按钮
        if (GUILayout.Button("生成脚本", GUILayout.Height(30)))
        {
            Generate();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void Generate()
    {
        Debug.Log("========== Generate方法被调用 ==========");
        try
        {
            if (prefabReference == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择预制体", "确定");
                Debug.LogError("预制体参考为空！");
                return;
            }
            
            if (string.IsNullOrEmpty(scriptOutputPath))
            {
                EditorUtility.DisplayDialog("错误", "请选择脚本输出路径", "确定");
                return;
            }
            
            Debug.Log($"预制体参考: {prefabReference.name}");
            Debug.Log($"提取的面板名称: {panelName}");
            
            // 生成脚本
            GenerateScripts();
            
            EditorUtility.DisplayDialog("成功", $"面板 {panelName} 脚本生成成功！\n请查看Console了解详细信息", "确定");
        }
        catch (Exception e)
        {
            Debug.LogError($"========== 生成失败 ==========");
            Debug.LogError($"错误信息: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}\n{e.StackTrace}", "确定");
        }
    }
    
    private void GenerateScripts()
    {
        Debug.Log("========== 开始生成脚本 ==========");
        Debug.Log($"面板名称: {panelName}");
        
        // 类名就是完整的预制体名称（包含Panel后缀）
        string className = panelName;
        
        // 文件名基础名称（去掉Panel后缀，用于View和Model文件名）
        string baseName = panelName;
        if (baseName.EndsWith("Panel", StringComparison.OrdinalIgnoreCase))
        {
            baseName = baseName.Substring(0, baseName.Length - 5);
        }
        
        // 创建目录结构：目标目录/xxxPanel/
        string panelDir = Path.Combine(scriptOutputPath, className);
        Debug.Log($"输出目录: {panelDir}");
        
        // 确保目录存在
        if (!Directory.Exists(panelDir))
        {
            Directory.CreateDirectory(panelDir);
        }
        
        // 解析预制体，提取组件信息
        Debug.Log("开始解析预制体组件...");
        List<ComponentInfo> components = ParsePrefabComponents();
        Debug.Log($"解析完成，找到 {components.Count} 个组件");
        
        // 生成主脚本 Panel.cs（只声明类名，不做实现）
        Debug.Log("生成Panel脚本...");
        string panelContent = GeneratePanelScript();
        string panelPath = Path.Combine(panelDir, className + ".cs");
        File.WriteAllText(panelPath, panelContent, System.Text.Encoding.UTF8);
        Debug.Log($"Panel脚本已写入: {panelPath}");
        
        // 生成 View 脚本（包含组件注册）
        Debug.Log("生成View脚本...");
        string viewContent = GenerateViewScript(components);
        Debug.Log($"View脚本内容长度: {viewContent.Length}");
        Debug.Log($"View脚本是否包含组件代码: {viewContent.Contains("GetComponent")}");
        
        string viewPath = Path.Combine(panelDir, baseName + "View.cs");
        File.WriteAllText(viewPath, viewContent, System.Text.Encoding.UTF8);
        Debug.Log($"View脚本已写入: {viewPath}");
        
        // 生成 Model 脚本（包含回调方法）
        Debug.Log("生成Model脚本...");
        string modelPath = Path.Combine(panelDir, baseName + "Model.cs");
        string modelContent = GenerateModelScript(components, modelPath);
        File.WriteAllText(modelPath, modelContent, System.Text.Encoding.UTF8);
        Debug.Log($"Model脚本已写入: {modelPath}");
        
        // 刷新资源数据库
        Debug.Log("刷新资源数据库...");
        AssetDatabase.Refresh();
        Debug.Log("========== 脚本生成完成 ==========");
    }
    
    private string GeneratePanelScript()
    {
        string templatePath = "Assets/Editor/AutoUI/Template_Panel.cs.txt";
        return LoadTemplateAndReplace(templatePath);
    }
    
    private List<ComponentInfo> ParsePrefabComponents()
    {
        List<ComponentInfo> components = new List<ComponentInfo>();
        
        if (prefabReference == null)
        {
            Debug.LogWarning("预制体参考为空");
            return components;
        }
        
        string prefabPath = AssetDatabase.GetAssetPath(prefabReference);
        if (string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
        {
            Debug.LogWarning($"预制体路径无效: {prefabPath}");
            return components;
        }
        
        string content = File.ReadAllText(prefabPath);
        
        // 第一步：提取所有GameObject的信息（ID和名称）
        Dictionary<string, string> gameObjectIdToName = new Dictionary<string, string>(); // GameObject ID -> Name
        Dictionary<string, string> gameObjectIdToTransformId = new Dictionary<string, string>(); // GameObject ID -> Transform ID
        Dictionary<string, string> transformIdToGameObjectId = new Dictionary<string, string>(); // Transform ID -> GameObject ID
        Dictionary<string, string> transformIdToParentId = new Dictionary<string, string>(); // Transform ID -> Parent Transform ID
        
        // 提取GameObject信息
        string gameObjectPattern = @"^--- !u!1 &(\d+)\s+GameObject:[\s\S]*?m_Name: ([^\r\n]+)";
        MatchCollection gameObjectMatches = Regex.Matches(content, gameObjectPattern, RegexOptions.Multiline);
        
        foreach (Match match in gameObjectMatches)
        {
            string gameObjectId = match.Groups[1].Value;
            string gameObjectName = match.Groups[2].Value.Trim();
            gameObjectIdToName[gameObjectId] = gameObjectName;
        }
        
        // 提取Transform信息，建立父子关系
        // 匹配RectTransform或Transform
        string transformPattern = @"^--- !u!(?:224|4) &(\d+)\s+(?:RectTransform|Transform):[\s\S]*?m_GameObject: \{fileID: (\d+)\}[\s\S]*?m_Father: \{fileID: (\d+)\}";
        MatchCollection transformMatches = Regex.Matches(content, transformPattern, RegexOptions.Multiline);
        
        string rootTransformId = null;
        foreach (Match match in transformMatches)
        {
            string transformId = match.Groups[1].Value;
            string gameObjectId = match.Groups[2].Value;
            string parentId = match.Groups[3].Value;
            
            gameObjectIdToTransformId[gameObjectId] = transformId;
            transformIdToGameObjectId[transformId] = gameObjectId;
            
            if (parentId == "0")
            {
                rootTransformId = transformId;
            }
            else
            {
                transformIdToParentId[transformId] = parentId;
            }
        }
        
        // 第二步：为每个GameObject计算相对路径
        Dictionary<string, string> gameObjectIdToPath = new Dictionary<string, string>();
        
        // 递归函数：计算从根对象到指定对象的路径
        System.Func<string, string> getPath = null;
        getPath = (string transformId) =>
        {
            if (!transformIdToGameObjectId.ContainsKey(transformId))
                return "";
            
            string gameObjectId = transformIdToGameObjectId[transformId];
            string name = gameObjectIdToName[gameObjectId];
            
            // 如果是根对象，返回空字符串（根对象不需要路径）
            if (transformId == rootTransformId || name == panelName)
            {
                return "";
            }
            
            // 获取父Transform
            if (transformIdToParentId.ContainsKey(transformId))
            {
                string parentTransformId = transformIdToParentId[transformId];
                string parentPath = getPath(parentTransformId);
                
                if (string.IsNullOrEmpty(parentPath))
                {
                    return name;
                }
                else
                {
                    return parentPath + "/" + name;
                }
            }
            
            return name;
        };
        
        // 为所有GameObject计算路径
        foreach (var kvp in gameObjectIdToName)
        {
            string gameObjectId = kvp.Key;
            if (gameObjectIdToTransformId.ContainsKey(gameObjectId))
            {
                string transformId = gameObjectIdToTransformId[gameObjectId];
                string path = getPath(transformId);
                if (!string.IsNullOrEmpty(path))
                {
                    gameObjectIdToPath[gameObjectId] = path;
                }
            }
        }
        
        Debug.Log($"找到 {gameObjectMatches.Count} 个GameObject");
        
        HashSet<string> processedNames = new HashSet<string>();
        
        foreach (Match match in gameObjectMatches)
        {
            string gameObjectId = match.Groups[1].Value;
            string gameObjectName = match.Groups[2].Value.Trim();
            
            // 跳过根对象（面板名称，已经是完整的名称包含Panel后缀）
            if (gameObjectName == panelName)
            {
                Debug.Log($"跳过根对象: {gameObjectName}");
                continue;
            }
            
            // 获取相对路径
            string relativePath = gameObjectName; // 默认使用对象名
            if (gameObjectIdToPath.ContainsKey(gameObjectId))
            {
                relativePath = gameObjectIdToPath[gameObjectId];
            }
            
            // 避免重复处理同名对象（使用路径作为唯一标识）
            string uniqueKey = relativePath;
            if (processedNames.Contains(uniqueKey))
            {
                continue;
            }
            
            processedNames.Add(uniqueKey);
            
            // 根据后缀识别组件类型（支持多个后缀，如 game_Btn_Img）
            // 从后往前查找所有匹配的后缀
            string remainingName = gameObjectName;
            List<(string suffix, string componentType, bool hasCallback)> matchedSuffixes = new List<(string, string, bool)>();
            
            // 按后缀长度从长到短排序，优先匹配长后缀
            var sortedSuffixes = suffixToComponentType.Keys.OrderByDescending(s => s.Length);
            
            // 从后往前匹配，每次匹配后去掉已匹配的部分
            while (!string.IsNullOrEmpty(remainingName))
            {
                bool found = false;
                foreach (string suffix in sortedSuffixes)
                {
                    if (remainingName.EndsWith(suffix))
                    {
                        string compType = suffixToComponentType[suffix];
                        // 根据组件类型判断是否需要回调，而不是只检查后缀
                        bool needsCallback = (compType == "Button" || 
                                            compType == "Toggle" || 
                                            compType == "Slider" ||
                                            compType == "TMP_Dropdown");
                        matchedSuffixes.Add((suffix, compType, needsCallback));
                        
                        // 去掉已匹配的后缀，继续查找前面的后缀
                        remainingName = remainingName.Substring(0, remainingName.Length - suffix.Length);
                        found = true;
                        break;
                    }
                }
                
                // 如果没有找到匹配的后缀，停止查找
                if (!found)
                {
                    break;
                }
            }
            
            // 为每个匹配的后缀创建组件信息（从后往前，所以需要反转）
            matchedSuffixes.Reverse();
            foreach (var (suffix, componentType, hasCallback) in matchedSuffixes)
            {
                // 生成变量名：去掉当前后缀，转换为驼峰命名，然后加上后缀标识
                // 例如：quit_Btn → 去掉 _Btn 得到 quit → quit + Btn → quitBtn
                // 例如：game_Btn_Img 匹配 _Img 时，去掉 _Img 得到 game_Btn → gameBtn + Img → gameBtnImg
                string nameWithoutSuffix = gameObjectName.Substring(0, gameObjectName.Length - suffix.Length);
                string baseName = ToCamelCase(nameWithoutSuffix);
                
                // 从后缀中提取标识（去掉下划线，首字母大写）
                string suffixIdentifier = suffix.Substring(1); // 去掉开头的下划线
                // 确保首字母大写
                if (suffixIdentifier.Length > 0)
                {
                    suffixIdentifier = char.ToUpper(suffixIdentifier[0]) + suffixIdentifier.Substring(1);
                }
                
                // 组合变量名：baseName + suffixIdentifier
                string variableName = baseName + suffixIdentifier;
                
                Debug.Log($"找到组件: {gameObjectName} -> {componentType} (路径: {relativePath}, 后缀: {suffix}, 变量名: {variableName})");
                components.Add(new ComponentInfo(gameObjectName, relativePath, componentType, variableName, hasCallback));
            }
        }
        
        Debug.Log($"总共解析到 {components.Count} 个组件");
        return components;
    }
    
    private string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        
        // 处理下划线命名：将下划线后的字母大写，去掉下划线
        string[] parts = name.Split('_');
        if (parts.Length > 1)
        {
            string result = parts[0].ToLower();
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
            return result;
        }
        
        // 首字母小写
        return char.ToLower(name[0]) + name.Substring(1);
    }
    
    private string GenerateViewScript(List<ComponentInfo> components)
    {
        Debug.Log("========== 开始生成View脚本 ==========");
        Debug.Log($"组件数量: {components.Count}");
        
        string templatePath = "Assets/Editor/AutoUI/Template_View.cs.txt";
        string templateContent = LoadTemplateAndReplace(templatePath);
        Debug.Log($"模板内容长度: {templateContent.Length}");
        Debug.Log($"模板是否包含Define标记: {templateContent.Contains("//Define")}");
        Debug.Log($"模板是否包含Init标记: {templateContent.Contains("//Init")}");
        
        // 生成Define区域的代码
        string defineCode = "";
        foreach (var comp in components)
        {
            defineCode += $"    private {comp.componentType} {comp.variableName};\n";
            Debug.Log($"添加Define: {comp.componentType} {comp.variableName}");
        }
        Debug.Log($"Define代码:\n{defineCode}");
        
        // 生成Init区域的代码（组件注册）
        string initCode = "";
        foreach (var comp in components)
        {
            // 使用transform.Find()来查找子对象，使用相对路径
            initCode += $"        {comp.variableName} = transform.Find(\"{comp.relativePath}\").GetComponent<{comp.componentType}>();\n";
            Debug.Log($"添加Init: {comp.variableName} = transform.Find(\"{comp.relativePath}\").GetComponent<{comp.componentType}>()");
            
            // 如果需要回调，添加回调注册（调用Model中的方法）
            if (comp.hasCallback)
            {
                if (comp.componentType == "Button")
                {
                    initCode += $"        {comp.variableName}.onClick.AddListener(On{ToPascalCase(comp.variableName)}Click);\n";
                }
                else if (comp.componentType == "Toggle")
                {
                    initCode += $"        {comp.variableName}.onValueChanged.AddListener(On{ToPascalCase(comp.variableName)}ValueChanged);\n";
                }
                else if (comp.componentType == "Slider")
                {
                    initCode += $"        {comp.variableName}.onValueChanged.AddListener(On{ToPascalCase(comp.variableName)}ValueChanged);\n";
                }
                else if (comp.componentType == "TMP_Dropdown")
                {
                    initCode += $"        {comp.variableName}.onValueChanged.AddListener(On{ToPascalCase(comp.variableName)}ValueChanged);\n";
                }
            }
        }
        Debug.Log($"Init代码:\n{initCode}");
        
        // 替换模板中的占位符
        // 处理Define区域 - 使用更灵活的匹配方式
        string defineStart = "//Define";
        string defineEnd = "//end Define";
        int defineStartIndex = templateContent.IndexOf(defineStart);
        int defineEndIndex = templateContent.IndexOf(defineEnd);
        
        Debug.Log($"Define区域查找: startIndex={defineStartIndex}, endIndex={defineEndIndex}");
        
        if (defineStartIndex >= 0 && defineEndIndex > defineStartIndex)
        {
            // 找到Define开始后的换行位置
            int defineNewlineIndex = templateContent.IndexOf('\n', defineStartIndex);
            string beforeDefine = templateContent.Substring(0, defineNewlineIndex + 1);
            string afterDefine = templateContent.Substring(defineEndIndex);
            
            if (!string.IsNullOrEmpty(defineCode))
            {
                templateContent = beforeDefine + defineCode.TrimEnd() + "\n\n" + afterDefine;
                Debug.Log("Define区域已替换");
            }
            else
            {
                templateContent = beforeDefine + "\n" + afterDefine;
                Debug.Log("Define区域为空，只移除占位符");
            }
        }
        else
        {
            Debug.LogError($"未找到Define区域标记! defineStartIndex={defineStartIndex}, defineEndIndex={defineEndIndex}");
        }
        
        // 处理Init区域 - 使用更灵活的匹配方式
        string initStart = "        //Init";
        string initEnd = "        //end Init";
        int initStartIndex = templateContent.IndexOf(initStart);
        int initEndIndex = templateContent.IndexOf(initEnd);
        
        Debug.Log($"Init区域查找: startIndex={initStartIndex}, endIndex={initEndIndex}");
        
        if (initStartIndex >= 0 && initEndIndex > initStartIndex)
        {
            // 找到Init开始后的换行位置
            int initNewlineIndex = templateContent.IndexOf('\n', initStartIndex);
            string beforeInit = templateContent.Substring(0, initNewlineIndex + 1);
            string afterInit = templateContent.Substring(initEndIndex);
            
            if (!string.IsNullOrEmpty(initCode))
            {
                templateContent = beforeInit + initCode.TrimEnd() + "\n" + afterInit;
                Debug.Log("Init区域已替换");
            }
            else
            {
                Debug.Log("Init区域为空");
            }
        }
        else
        {
            Debug.LogError($"未找到Init区域标记! initStartIndex={initStartIndex}, initEndIndex={initEndIndex}");
        }
        
        Debug.Log($"最终View脚本内容长度: {templateContent.Length}");
        Debug.Log("========== View脚本生成完成 ==========");
        return templateContent;
    }
    
    private string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        
        return char.ToUpper(name[0]) + name.Substring(1);
    }
    
    private string GenerateModelScript(List<ComponentInfo> components, string modelPath)
    {
        Debug.Log("========== 开始生成Model脚本 ==========");
        Debug.Log($"组件数量: {components.Count}");
        
        string templatePath = "Assets/Editor/AutoUI/Template_Model.cs.txt";
        string templateContent = LoadTemplateAndReplace(templatePath);
        
        // 检查文件是否存在
        bool fileExists = File.Exists(modelPath);
        string existingContent = "";
        HashSet<string> existingMethods = new HashSet<string>();
        
        if (fileExists)
        {
            Debug.Log("Model文件已存在，将进行增量更新");
            existingContent = File.ReadAllText(modelPath, System.Text.Encoding.UTF8);
            
            // 提取已存在的方法名（用于避免重复添加）
            // 匹配模式：private void 方法名(
            string methodPattern = @"private\s+void\s+(\w+)\s*\(";
            MatchCollection methodMatches = Regex.Matches(existingContent, methodPattern);
            foreach (Match match in methodMatches)
            {
                existingMethods.Add(match.Groups[1].Value);
            }
            
            // 保留现有的OnInit、OnShow、OnHide、WhenShowWithParameter方法
            // 提取这些方法的内容
            string preservedMethods = "";
            
            // 提取OnInit方法（保留用户实现，但会检查是否有ChangeLanguage订阅）
            string initPattern = @"(private\s+void\s+OnInit\(\)\s*\{[\s\S]*?\n\s*\})";
            Match initMatch = Regex.Match(existingContent, initPattern);
            if (initMatch.Success)
            {
                preservedMethods += initMatch.Value + "\n\n";
            }
            
            // 提取OnShow方法
            string showPattern = @"(private\s+void\s+OnShow\(\)\s*\{[\s\S]*?\n\s*\})";
            Match showMatch = Regex.Match(existingContent, showPattern);
            if (showMatch.Success)
            {
                preservedMethods += showMatch.Value + "\n\n";
            }
            
            // 提取OnHide方法
            string hidePattern = @"(private\s+void\s+OnHide\(\)\s*\{[\s\S]*?\n\s*\})";
            Match hideMatch = Regex.Match(existingContent, hidePattern);
            if (hideMatch.Success)
            {
                preservedMethods += hideMatch.Value + "\n\n";
            }
            
            // 提取WhenShowWithParameter方法
            string whenShowPattern = @"(protected\s+override\s+void\s+WhenShowWithParameter\([\s\S]*?\n\s*\})";
            Match whenShowMatch = Regex.Match(existingContent, whenShowPattern);
            if (whenShowMatch.Success)
            {
                preservedMethods += whenShowMatch.Value + "\n\n";
            }
            
            // 提取所有已存在的回调方法（保留用户实现）
            string callbackPattern = @"(private\s+void\s+On\w+Click\(\)\s*\{[\s\S]*?\n\s*\})";
            MatchCollection callbackMatches = Regex.Matches(existingContent, callbackPattern);
            foreach (Match match in callbackMatches)
            {
                preservedMethods += match.Value + "\n\n";
            }
            
            string valueChangedPattern = @"(private\s+void\s+On\w+ValueChanged\([^)]+\)\s*\{[\s\S]*?\n\s*\})";
            MatchCollection valueChangedMatches = Regex.Matches(existingContent, valueChangedPattern);
            foreach (Match match in valueChangedMatches)
            {
                preservedMethods += match.Value + "\n\n";
            }
            
            // 如果文件已存在，直接使用现有内容作为基础，只添加新方法
            templateContent = existingContent;
            
            // 移除旧的ChangeLanguage方法（如果存在），后面会重新生成
            string oldChangeLangPattern = @"private\s+void\s+ChangeLanguage\(\)\s*\{[\s\S]*?\n\s*\}";
            templateContent = Regex.Replace(templateContent, oldChangeLangPattern, "");
        }
        
        // 收集所有Txt组件（用于生成ChangeLanguage）
        List<ComponentInfo> txtComponents = components.Where(c => c.componentType == "TextMeshProUGUI" || c.componentType == "Text").ToList();
        txtComponents = txtComponents.OrderBy(c => c.variableName).ToList();
        
        // 生成ChangeLanguage方法（每次更新都覆盖）
        string changeLanguageCode = "";
        if (txtComponents.Count > 0)
        {
            changeLanguageCode += "    private void ChangeLanguage()\n";
            changeLanguageCode += "    {\n";
            changeLanguageCode += $"        var lang= LangManager.GetLangContent(LangModuleEnum.UI,\"{panelName}\");\n";
            changeLanguageCode += "        var langs = lang.Split(',');\n";
            
            for (int i = 0; i < txtComponents.Count; i++)
            {
                changeLanguageCode += $"        {txtComponents[i].variableName}.text = langs[{i}];\n";
            }
            
            changeLanguageCode += "    }\n";
        }
        
        // 生成新的回调方法（只添加不存在的）
        string newCallbackCode = "";
        foreach (var comp in components)
        {
            if (comp.hasCallback)
            {
                string methodName = "";
                string methodSignature = "";
                
                if (comp.componentType == "Button")
                {
                    methodName = $"On{ToPascalCase(comp.variableName)}Click";
                    methodSignature = $"    private void {methodName}()\n";
                }
                else if (comp.componentType == "Toggle")
                {
                    methodName = $"On{ToPascalCase(comp.variableName)}ValueChanged";
                    methodSignature = $"    private void {methodName}(bool value)\n";
                }
                else if (comp.componentType == "Slider")
                {
                    methodName = $"On{ToPascalCase(comp.variableName)}ValueChanged";
                    methodSignature = $"    private void {methodName}(float value)\n";
                }
                else if (comp.componentType == "TMP_Dropdown")
                {
                    methodName = $"On{ToPascalCase(comp.variableName)}ValueChanged";
                    methodSignature = $"    private void {methodName}(int value)\n";
                }
                
                // 只添加不存在的方法
                if (!string.IsNullOrEmpty(methodName) && !existingMethods.Contains(methodName))
                {
                    newCallbackCode += methodSignature;
                    newCallbackCode += "    {\n";
                    newCallbackCode += "        \n";
                    newCallbackCode += "    }\n\n";
                    Debug.Log($"添加新回调方法: {methodName}");
                }
                else if (existingMethods.Contains(methodName))
                {
                    Debug.Log($"跳过已存在的回调方法: {methodName}");
                }
            }
        }
        
        // 在类的末尾（最后一个}之前）添加方法
        int lastBraceIndex = templateContent.LastIndexOf('}');
        if (lastBraceIndex > 0)
        {
            string methodsToAdd = "";
            
            // 先添加ChangeLanguage方法（如果存在Txt组件）
            if (!string.IsNullOrEmpty(changeLanguageCode))
            {
                // 如果已存在ChangeLanguage，先删除旧的
                string oldChangeLangPattern = @"private\s+void\s+ChangeLanguage\(\)\s*\{[\s\S]*?\n\s*\}";
                templateContent = Regex.Replace(templateContent, oldChangeLangPattern, "");
                
                methodsToAdd += changeLanguageCode + "\n";
                Debug.Log("ChangeLanguage方法已生成/更新");
            }
            
            // 再添加新的回调方法
            if (!string.IsNullOrEmpty(newCallbackCode))
            {
                methodsToAdd += newCallbackCode;
            }
            
            if (!string.IsNullOrEmpty(methodsToAdd))
            {
                templateContent = templateContent.Insert(lastBraceIndex, "\n" + methodsToAdd.TrimEnd() + "\n");
            }
        }
        
        Debug.Log($"最终Model脚本内容长度: {templateContent.Length}");
        Debug.Log("========== Model脚本生成完成 ==========");
        return templateContent;
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
            // 替换占位符 {PANEL_NAME} 为完整的面板名称（包含Panel后缀）
            // 先替换 {PANEL_NAME}Panel，再替换 {PANEL_NAME}，确保都能正确替换
            templateContent = templateContent.Replace("{PANEL_NAME}Panel", panelName);
            templateContent = templateContent.Replace("{PANEL_NAME}", panelName);
            return templateContent;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取模板文件失败: {e.Message}");
            return $"// 错误：读取模板文件失败 {e.Message}";
        }
    }
    
    
    
    private void LoadSettings()
    {
        scriptOutputPath = EditorPrefs.GetString("AutoUI_ScriptOutputPath", "Assets/Scripts/DMT_ACT_Implement/UI");
    }
    
    private void SaveSettings()
    {
        EditorPrefs.SetString("AutoUI_ScriptOutputPath", scriptOutputPath);
    }
}
#endif

