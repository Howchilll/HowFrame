#define EDITOR
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using HowEnum;

/// <summary>
/// 多语言系统编辑器工具（基于 Excel 管理）
/// 动态从 LangTypeEnum 和 LangModuleEnum 读取配置
/// </summary>
public class LanguageConfiger : EditorWindow
{
    private string excelFolder = "EditorPath.LanguageExcelPath";
    private string jsonOutputFolder = "EditorPath.LanguageJsonPath";

    private const string EditorPrefsKey = "LanguageConfiger_State";
    
    public static void OpenWindow()
    {
        var window = GetWindow<LanguageConfiger>("Language Helper");
        window.LoadEditorPrefs();
    }

    private void OnDisable()
    {
        SaveEditorPrefs();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("多语言系统工具（Excel 管理）", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox("语言配置现在通过 Excel 文件管理。\n1. 点击「扫描并更新 Excel」扫描项目并更新 Excel 文件\n2. 在 Excel 中编辑语言内容\n3. 点击「转换所有语言配置文件生成 JSON」生成 JSON 文件", MessageType.Info);
        EditorGUILayout.Space(10);

        excelFolder = EditorGUILayout.TextField("Excel 文件夹路径", excelFolder);
        jsonOutputFolder = EditorGUILayout.TextField("JSON 输出文件夹", jsonOutputFolder);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("扫描并更新 Excel", GUILayout.Height(40)))
        {
            ScanAndUpdateAllExcels();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("转换所有语言配置文件生成 JSON", GUILayout.Height(40)))
        {
            ConvertAllExcelsToJson();
        }
    }

    /// <summary>
    /// 扫描项目中所有 GetLangContent 调用（针对指定模块）
    /// </summary>
    private Dictionary<string, string> ScanProjectForModule(string moduleName)
    {
        var langEntries = new Dictionary<string, string>();
        string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        // 扫描固定字符串的GetLangContent调用
        Regex regex = new Regex(
            @"GetLangContent\s*\(\s*(?:(?:[\w\.]+\.)?LangModuleEnum\.(\w+)\s*,\s*)?""([^""]+)""\s*\)"
        );
        
        // 扫描注释中的语言键定义
        Regex commentRegex = new Regex(
            @"//\s*GetLangContent\s*:\s*(\w+)\s*,\s*\{([^}]+)\}",
            RegexOptions.IgnoreCase
        );

        foreach (var file in csFiles)
        {
            string content = File.ReadAllText(file);
            
            // 扫描固定字符串的GetLangContent调用
            MatchCollection matches = regex.Matches(content);
            foreach (Match match in matches)
            {
                string scannedModuleName = match.Groups[1].Success && !string.IsNullOrEmpty(match.Groups[1].Value)
                    ? match.Groups[1].Value
                    : "Default"; // 没写模块参数的，默认 Default

                string key = match.Groups[2].Value;

                if (scannedModuleName.Equals(moduleName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!langEntries.ContainsKey(key))
                        langEntries[key] = "";
                }
            }
            
            // 扫描注释中的语言键定义
            MatchCollection commentMatches = commentRegex.Matches(content);
            foreach (Match commentMatch in commentMatches)
            {
                string scannedModuleName = commentMatch.Groups[1].Value;
                string keysString = commentMatch.Groups[2].Value;
                
                if (scannedModuleName.Equals(moduleName, System.StringComparison.OrdinalIgnoreCase))
                {
                    // 解析键列表，支持 "str1","str2" 格式
                    string[] keys = keysString.Split(',');
                    foreach (string key in keys)
                    {
                        string cleanKey = key.Trim().Trim('"', '\'', ' ');
                        if (!string.IsNullOrEmpty(cleanKey) && !langEntries.ContainsKey(cleanKey))
                        {
                            langEntries[cleanKey] = "";
                        }
                    }
                }
            }
        }

        return langEntries;
    }

