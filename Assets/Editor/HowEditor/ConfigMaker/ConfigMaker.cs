#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ConfigMaker : EditorWindow
{
    private enum TypeKind { Struct, Class, Record }
    private enum FieldKind { Int, Float, String, Bool, List, Dictionary, Custom }

    [System.Serializable]
    private class FieldDef
    {
        public FieldKind FieldType;
        public string TypeName = "int"; 
        public string FieldName = "field";
    }

    private string className = "NewType";
    private TypeKind typeKind = TypeKind.Struct;
    private bool hasBase = false;
    private string baseType = "";
    private List<FieldDef> fields = new List<FieldDef>();

    private string outputDir = "Assets/Config"; // ✅ 默认路径

    // using 列表（支持折叠）
    private bool showUsings = true;
    private List<string> usingList = new List<string>() { "System" };

    // 命名空间
    private bool useNamespace = false;
    private string namespaceName = "MyNamespace";

    // MessagePack toggle
    private bool useMessagePack = false;

    //[MenuItem("Tools/Config/Config Maker")]
    public static void ShowWindow()
    {
        GetWindow<ConfigMaker>("Config Maker");
    }

    private void OnGUI()
    {
        // 顶部类型选择
        typeKind = (TypeKind)EditorGUILayout.EnumPopup("Kind", typeKind);
        className = EditorGUILayout.TextField("Name", className);

        if (typeKind != TypeKind.Struct)
        {
            hasBase = EditorGUILayout.Toggle("Has Base", hasBase);
            if (hasBase)
                baseType = EditorGUILayout.TextField("Base Type", baseType);
        }

        EditorGUILayout.Space();

        // using 折叠面板
        showUsings = EditorGUILayout.Foldout(showUsings, "Using Namespaces");
        if (showUsings)
        {
            int removeUsing = -1;
            for (int i = 0; i < usingList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                usingList[i] = EditorGUILayout.TextField(usingList[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    removeUsing = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (removeUsing >= 0) usingList.RemoveAt(removeUsing);
            if (GUILayout.Button("Add Using")) usingList.Add("System");
        }

        EditorGUILayout.Space();

        // 命名空间
        useNamespace = EditorGUILayout.BeginToggleGroup("Use Namespace", useNamespace);
        namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
        EditorGUILayout.EndToggleGroup();

        // MessagePack toggle
        useMessagePack = EditorGUILayout.Toggle("Using MessagePack", useMessagePack);

        EditorGUILayout.Space();

        // 输出路径
        EditorGUILayout.LabelField("Output Directory", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        outputDir = EditorGUILayout.TextField("Folder Path", outputDir);

        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string folder = EditorUtility.OpenFolderPanel("Choose Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                    outputDir = "Assets" + folder.Substring(Application.dataPath.Length);
                else
                    Debug.LogError("❌ Please choose a folder inside the Unity project (under Assets).");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 字段列表
        EditorGUILayout.LabelField("Fields", EditorStyles.boldLabel);
        int removeIndex = -1;
        for (int i = 0; i < fields.Count; i++)
        {
            var f = fields[i];
            EditorGUILayout.BeginHorizontal();

            FieldKind prevKind = f.FieldType;
            f.FieldType = (FieldKind)EditorGUILayout.EnumPopup(f.FieldType, GUILayout.Width(100));

            if (f.FieldType == FieldKind.Custom && prevKind != FieldKind.Custom)
                f.TypeName = "";

            switch (f.FieldType)
            {
                case FieldKind.Int:
                case FieldKind.Float:
                case FieldKind.String:
                case FieldKind.Bool:
                    f.TypeName = f.FieldType.ToString().ToLower();
                    EditorGUILayout.LabelField(f.TypeName, GUILayout.ExpandWidth(true));
                    break;
                case FieldKind.List:
                    f.TypeName = EditorGUILayout.TextField(
                        f.TypeName.StartsWith("List<") ? f.TypeName : "List<?>",
                        GUILayout.ExpandWidth(true));
                    break;
                case FieldKind.Dictionary:
                    f.TypeName = EditorGUILayout.TextField(
                        f.TypeName.StartsWith("Dictionary<") ? f.TypeName : "Dictionary<?,?>",
                        GUILayout.ExpandWidth(true));
                    break;
                case FieldKind.Custom:
                    f.TypeName = EditorGUILayout.TextField(f.TypeName, GUILayout.ExpandWidth(true));
                    break;
            }

            f.FieldName = EditorGUILayout.TextField(f.FieldName, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("X", GUILayout.Width(25))) removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0) fields.RemoveAt(removeIndex);
        if (GUILayout.Button("Add Field")) fields.Add(new FieldDef());

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Generate Code", GUILayout.Height(30)))
        {
            GenerateCode();
        }
    }

  private void GenerateCode()
{
    System.Text.StringBuilder sb = new System.Text.StringBuilder();

    // 自动添加 System.Collections.Generic
    bool needGeneric = fields.Exists(f => f.FieldType == FieldKind.List || f.FieldType == FieldKind.Dictionary);
    if (needGeneric && !usingList.Contains("System.Collections.Generic"))
        usingList.Add("System.Collections.Generic");

    if (useMessagePack && !usingList.Contains("MessagePack"))
        usingList.Add("MessagePack");

    // 写 using
    foreach (var u in usingList)
    {
        if (!string.IsNullOrEmpty(u))
            sb.AppendLine($"using {u};");
    }
    sb.AppendLine();

    // 命名空间开始
    if (useNamespace)
    {
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
    }

    string indent = useNamespace ? "    " : "";

    if (useMessagePack)
        sb.AppendLine($"{indent}[MessagePackObject(AllowPrivate = true)]");
    sb.AppendLine($"{indent}[Serializable]");
    string kindWord = typeKind.ToString().ToLower();
    if (hasBase && typeKind != TypeKind.Struct)
        sb.AppendLine($"{indent}public {kindWord} {className} : {baseType}");
    else
        sb.AppendLine($"{indent}public {kindWord} {className}");
    sb.AppendLine($"{indent}{{");

    // 字段
    for (int i = 0; i < fields.Count; i++)
    {
        var f = fields[i];
        if (useMessagePack)
            sb.AppendLine($"{indent}    [Key({i})] public {f.TypeName} {UpperFirst(f.FieldName)};");
        else
            sb.AppendLine($"{indent}    public {f.TypeName} {UpperFirst(f.FieldName)};");
    }
    sb.AppendLine();

    // 构造函数
    sb.Append($"{indent}    public {className}(");
    for (int i = 0; i < fields.Count; i++)
    {
        var f = fields[i];
        sb.Append($"{f.TypeName} {LowerFirst(f.FieldName)}");
        if (i < fields.Count - 1) sb.Append(", ");
    }
    sb.AppendLine(")");
    sb.AppendLine($"{indent}    {{");
    foreach (var f in fields)
    {
        sb.AppendLine($"{indent}        {UpperFirst(f.FieldName)} = {LowerFirst(f.FieldName)};");
    }
    sb.AppendLine($"{indent}    }}");

    sb.AppendLine($"{indent}}}");

    if (useNamespace)
        sb.AppendLine("}");

    // 写文件
    string dir = string.IsNullOrEmpty(outputDir) ? "Assets/Configs" : outputDir;
    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    string filePath = Path.Combine(dir, className + ".cs");

    try
    {
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"✅ Data class generated at: {filePath}");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"❌ Failed to write file: {ex.Message}");
    }

    // ====== 伴随 ScriptableObject 生成 ======
    System.Text.StringBuilder soSb = new System.Text.StringBuilder();
    soSb.AppendLine("using UnityEngine;");
    if (useNamespace)
    {
        soSb.AppendLine($"namespace {namespaceName}");
        soSb.AppendLine("{");
    }

    string soIndent = useNamespace ? "    " : "";
    soSb.AppendLine("#if UNITY_EDITOR");
    soSb.AppendLine($"{soIndent}internal class {className}SO : ScriptableObject");
    soSb.AppendLine($"{soIndent}{{");
    soSb.AppendLine($"{soIndent}    public {className} Data;");
    soSb.AppendLine($"{soIndent}}}");
    soSb.AppendLine("#endif");

    if (useNamespace)
        soSb.AppendLine("}");

    string soPath = Path.Combine(dir, className + "SO.cs");
    try
    {
        File.WriteAllText(soPath, soSb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"✅ ScriptableObject wrapper generated at: {soPath}");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"❌ Failed to write SO wrapper: {ex.Message}");
    }
}

    private string UpperFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    private string LowerFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s.Substring(1);
}
#endif