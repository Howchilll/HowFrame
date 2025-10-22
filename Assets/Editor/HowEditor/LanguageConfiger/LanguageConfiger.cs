#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;

/// <summary>
/// 多语言系统编辑器工具
/// </summary>
public class LanguageConfiger : EditorWindow
{
    // === 内部枚举类型 ===
    public enum Language
    {
        English,
        French,
        Malayu,
        Chinese,
    }

    public enum Module
    {
        UI,
        Default,
        ItemInfo,
        Scene,
    }

    private Language selectedLanguage = Language.English;
    private Module selectedModule = Module.UI;
    private string outputFolder = "Assets/LangOutput";
    private Vector2 scrollPos;
    private string searchFilter = "";
    private bool showOnlyEmpty = false;

    // 当前扫描的词条
    private Dictionary<string, string> langEntries = new();

    // 本地缓存的旧文件数据
    private Dictionary<string, string> cachedEntries = new();

    private const string EditorPrefsKey = "LanguageConfiger_State";

    [MenuItem("Tools/Language Helper")]
    public static void OpenWindow()
    {
        var window = GetWindow<LanguageConfiger>("Language Helper");
        window.LoadEditorPrefs(); // 打开时加载编辑器状态
    }

    private void OnDisable()
    {
        SaveEditorPrefs(); // 关闭时保存编辑器状态
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("多语言系统工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        selectedLanguage = (Language)EditorGUILayout.EnumPopup("Language", selectedLanguage);
        selectedModule = (Module)EditorGUILayout.EnumPopup("Lang Module", selectedModule);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("扫描项目", GUILayout.Height(30)))
        {
            ScanProject();
            LoadCachedEntries();
            MergeCachedEntries();
        }

        if (GUILayout.Button("输出文件", GUILayout.Height(30)))
        {
            ExportJson();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        
        // 添加搜索和过滤功能
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        showOnlyEmpty = EditorGUILayout.Toggle("仅显示空值", showOnlyEmpty, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        // 获取过滤后的键列表
        var filteredKeys = GetFilteredKeys();
        EditorGUILayout.LabelField($"扫描结果 ({filteredKeys.Count}/{langEntries.Count} 条):", EditorStyles.boldLabel);
        
        // 使用更紧凑的布局和更好的滚动体验
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
        
        for (int i = 0; i < filteredKeys.Count; i++)
        {
            var key = filteredKeys[i];
            var value = langEntries[key];
            bool isEmpty = string.IsNullOrEmpty(value);
            
            // 根据是否为空值设置不同的背景色
            Color originalColor = GUI.backgroundColor;
            if (isEmpty)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f, 0.3f); // 浅红色背景
            }
            
            // 使用更紧凑的布局
            EditorGUILayout.BeginHorizontal();
            
            // 键名标签 - 固定宽度，显示空值状态
            string keyLabel = isEmpty ? $"{key} (空)" : key;
            EditorGUILayout.LabelField(keyLabel, GUILayout.Width(200), GUILayout.ExpandWidth(false));
            
            // 值输入框 - 占用剩余空间
            langEntries[key] = EditorGUILayout.TextField(value, GUILayout.ExpandWidth(true));
            
            // 删除按钮 - 紧凑布局
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(18)))
            {
                langEntries.Remove(key);
                break; // 退出循环，避免修改集合时的问题
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 恢复原始背景色
            GUI.backgroundColor = originalColor;
            
            // 添加分隔线（可选）
            if (i < filteredKeys.Count - 1)
            {
                EditorGUILayout.Space(1);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 扫描项目中所有 GetLangContent 调用
    /// </summary>
    private void ScanProject()
    {
        langEntries.Clear();
        string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        Regex regex = new Regex(
            @"GetLangContent\s*\(\s*(?:(?:[\w\.]+\.)?LangModuleEnum\.(\w+)\s*,\s*)?""([^""]+)""\s*\)"
        );

        foreach (var file in csFiles)
        {
            string content = File.ReadAllText(file);
            MatchCollection matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                string module = match.Groups[1].Success && !string.IsNullOrEmpty(match.Groups[1].Value)
                    ? match.Groups[1].Value
                    : "Default"; // 没写模块参数的，默认 Default

                string key = match.Groups[2].Value;

                if (module.Equals(selectedModule.ToString(), System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!langEntries.ContainsKey(key))
                        langEntries[key] = "";
                }
            }
        }

        Debug.Log($"扫描完成，共找到 {langEntries.Count} 个键。");
    }

    /// <summary>
    /// 从本地 JSON 读取缓存内容
    /// </summary>
    private void LoadCachedEntries()
    {
        cachedEntries.Clear();

        string fileName = $"{selectedLanguage}_{selectedModule}_Lang.json";
        string op = ResolvePath(outputFolder);
        
        string fullPath = Path.Combine(op, fileName);

        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                cachedEntries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (cachedEntries == null)
                    cachedEntries = new();

                Debug.Log($"已读取本地缓存：{fileName}（{cachedEntries.Count} 条）");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"读取本地语言文件失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 将缓存值合并进当前扫描结果
    /// </summary>
    private void MergeCachedEntries()
    {
        foreach (var kvp in cachedEntries)
        {
            if (langEntries.ContainsKey(kvp.Key) && string.IsNullOrEmpty(langEntries[kvp.Key]))
            {
                langEntries[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// 导出 JSON 文件
    /// </summary>
    private void ExportJson()
    {
        if (langEntries.Count == 0)
        {
            Debug.LogWarning("没有可导出的语言数据。请先扫描。");
            return;
        }

        string op = ResolvePath(outputFolder);
       
        if (!Directory.Exists(op))
        {
            Directory.CreateDirectory(op);
        }

        string fileName = $"{selectedLanguage}_{selectedModule}_Lang.json";
        string fullPath = Path.Combine(op, fileName);

        string json = JsonConvert.SerializeObject(langEntries, Formatting.Indented);
        File.WriteAllText(fullPath, json);

        Debug.Log($"导出成功：{fullPath}");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 保存编辑器状态（持久化）
    /// </summary>
    private void SaveEditorPrefs()
    {
        var state = new
        {
            selectedLanguage = selectedLanguage.ToString(),
            selectedModule = selectedModule.ToString(),
            outputFolder = outputFolder
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

            if (state.TryGetValue("selectedLanguage", out string lang))
                selectedLanguage = (Language)System.Enum.Parse(typeof(Language), lang);

            if (state.TryGetValue("selectedModule", out string mod))
                selectedModule = (Module)System.Enum.Parse(typeof(Module), mod);

            if (state.TryGetValue("outputFolder", out string path))
                outputFolder = path;
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
    
    /// <summary>
    /// 获取过滤后的键列表
    /// </summary>
    private List<string> GetFilteredKeys()
    {
        var keys = new List<string>(langEntries.Keys);
        
        // 应用搜索过滤
        if (!string.IsNullOrEmpty(searchFilter))
        {
            keys = keys.Where(k => k.ToLower().Contains(searchFilter.ToLower()) || 
                                  langEntries[k].ToLower().Contains(searchFilter.ToLower())).ToList();
        }
        
        // 应用空值过滤
        if (showOnlyEmpty)
        {
            keys = keys.Where(k => string.IsNullOrEmpty(langEntries[k])).ToList();
        }
        
        return keys;
    }
}
#endif