    /// <summary>
    /// 扫描并更新所有 Excel 文件
    /// </summary>
    private void ScanAndUpdateAllExcels()
    {
        string excelPath = ResolvePath(excelFolder);
        if (!Directory.Exists(excelPath))
        {
            Directory.CreateDirectory(excelPath);
        }

        // 动态获取所有语言和模块
        var allLanguages = LangTypeEnum.GetAll();
        var allModules = LangModuleEnum.GetAll();

        if (allLanguages == null || allLanguages.Count == 0)
        {
            Debug.LogError("❌ 无法获取语言列表，请检查 LangTypeEnum 配置");
            return;
        }

        if (allModules == null || allModules.Count == 0)
        {
            Debug.LogError("❌ 无法获取模块列表，请检查 LangModuleEnum 配置");
            return;
        }

        int totalUpdated = 0;

        // 遍历所有语言和模块组合
        foreach (var langKey in allLanguages)
        {
            string langName = langKey.name;
            
            foreach (var moduleKey in allModules)
            {
                string moduleName = moduleKey.name;
                
                // 扫描项目获取该模块的所有键
                var scannedKeys = ScanProjectForModule(moduleName);
                
                if (scannedKeys.Count == 0)
                    continue;

                // 构建 Excel 文件路径
                string excelFileName = $"{langName}_{moduleName}_Lang.xlsx";
                string excelFilePath = Path.Combine(excelPath, excelFileName);

                // 更新 Excel（只新建新项，删除没有了的项，不改变现有项的赋值）
                LanguageExcelSerializer.UpdateExcel(excelFilePath, scannedKeys);
                totalUpdated++;
            }
        }

        Debug.Log($"✅ 扫描并更新完成！共更新 {totalUpdated} 个 Excel 文件");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 转换所有语言 Excel 文件生成 JSON
    /// </summary>
    private void ConvertAllExcelsToJson()
    {
        string excelPath = ResolvePath(excelFolder);
        string jsonPath = ResolvePath(jsonOutputFolder);

        if (!Directory.Exists(excelPath))
        {
            Debug.LogError($"Excel 文件夹不存在: {excelPath}");
            return;
        }

        if (!Directory.Exists(jsonPath))
        {
            Directory.CreateDirectory(jsonPath);
        }

        // 动态获取所有语言和模块
        var allLanguages = LangTypeEnum.GetAll();
        var allModules = LangModuleEnum.GetAll();

        if (allLanguages == null || allLanguages.Count == 0)
        {
            Debug.LogError("❌ 无法获取语言列表，请检查 LangTypeEnum 配置");
            return;
        }

        if (allModules == null || allModules.Count == 0)
        {
            Debug.LogError("❌ 无法获取模块列表，请检查 LangModuleEnum 配置");
            return;
        }

        int totalConverted = 0;

        // 遍历所有语言和模块组合
        foreach (var langKey in allLanguages)
        {
            string langName = langKey.name;
            
            foreach (var moduleKey in allModules)
            {
                string moduleName = moduleKey.name;
                
                // 构建 Excel 文件路径
                string excelFileName = $"{langName}_{moduleName}_Lang.xlsx";
                string excelFilePath = Path.Combine(excelPath, excelFileName);

                if (!File.Exists(excelFilePath))
                {
                    continue;
                }

                // 从 Excel 读取数据
                var langData = LanguageExcelSerializer.ExcelToJsonDict(excelFilePath);

                if (langData.Count == 0)
                {
                    continue;
                }

                // 生成 JSON 文件
                string jsonFileName = $"{langName}_{moduleName}_Lang.json";
                string jsonFilePath = Path.Combine(jsonPath, jsonFileName);

                string json = JsonConvert.SerializeObject(langData, Formatting.Indented);
                File.WriteAllText(jsonFilePath, json);
                totalConverted++;

                Debug.Log($"✅ 转换完成: {jsonFileName} ({langData.Count} 条)");
            }
        }

        Debug.Log($"✅ 所有语言配置文件转换完成！共转换 {totalConverted} 个 JSON 文件");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 保存编辑器状态（持久化）
    /// </summary>
    private void SaveEditorPrefs()
    {
        var state = new
        {
            excelFolder = excelFolder,
            jsonOutputFolder = jsonOutputFolder
        };
        string json = JsonConvert.SerializeObject(state);
        EditorPrefs.SetString(EditorPrefsKey, json);
    }

    /// <summary>
    /// 加载编辑器状态
    /// </summary>
    private void LoadEditorPrefs()
    {
        if (!EditorPrefs.HasKey(EditorPrefsKey)) return;
        try
        {
            var json = EditorPrefs.GetString(EditorPrefsKey);
            var state = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (state.TryGetValue("excelFolder", out string excelPath))
                excelFolder = excelPath;

            if (state.TryGetValue("jsonOutputFolder", out string jsonPath))
                jsonOutputFolder = jsonPath;
        }
        catch { }
    }
    
    private string ResolvePath(string pathSetting)
    {
        if (!string.IsNullOrEmpty(pathSetting) && pathSetting.Contains("."))
        {
            return PathEditor.FindPath(pathSetting);
        }
        return pathSetting;
    }
}
#endif
