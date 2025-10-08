#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FileMover : EditorWindow
{
    [System.Serializable]
    public class FileEntry
    {
        public string From = "";
        public string To = "";
    }

    [System.Serializable]
    private class FileMoverConfig
    {
        public List<FileEntry> Entries = new List<FileEntry>();
    }

    private const string EditorPrefsKey = "FileMover_Config";
    private FileMoverConfig config = new FileMoverConfig();
    private Vector2 scrollPos;

    [MenuItem("Tools/File Mover")]
    public static void ShowWindow()
    {
        GetWindow<FileMover>("File Mover");
    }

    private void OnEnable()
    {
        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            string json = EditorPrefs.GetString(EditorPrefsKey);
            config = JsonUtility.FromJson<FileMoverConfig>(json) ?? new FileMoverConfig();
        }
    }

    private void OnDisable()
    {
        string json = JsonUtility.ToJson(config);
        EditorPrefs.SetString(EditorPrefsKey, json);
    }

    private void OnGUI()
    {
        GUILayout.Label("文件迁移工具", EditorStyles.boldLabel);
        GUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < config.Entries.Count; i++)
        {
            var entry = config.Entries[i];
            EditorGUILayout.BeginHorizontal();

            entry.From = EditorGUILayout.TextField(entry.From);
            entry.To = EditorGUILayout.TextField(entry.To);

            if (GUILayout.Button("Move", GUILayout.Width(50)))
            {
                MoveFileOrDirectory(entry.From, entry.To);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                DeleteFileOrDirectory(entry.From);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                config.Entries.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加条目"))
        {
            config.Entries.Add(new FileEntry());
        }

        if (GUILayout.Button("一键执行全部 Move"))
        {
            foreach (var entry in config.Entries)
            {
                MoveFileOrDirectory(entry.From, entry.To);
            }
        }

        if (GUILayout.Button("一键执行全部 Delete"))
        {
            foreach (var entry in config.Entries)
            {
                DeleteFileOrDirectory(entry.From);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private string ResolvePath(string rawPath)
    {
        if (!string.IsNullOrEmpty(rawPath) && rawPath.Contains("."))
        {
            return PathEditor.FindPath(rawPath);
        }
        return rawPath;
    }

    private void MoveFileOrDirectory(string fromRaw, string toRaw)
    {
        string from = ResolvePath(fromRaw);
        string to = ResolvePath(toRaw);

        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            Debug.LogWarning("路径不能为空！");
            return;
        }

        if (!File.Exists(from) && !Directory.Exists(from))
        {
            Debug.LogWarning($"源路径不存在: {from}");
            return;
        }

        try
        {
            string toDir = Path.GetDirectoryName(to);
            if (!string.IsNullOrEmpty(toDir) && !Directory.Exists(toDir))
            {
                Directory.CreateDirectory(toDir);
            }

            if (File.Exists(from))
                File.Move(from, to);
            else if (Directory.Exists(from))
                Directory.Move(from, to);

            Debug.Log($"已移动: {from} -> {to}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"移动失败: {from} -> {to}\n{e}");
        }
    }

    private void DeleteFileOrDirectory(string rawPath)
    {
        string path = ResolvePath(rawPath);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("路径不能为空！");
            return;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Debug.LogWarning($"路径不存在: {path}");
            return;
        }

        try
        {
            if (File.Exists(path))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);

            Debug.Log($"已删除: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"删除失败: {path}\n{e}");
        }
    }
}
#endif