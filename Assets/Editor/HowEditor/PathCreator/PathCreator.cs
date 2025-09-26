using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

public class PathCreator : EditorWindow
{
    [Serializable]
    public class PathEntry
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public class PathConfig
    {
        public string Namespace = "HowFrame";
        public string ClassName = "HowPath";
        public string JsonDir = "Assets/Config/";   // json目录
        public string CsDir = "Assets/Scripts/Generated/"; // cs目录
        public List<PathEntry> Entries = new List<PathEntry>();
    }

    private PathConfig config = new PathConfig();
    private Vector2 scrollPos;

    [MenuItem("Tools/Path Creator")]
    public static void ShowWindow()
    {
        GetWindow<PathCreator>("Path Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("基本设置", EditorStyles.boldLabel);
        config.Namespace = EditorGUILayout.TextField("命名空间", config.Namespace);
        config.ClassName = EditorGUILayout.TextField("类名 (Json 和 CS 共用)", config.ClassName);
        config.JsonDir = EditorGUILayout.TextField("Json 输出目录", config.JsonDir);
        config.CsDir = EditorGUILayout.TextField("C# 输出目录", config.CsDir);

        GUILayout.Space(10);

        GUILayout.Label("路径定义", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < config.Entries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            config.Entries[i].Key = EditorGUILayout.TextField(config.Entries[i].Key, GUILayout.Width(150));
            config.Entries[i].Value = EditorGUILayout.TextField(config.Entries[i].Value);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                config.Entries.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("添加字段"))
        {
            config.Entries.Add(new PathEntry() { Key = "Path1", Value = "\"Assets/SomePath\"" });
        }

        GUILayout.Space(10);

        if (GUILayout.Button("保存配置并生成"))
        {
            SaveConfig();
            GenerateCsFile();
        }

        if (GUILayout.Button("加载配置"))
        {
            LoadConfig();
        }
    }

    private string GetJsonPath() => Path.Combine(config.JsonDir, config.ClassName + ".json");
    private string GetCsPath() => Path.Combine(config.CsDir, config.ClassName + ".cs");

    private void SaveConfig()
    {
        string jsonPath = GetJsonPath();
        Directory.CreateDirectory(config.JsonDir);
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(jsonPath, json, Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("已保存 Json 配置: " + jsonPath);
    }

    private void LoadConfig()
    {
        string jsonPath = GetJsonPath();
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath, Encoding.UTF8);
            config = JsonUtility.FromJson<PathConfig>(json);
        }
        else
        {
            Debug.LogWarning("找不到 Json 配置文件: " + jsonPath);
        }
    }

    private void GenerateCsFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace " + config.Namespace);
        sb.AppendLine("{");
        sb.AppendLine("    public static class " + config.ClassName);
        sb.AppendLine("    {");

        foreach (var entry in config.Entries)
        {
            if (string.IsNullOrEmpty(entry.Key)) continue;
            sb.AppendLine($"        public static readonly string {entry.Key} = {entry.Value};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        string csPath = GetCsPath();
        Directory.CreateDirectory(config.CsDir);
        File.WriteAllText(csPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("已生成 C# 文件: " + csPath);
    }
}
