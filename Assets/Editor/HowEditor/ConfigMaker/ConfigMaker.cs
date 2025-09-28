using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

public class ConfigMaker : EditorWindow
{
    private string typeName = "";
    private string saveDir = "Assets/Config/";
    private string fileName = "NewConfig.json";

    private Type targetType;
    private JObject jsonObject; // 当前编辑数据
    private Vector2 scrollPos;

    [MenuItem("Tools/ConfigMaker")]
    public static void ShowWindow()
    {
        GetWindow<ConfigMaker>("ConfigMaker");
    }

    private void OnGUI()
    {
        GUILayout.Label("配置生成器", EditorStyles.boldLabel);

        typeName = EditorGUILayout.TextField("类名 / 结构体名", typeName);
        saveDir = EditorGUILayout.TextField("保存目录", saveDir);
        fileName = EditorGUILayout.TextField("文件名", fileName);

        EditorGUILayout.Space();

        if (GUILayout.Button("加载类型"))
        {
            LoadType();
        }

        if (jsonObject != null)
        {
            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawJsonEditor(jsonObject, targetType);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("保存 JSON"))
            {
                SaveJson();
            }
        }
    }

    private void LoadType()
    {
        if (string.IsNullOrEmpty(typeName))
        {
            Debug.LogError("请输入类名或结构体名");
            return;
        }

        targetType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

        if (targetType == null)
        {
            Debug.LogError($"未找到类型: {typeName}");
            return;
        }

        // 创建空实例
        var instance = Activator.CreateInstance(targetType);
        string json = JsonConvert.SerializeObject(instance, Formatting.Indented);
        jsonObject = JObject.Parse(json);
    }

    private void SaveJson()
    {
        Directory.CreateDirectory(saveDir);
        string path = Path.Combine(saveDir, fileName);
        File.WriteAllText(path, jsonObject.ToString(Formatting.Indented));
        AssetDatabase.Refresh();
        Debug.Log($"✅ 已生成 JSON: {path}");
    }

 private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

private void DrawJsonEditor(JObject jObject, Type type, string parentKey = "")
{
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
    {
        JToken token = jObject[field.Name];
        Type fieldType = field.FieldType;
        string foldoutKey = parentKey + "." + field.Name;

        // 嵌套类或者List用折叠
        if (fieldType.IsClass && fieldType != typeof(string))
        {
            if (!foldouts.ContainsKey(foldoutKey)) foldouts[foldoutKey] = true;
            foldouts[foldoutKey] = EditorGUILayout.Foldout(foldouts[foldoutKey], field.Name);
            if (foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elementType = fieldType.GetGenericArguments()[0];
                    JArray array = token as JArray ?? new JArray();
                    jObject[field.Name] = DrawJsonArray(array, elementType, foldoutKey);
                }
                else
                {
                    JObject nestedObj = token as JObject ?? new JObject();
                    jObject[field.Name] = nestedObj;
                    DrawJsonEditor(nestedObj, fieldType, foldoutKey);
                }
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.Name, GUILayout.Width(150));
            // 基础类型处理
            if (fieldType == typeof(string))
                jObject[field.Name] = EditorGUILayout.TextField(token?.ToString() ?? "");
            else if (fieldType == typeof(int))
                jObject[field.Name] = EditorGUILayout.IntField(token?.ToObject<int>() ?? 0);
            else if (fieldType == typeof(float))
                jObject[field.Name] = EditorGUILayout.FloatField(token?.ToObject<float>() ?? 0f);
            else if (fieldType == typeof(bool))
                jObject[field.Name] = EditorGUILayout.Toggle(token?.ToObject<bool>() ?? false);
            EditorGUILayout.EndHorizontal();
        }
    }
}

private JArray DrawJsonArray(JArray array, Type elementType, string parentKey)
{
    if (!foldouts.ContainsKey(parentKey)) foldouts[parentKey] = true;
    foldouts[parentKey] = EditorGUILayout.Foldout(foldouts[parentKey], $"List<{elementType.Name}> [{array.Count}]");
    if (foldouts[parentKey])
    {
        EditorGUI.indentLevel++;
        int newCount = EditorGUILayout.IntField("Size", array.Count);
        while (newCount > array.Count) array.Add(elementType.IsClass ? new JObject() : JToken.FromObject(Activator.CreateInstance(elementType)));
        while (newCount < array.Count) array.RemoveAt(array.Count - 1);

        for (int i = 0; i < array.Count; i++)
        {
            JToken token = array[i];
            string itemKey = parentKey + "." + i;

            if (elementType.IsClass && elementType != typeof(string))
            {
                JObject nestedObj = token as JObject ?? new JObject();
                array[i] = nestedObj;
                DrawJsonEditor(nestedObj, elementType, itemKey);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                if (elementType == typeof(string))
                    array[i] = EditorGUILayout.TextField(token?.ToString() ?? "");
                else if (elementType == typeof(int))
                    array[i] = EditorGUILayout.IntField(token?.ToObject<int>() ?? 0);
                else if (elementType == typeof(float))
                    array[i] = EditorGUILayout.FloatField(token?.ToObject<float>() ?? 0f);
                else if (elementType == typeof(bool))
                    array[i] = EditorGUILayout.Toggle(token?.ToObject<bool>() ?? false);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUI.indentLevel--;
    }
    return array;
}
}
