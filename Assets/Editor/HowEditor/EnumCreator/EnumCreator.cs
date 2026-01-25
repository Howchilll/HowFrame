#define EDITOR
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class EnumCreator : EditorWindow
{
    private EnumRoot root = new EnumRoot();

    private string enumNamespace = "HowEnum";
    private string jsonOutputDir = "Assets/Editor/HowEditorConfig";
    private string csOutputDir = "Assets/HowFrame/HowEnum/Enums";
    private bool enableConvertMethods = false;
    private bool enableGetAllMethod = false;
    private Vector2 scrollPos;

    // 配置文件存放路径（固定存到 Editor 下，避免和导出 json 混淆）
    private static string EditorConfigPath => "Assets/Editor/HowEditor/EnumCreator/EnumCreatorConfig.json";

    [MenuItem("Tools/Enum Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<EnumCreator>("Json Collection Editor");

        // EditorPrefs 恢复
        window.enumNamespace = EditorPrefs.GetString("EnumCreator_Namespace", "HowEnum");
        window.jsonOutputDir = EditorPrefs.GetString("EnumCreator_JsonOutputDir", "Assets/JsonData");
        window.csOutputDir = EditorPrefs.GetString("EnumCreator_CSOutputDir", "Assets/GeneratedEnum");
        window.enableConvertMethods = EditorPrefs.GetBool("EnumCreator_EnableConvert", false);
        window.enableGetAllMethod = EditorPrefs.GetBool("EnumCreator_EnableGetAll", false);

        // 自动加载配置
        window.LoadEditorConfig();
    }

    private void OnDisable()
    {
        SaveEditorConfig(); // 窗口关闭时保存 root
    }

    private void OnGUI()
    {
        GUILayout.Label("Enum 定义", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        root.collectionName = EditorGUILayout.TextField("enum名字", root.collectionName);
        if (GUILayout.Button("加载", GUILayout.Width(60)))
        {
            if(!root.collectionName.EndsWith("Enum"))
                 root.collectionName += "Enum";
            LoadJson();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("输出参数", EditorStyles.boldLabel);
        enumNamespace = EditorGUILayout.TextField("命名空间", enumNamespace);
        jsonOutputDir = EditorGUILayout.TextField("JSON 输出路径", jsonOutputDir);
        csOutputDir = EditorGUILayout.TextField("C# 输出路径", csOutputDir);
        enableConvertMethods = EditorGUILayout.Toggle("启用Convert方法", enableConvertMethods);
        enableGetAllMethod = EditorGUILayout.Toggle("启用GetAll方法", enableGetAllMethod);

        // 保存到 EditorPrefs
        EditorPrefs.SetString("EnumCreator_Namespace", enumNamespace);
        EditorPrefs.SetString("EnumCreator_JsonOutputDir", jsonOutputDir);
        EditorPrefs.SetString("EnumCreator_CSOutputDir", csOutputDir);
        EditorPrefs.SetBool("EnumCreator_EnableConvert", enableConvertMethods);
        EditorPrefs.SetBool("EnumCreator_EnableGetAll", enableGetAllMethod);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawElements(root.elements, 0);
        EditorGUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("一键导出 JSON + C# 脚本"))
        {   if(!root.collectionName.EndsWith("Enum"))
                root.collectionName += "Enum";
            ExportAll();
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();

     
    }

    private void ExportAll()
    {
        if (string.IsNullOrEmpty(root.collectionName))
        {
            Debug.LogWarning("请先填写枚举名字，生成类名将用枚举名字！");
            return;
        }

        string resolvedJsonDir = ResolveDir(jsonOutputDir);
        if (!Directory.Exists(resolvedJsonDir))
            Directory.CreateDirectory(resolvedJsonDir);

        string jsonPath = Path.Combine(resolvedJsonDir, root.collectionName + ".json");
        string json = EnumHelper.Serialize(root);
        File.WriteAllText(jsonPath, json);
        Debug.Log("JSON 已保存到: " + jsonPath);

        string resolvedCsDir = ResolveDir(csOutputDir);
        if (!Directory.Exists(resolvedCsDir))
            Directory.CreateDirectory(resolvedCsDir);

        EnumGenerater.Generate(root, enumNamespace, root.collectionName, resolvedCsDir, enableConvertMethods, enableGetAllMethod);
        Debug.Log("C# Enum 脚本已生成到: " + resolvedCsDir);
    }

    private void LoadJson()
    {
        string resolvedJsonDir = ResolveDir(jsonOutputDir);
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

    private string ResolveDir(string dir)
    {
        if (!string.IsNullOrEmpty(dir) && dir.Contains("."))
        {
            string resolved = PathEditor.FindPath(dir);
            if (string.IsNullOrEmpty(resolved))
            {
                Debug.LogError($"目录索引解析失败: {dir}");
                return dir;
            }
            return resolved;
        }
        return dir;
    }

    // --- 编辑器持久化 ---
    private void SaveEditorConfig()
    {
        string json = EnumHelper.Serialize(root);
        string dir = Path.GetDirectoryName(EditorConfigPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(EditorConfigPath, json);
    }

    private void LoadEditorConfig()
    {
        if (!File.Exists(EditorConfigPath)) return;

        string json = File.ReadAllText(EditorConfigPath);
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