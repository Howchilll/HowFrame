#define EDITOR
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class EnumCreator : EditorWindow
{
    private EnumRoot root = new EnumRoot();
    private Vector2 scrollPos;

    private static HowFrameSettings Settings => HowFrameSettings.instance;

    // 窗口编辑中的草稿（个人状态，不进版本库）
    private static string DraftPath => "Library/HowFrame/EnumCreatorDraft.json";

    [MenuItem("Tools/Enum Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<EnumCreator>("Enum Creator");
        window.LoadEditorConfig();
    }

    private void OnDisable()
    {
        SaveEditorConfig(); // 窗口关闭时保存草稿
    }

    private void OnGUI()
    {
        GUILayout.Label("Enum 定义", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        root.collectionName = EditorGUILayout.TextField("enum名字", root.collectionName);
        if (GUILayout.Button("加载", GUILayout.Width(60)))
        {
            EnsureEnumSuffix();
            LoadJson();
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("输出参数（来自 HowFrame 设置）", EditorStyles.boldLabel);
        HowFrameEditorGUI.PathInfo("JSON 目录", Settings.enumJsonDir);
        HowFrameEditorGUI.PathInfo("C# 输出目录", Settings.enumCsDir);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("命名空间", Settings.enumNamespace);
            EditorGUILayout.Toggle("生成 Convert 方法", Settings.enumGenerateConvert);
            EditorGUILayout.Toggle("生成 GetAll 方法", Settings.enumGenerateGetAll);
        }
        HowFrameEditorGUI.SettingsButton();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawElements(root.elements, 0);
        EditorGUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("一键导出 JSON + C# 脚本"))
        {
            EnsureEnumSuffix();
            ExportAll();
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();
    }

    private void EnsureEnumSuffix()
    {
        if (!string.IsNullOrEmpty(root.collectionName) && !root.collectionName.EndsWith("Enum"))
            root.collectionName += "Enum";
    }

    private void ExportAll()
    {
        if (string.IsNullOrEmpty(root.collectionName))
        {
            Debug.LogWarning("请先填写枚举名字，生成类名将用枚举名字！");
            return;
        }

        string resolvedJsonDir = HowFrameSettings.ToFullPath(Settings.enumJsonDir);
        if (!Directory.Exists(resolvedJsonDir))
            Directory.CreateDirectory(resolvedJsonDir);

        string jsonPath = Path.Combine(resolvedJsonDir, root.collectionName + ".json");
        string json = EnumHelper.Serialize(root);
        File.WriteAllText(jsonPath, json);
        Debug.Log("JSON 已保存到: " + jsonPath);

        string resolvedCsDir = HowFrameSettings.ToFullPath(Settings.enumCsDir);
        if (!Directory.Exists(resolvedCsDir))
            Directory.CreateDirectory(resolvedCsDir);

        EnumGenerater.Generate(root, Settings.enumNamespace, root.collectionName, resolvedCsDir, Settings.enumGenerateConvert, Settings.enumGenerateGetAll);
        Debug.Log("C# Enum 脚本已生成到: " + resolvedCsDir);
    }

    private void LoadJson()
    {
        string resolvedJsonDir = HowFrameSettings.ToFullPath(Settings.enumJsonDir);
        string path = Path.Combine(resolvedJsonDir, root.collectionName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("未找到文件: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        root = EnumHelper.Deserialize(json);
        Debug.Log("JSON 已加载: " + path);
    }

    // --- 编辑器持久化 ---
    private void SaveEditorConfig()
    {
        string json = EnumHelper.Serialize(root);
        string dir = Path.GetDirectoryName(DraftPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(DraftPath, json);
    }

    private void LoadEditorConfig()
    {
        if (!File.Exists(DraftPath)) return;

        string json = File.ReadAllText(DraftPath);
        root = EnumHelper.Deserialize(json);
    }

    // DrawElements 保持不变
    private void DrawElements(List<EnumElement> elements, int depth)
    {
        int removeIndex = -1;

        for (int i = 0; i < elements.Count; i++)
        {
            var elem = elements[i];

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.Lerp(Color.white, Color.gray, depth * 0.12f);

            EditorGUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * 20);

            elem.isList = EditorGUILayout.Toggle(elem.isList, GUILayout.Width(20));

            if (!elem.isList)
            {
                elem.value = EditorGUILayout.TextField(elem.value);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                    removeIndex = i;
            }
            else
            {
                elem.foldout = EditorGUILayout.Foldout(elem.foldout, "子集合", true);
                elem.groupName = EditorGUILayout.TextField(elem.groupName);

                if (GUILayout.Button("+", GUILayout.Width(25)))
                    elem.children.Add(new EnumElement());

                if (GUILayout.Button("X", GUILayout.Width(25)))
                    removeIndex = i;
            }

            GUILayout.EndHorizontal();

            if (elem.isList && elem.foldout)
                DrawElements(elem.children, depth + 1);

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = oldColor;
        }

        if (removeIndex >= 0)
            elements.RemoveAt(removeIndex);

        if (GUILayout.Button("添加元素"))
            elements.Add(new EnumElement());
    }
}
#endif