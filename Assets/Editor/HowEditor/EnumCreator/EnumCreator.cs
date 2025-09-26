using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class EnumCreator : EditorWindow
{
    private EnumRoot root = new EnumRoot();

    // 可编辑的命名空间和输出路径
    private string enumNamespace = "HowEnum";
    private string jsonOutputDir = "Assets/JsonData";
    private string csOutputDir = "Assets/GeneratedEnum";

    private Vector2 scrollPos;

    [MenuItem("Tools/EnumCreator")]
    public static void ShowWindow()
    {
        var window = GetWindow<EnumCreator>("Json Collection Editor");

        // EditorPrefs 恢复
        window.enumNamespace = EditorPrefs.GetString("EnumCreator_Namespace", "HowEnum");
        window.jsonOutputDir = EditorPrefs.GetString("EnumCreator_JsonOutputDir", "Assets/JsonData");
        window.csOutputDir = EditorPrefs.GetString("EnumCreator_CSOutputDir", "Assets/GeneratedEnum");
    }

    private void OnGUI()
    {
        GUILayout.Label("JSON 集合编辑器", EditorStyles.boldLabel);

        // 集合名字 + 加载按钮
        GUILayout.BeginHorizontal();
        root.collectionName = EditorGUILayout.TextField("集合名字", root.collectionName);
        if (GUILayout.Button("加载", GUILayout.Width(60)))
        {
            LoadJson();
        }
        GUILayout.EndHorizontal();

        // 命名空间 + 输出路径
        GUILayout.Label("C# Enum 生成参数", EditorStyles.boldLabel);
        enumNamespace = EditorGUILayout.TextField("命名空间", enumNamespace);
        jsonOutputDir = EditorGUILayout.TextField("JSON 输出路径", jsonOutputDir);
        csOutputDir = EditorGUILayout.TextField("C# 输出路径", csOutputDir);

        // 保存到 EditorPrefs
        EditorPrefs.SetString("EnumCreator_Namespace", enumNamespace);
        EditorPrefs.SetString("EnumCreator_JsonOutputDir", jsonOutputDir);
        EditorPrefs.SetString("EnumCreator_CSOutputDir", csOutputDir);

        // 元素编辑器
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawElements(root.elements, 0);
        EditorGUILayout.EndScrollView();

        // 按钮
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("一键导出 JSON + C# 脚本"))
        {
            if (string.IsNullOrEmpty(root.collectionName))
            {
                Debug.LogWarning("请先填写集合名字，生成类名将使用集合名字！");
                return;
            }

            // --- 写 JSON ---
            if (!Directory.Exists(jsonOutputDir))
                Directory.CreateDirectory(jsonOutputDir);

            string jsonPath = Path.Combine(jsonOutputDir, root.collectionName + ".json");
            string json = EnumHelper.Serialize(root);
            File.WriteAllText(jsonPath, json);
            Debug.Log("JSON 已保存到: " + jsonPath);

            // --- 生成 C# 脚本 ---
            if (!Directory.Exists(csOutputDir))
                Directory.CreateDirectory(csOutputDir);

            EnumGenerater.Generate(root, enumNamespace, root.collectionName, csOutputDir);
            Debug.Log("C# Enum 脚本已生成到: " + csOutputDir);
        }
        GUILayout.EndHorizontal();
        AssetDatabase.Refresh();
    }
    
    private void LoadJson()
    {
        string path = Path.Combine(jsonOutputDir, root.collectionName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("未找到文件: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        root = EnumHelper.Deserialize(json);
        Debug.Log("JSON 已加载: " + path);
    }

    // DrawElements 保持之前实现（支持折叠和嵌套）
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

            // 是否集合
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
