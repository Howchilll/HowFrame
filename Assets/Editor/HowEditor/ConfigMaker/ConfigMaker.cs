using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConfigMaker : EditorWindow
{
    private enum TypeKind { Struct, Class, Record }
    private enum FieldKind { Int, Float, String, Bool, List, Dictionary, Custom }

    [System.Serializable]
    private class FieldDef
    {
        public FieldKind FieldType;
        public string TypeName = "int"; // 默认
        public string FieldName = "field";
    }

    private string className = "NewType";
    private TypeKind typeKind = TypeKind.Struct;
    private bool hasBase = false;
    private string baseType = "";
    private List<FieldDef> fields = new List<FieldDef>();

    [MenuItem("Tools/Config Maker")]
    public static void ShowWindow()
    {
        GetWindow<ConfigMaker>("Config Maker");
    }

private void OnGUI()
{
    // 顶部类型选择
    typeKind = (TypeKind)EditorGUILayout.EnumPopup("Kind", typeKind);

    // 类名
    className = EditorGUILayout.TextField("Name", className);

    // 继承父类（仅 Class 和 Record 才能继承）
    if (typeKind != TypeKind.Struct)
    {
        hasBase = EditorGUILayout.Toggle("Has Base", hasBase);
        if (hasBase)
        {
            baseType = EditorGUILayout.TextField("Base Type", baseType);
        }
    }

    EditorGUILayout.Space();

    // 字段列表
    EditorGUILayout.LabelField("Fields", EditorStyles.boldLabel);

    int removeIndex = -1;
    for (int i = 0; i < fields.Count; i++)
    {
        var f = fields[i];
        EditorGUILayout.BeginHorizontal();

        // 类型选择
        FieldKind prevKind = f.FieldType;
        f.FieldType = (FieldKind)EditorGUILayout.EnumPopup(f.FieldType, GUILayout.Width(100));

        // 自定义类型选中时重置类型名
        if (f.FieldType == FieldKind.Custom && prevKind != FieldKind.Custom)
        {
            f.TypeName = "";
        }

        // 类型输入/显示，自适应宽度
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

        // 字段名输入框，自适应宽度
        f.FieldName = EditorGUILayout.TextField(f.FieldName, GUILayout.ExpandWidth(true));

        // 删除按钮
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            removeIndex = i;
        }

        EditorGUILayout.EndHorizontal();
    }

    // 延迟删除
    if (removeIndex >= 0)
    {
        fields.RemoveAt(removeIndex);
    }

    // 添加字段按钮
    if (GUILayout.Button("Add Field"))
    {
        fields.Add(new FieldDef());
    }

    GUILayout.FlexibleSpace(); // 推到最底部

    // 生成代码按钮
    if (GUILayout.Button("Generate Code", GUILayout.Height(30)))
    {
        GenerateCode();
    }
}


    private void GenerateCode()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // 类/struct/record 定义
        string kindWord = typeKind.ToString().ToLower();
        if (hasBase && typeKind != TypeKind.Struct)
            sb.AppendLine($"public {kindWord} {className} : {baseType}");
        else
            sb.AppendLine($"public {kindWord} {className}");

        sb.AppendLine("{");

        // 字段
        foreach (var f in fields)
        {
            sb.AppendLine($"    public {f.TypeName} {UpperFirst(f.FieldName)};");
        }
        sb.AppendLine();

        // 构造函数
        sb.Append($"    public {className}(");
        for (int i = 0; i < fields.Count; i++)
        {
            var f = fields[i];
            sb.Append($"{f.TypeName} {LowerFirst(f.FieldName)}");
            if (i < fields.Count - 1)
                sb.Append(", ");
        }
        sb.AppendLine(")");
        sb.AppendLine("    {");
        foreach (var f in fields)
        {
            sb.AppendLine($"        {UpperFirst(f.FieldName)} = {LowerFirst(f.FieldName)};");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");

        Debug.Log(sb.ToString());
    }

    private string UpperFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    private string LowerFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s.Substring(1);
}
