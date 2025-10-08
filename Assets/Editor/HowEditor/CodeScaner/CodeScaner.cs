#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class CodeScanner : EditorWindow
{
    private string folderPath = "Assets/Scripts";  // 扫描目录
    private string pattern = @"LanguageManager\.LanguageDic\[""(.*?)""\]"; // 正则
    private string outputDir = "Assets"; // 输出上级目录
    private string outputFileName = "LanguageKeys"; // 文件名（不带后缀）
    
    [MenuItem("Tools/Code Scanner")]
    public static void ShowWindow()
    {
        GetWindow<CodeScanner>("Code Scanner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Code Scanner Settings", EditorStyles.boldLabel);
        
        // 扫描目录
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Folder to Scan", GUILayout.Width(100));
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Folder to Scan", folderPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                    folderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                else
                    folderPath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        // 正则
        pattern = EditorGUILayout.TextField("Regex Pattern", pattern);

        // 输出目录
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Output Directory", GUILayout.Width(100));
        outputDir = EditorGUILayout.TextField(outputDir);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Output Directory", outputDir, "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                    outputDir = "Assets" + selected.Substring(Application.dataPath.Length);
                else
                    outputDir = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        // 文件名（不带后缀）
        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        if (GUILayout.Button("Scan and Export"))
        {
            ScanAndExport();
        }
    }

    private void ScanAndExport()
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Folder not found: {folderPath}");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        HashSet<string> keys = new HashSet<string>();
        Regex regex = new Regex(pattern);

        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            MatchCollection matches = regex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string key = match.Groups[1].Value;
                    keys.Add(key);
                }
            }
        }

        string json = JsonUtility.ToJson(new SerializableList(keys), true);

        // 拼接最终输出路径
        string fullPath = Path.Combine(outputDir, outputFileName + ".json");

        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"Scan complete! Found {keys.Count} keys. JSON saved to {fullPath}");
    }

    [System.Serializable]
    private class SerializableList
    {
        public List<string> items = new List<string>();
        public SerializableList(IEnumerable<string> collection) => items.AddRange(collection);
    }
}
#endif
