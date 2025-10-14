#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

public class SOExporterWindow : EditorWindow
{
    private string typeName;
    private string outputFileName;
    private string outputPath;

    private bool exportJson;
    private bool exportXml;
    private bool exportBinary;

    private ScriptableObject soInstance;
    private UnityEditor.Editor soEditor;
    private Vector2 scrollPos;

    private const string PrefTypeName = "SOExporter_TypeName";
    private const string PrefOutputName = "SOExporter_OutputFileName";
    private const string PrefOutputPath = "SOExporter_OutputPath";

    [MenuItem("Tools/Config/Config Exporter")]
    public static void ShowWindow()
    {
        GetWindow<SOExporterWindow>("SO Exporter");
    }

    private void OnEnable()
    {
        // EditorPrefs 持久化读取
        typeName = EditorPrefs.GetString(PrefTypeName, "Data");
        outputFileName = EditorPrefs.GetString(PrefOutputName, "Data");
        outputPath = EditorPrefs.GetString(PrefOutputPath, "Assets/Export");
    }

    private void OnDisable()
    {
        // EditorPrefs 保存
        EditorPrefs.SetString(PrefTypeName, typeName);
        EditorPrefs.SetString(PrefOutputName, outputFileName);
        EditorPrefs.SetString(PrefOutputPath, outputPath);
    }

    private void OnGUI()
    {
        GUILayout.Label("ScriptableObject Exporter", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        typeName = EditorGUILayout.TextField("SO Type Name", typeName);
        
        if (GUILayout.Button("Create Instance", GUILayout.Width(120)))
        {
            CreateSOInstance();
        }
        EditorGUILayout.EndHorizontal();

        // 输出文件名和路径
        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        EditorGUILayout.Space();

        if (soInstance != null)
        {
            GUILayout.Label("SO Data Inspector", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
            if (soEditor == null) 
                soEditor = UnityEditor.Editor.CreateEditor(soInstance);

            soEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            GUILayout.Label("Export Options", EditorStyles.boldLabel);

            exportJson = EditorGUILayout.Toggle("Export JSON", exportJson);
            exportXml = EditorGUILayout.Toggle("Export XML", exportXml);
            exportBinary = EditorGUILayout.Toggle("Export Binary", exportBinary);

            if (GUILayout.Button("Export"))
            {
                ExportSO();
            }
        }
        else
        {
            GUILayout.Label("⚠️ 请先创建 SO 实例", EditorStyles.helpBox);
        }
    }

    private void CreateSOInstance()
    {
        Type soType = Type.GetType(typeName+"OS");

        // 扫描所有程序集
        if (soType == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                soType = asm.GetType(typeName);
                if (soType != null) break;
            }
        }

        if (soType == null)
        {
            Debug.LogError($"❌ 没找到类型 {typeName}");
            return;
        }

        if (!typeof(ScriptableObject).IsAssignableFrom(soType))
        {
            Debug.LogError($"❌ {typeName} 不是 ScriptableObject");
            return;
        }

        soInstance = ScriptableObject.CreateInstance(soType);
        soEditor = null; // 重置编辑器
        Debug.Log($"✅ 已创建 {typeName} 实例（内存对象）");
    }

    private void ExportSO()
    {
        if (soInstance == null)
        {
            Debug.LogError("❌ 没有 SO 实例可导出");
            return;
        }

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        FieldInfo dataField = soInstance.GetType().GetField("Data", BindingFlags.Public | BindingFlags.Instance);
        object dataObj = dataField?.GetValue(soInstance);

        if (dataObj == null)
        {
            Debug.LogWarning("⚠️ Data 字段为空，将导出空内容");
        }

        if (exportJson)
        {
            string jsonPath = Path.Combine(outputPath, outputFileName + ".json");
            string json = dataObj != null ? JsonUtility.ToJson(dataObj, true) : "{}";
            File.WriteAllText(jsonPath, json);
            Debug.Log($"✅ 导出 JSON: {jsonPath}");
        }

        if (exportXml)
        {
            string xmlPath = Path.Combine(outputPath, outputFileName + ".xml");
            File.WriteAllText(xmlPath, "<!-- TODO: XML 导出 -->");
            Debug.Log($"⚠️ 已生成空 XML 文件: {xmlPath}");
        }

        if (exportBinary)
        {
            string binPath = Path.Combine(outputPath, outputFileName + ".dat");
            File.WriteAllText(binPath, "");
            Debug.Log($"⚠️ 已生成空二进制文件: {binPath}");
        }

        AssetDatabase.Refresh();
    }
}
#endif