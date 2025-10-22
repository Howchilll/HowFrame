#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class EnumGenerater
{
    public static void Generate(EnumRoot root, string namespaceName, string className, string outputDir, bool enableConvertMethods = false, bool enableGetAllMethod = false)
    {
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var sb = new StringBuilder();

        bool useNamespace = !string.IsNullOrEmpty(namespaceName);

        if (useNamespace)
        {
            sb.AppendLine("namespace " + namespaceName);
            sb.AppendLine("{");
        }

        // 顶层静态类
        sb.AppendLine($"    public static class {className}");
        sb.AppendLine("    {");

        // 生成固定 Tag 嵌套类
        sb.AppendLine("        public class Tag { }");
        sb.AppendLine();

        // 输出元素
        foreach (var elem in root.elements)
        {
            AppendElementCode(sb, elem, 2, "Tag");
        }

        // 如果启用Convert方法，生成Convert方法
        if (enableConvertMethods)
        {
            sb.AppendLine();
            GenerateConvertMethods(sb, root.elements, 2, "Tag", "");
        }

        // 如果启用GetAll方法，生成GetAll方法
        if (enableGetAllMethod)
        {
            sb.AppendLine();
            GenerateGetAllMethod(sb, root.elements, 2, "Tag");
        }

        sb.AppendLine("    }");

        if (useNamespace)
            sb.AppendLine("}");

        string filePath = Path.Combine(outputDir, className + ".cs");
        File.WriteAllText(filePath, sb.ToString());
        UnityEngine.Debug.Log("生成 Enum 脚本：" + filePath);
    }

    private static void AppendElementCode(StringBuilder sb, EnumElement elem, int indentLevel, string tagType)
    {
        string indent = new string(' ', indentLevel * 4);

        if (elem.isList)
        {
            // 子集合 → 静态类，子类继续用父级 tagType
            sb.AppendLine($"{indent}public static class {elem.groupName}");
            sb.AppendLine($"{indent}{{");

            foreach (var child in elem.children)
            {
                AppendElementCode(sb, child, indentLevel + 1, tagType);
            }

            sb.AppendLine($"{indent}}}");
        }
        else
        {
            // 普通元素 → 静态字段，带泛型 Tag
            sb.AppendLine($"{indent}public static readonly EnumKey<{tagType}> {elem.value} = new();");
        }
    }

    private static void GenerateConvertMethods(StringBuilder sb, List<EnumElement> elements, int indentLevel, string tagType, string currentPath)
    {
        string indent = new string(' ', indentLevel * 4);
        
        // 生成Convert方法
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// 将字符串转换为EnumKey");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static EnumKey<{tagType}> Convert(string value)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    switch (value)");
        sb.AppendLine($"{indent}    {{");
        
        // 为每个元素生成case
        foreach (var elem in elements)
        {
            if (!elem.isList)
            {
                string fullPath = string.IsNullOrEmpty(currentPath) ? elem.value : $"{currentPath}.{elem.value}";
                sb.AppendLine($"{indent}        case \"{fullPath}\": return {elem.value};");
            }
            else
            {
                // 处理嵌套类
                string nestedPath = string.IsNullOrEmpty(currentPath) ? elem.groupName : $"{currentPath}.{elem.groupName}";
                GenerateConvertMethodsForNested(sb, elem.children, indentLevel + 1, tagType, nestedPath, elem.groupName);
            }
        }
        
        sb.AppendLine($"{indent}        default: throw new System.ArgumentException($\"Unknown value: {{value}}\");");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        
        // 生成反向Convert方法
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// 将EnumKey转换为字符串");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static string Convert(EnumKey<{tagType}> enumKey)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (enumKey == null) return null;");
        sb.AppendLine();
        
        // 为每个元素生成if判断
        foreach (var elem in elements)
        {
            if (!elem.isList)
            {
                string fullPath = string.IsNullOrEmpty(currentPath) ? elem.value : $"{currentPath}.{elem.value}";
                sb.AppendLine($"{indent}    if (enumKey == {elem.value}) return \"{fullPath}\";");
            }
            else
            {
                // 处理嵌套类
                string nestedPath = string.IsNullOrEmpty(currentPath) ? elem.groupName : $"{currentPath}.{elem.groupName}";
                GenerateConvertMethodsForNestedReverse(sb, elem.children, indentLevel + 1, tagType, nestedPath, elem.groupName);
            }
        }
        
        sb.AppendLine($"{indent}    throw new System.ArgumentException($\"Unknown enumKey: {{enumKey}}\");");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateConvertMethodsForNested(StringBuilder sb, List<EnumElement> children, int indentLevel, string tagType, string currentPath, string className)
    {
        string indent = new string(' ', indentLevel * 4);
        
        foreach (var child in children)
        {
            if (!child.isList)
            {
                string fullPath = string.IsNullOrEmpty(currentPath) ? child.value : $"{currentPath}.{child.value}";
                sb.AppendLine($"{indent}        case \"{fullPath}\": return {className}.{child.value};");
            }
            else
            {
                string nestedPath = string.IsNullOrEmpty(currentPath) ? child.groupName : $"{currentPath}.{child.groupName}";
                GenerateConvertMethodsForNested(sb, child.children, indentLevel, tagType, nestedPath, $"{className}.{child.groupName}");
            }
        }
    }

    private static void GenerateConvertMethodsForNestedReverse(StringBuilder sb, List<EnumElement> children, int indentLevel, string tagType, string currentPath, string className)
    {
        string indent = new string(' ', indentLevel * 4);
        
        foreach (var child in children)
        {
            if (!child.isList)
            {
                string fullPath = string.IsNullOrEmpty(currentPath) ? child.value : $"{currentPath}.{child.value}";
                sb.AppendLine($"{indent}    if (enumKey == {className}.{child.value}) return \"{fullPath}\";");
            }
            else
            {
                string nestedPath = string.IsNullOrEmpty(currentPath) ? child.groupName : $"{currentPath}.{child.groupName}";
                GenerateConvertMethodsForNestedReverse(sb, child.children, indentLevel, tagType, nestedPath, $"{className}.{child.groupName}");
            }
        }
    }

    private static void GenerateGetAllMethod(StringBuilder sb, List<EnumElement> elements, int indentLevel, string tagType)
    {
        string indent = new string(' ', indentLevel * 4);
        
        // 生成GetAll方法
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// 获取所有枚举项");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static System.Collections.Generic.List<EnumKey<{tagType}>> GetAll()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    return new System.Collections.Generic.List<EnumKey<{tagType}>>");
        sb.AppendLine($"{indent}    {{");
        
        // 为每个元素生成枚举项
        foreach (var elem in elements)
        {
            if (!elem.isList)
            {
                sb.AppendLine($"{indent}        {elem.value},");
            }
            else
            {
                // 处理嵌套类
                GenerateGetAllForNested(sb, elem.children, indentLevel + 2, tagType, elem.groupName);
            }
        }
        
        sb.AppendLine($"{indent}    }};");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateGetAllForNested(StringBuilder sb, List<EnumElement> children, int indentLevel, string tagType, string className)
    {
        string indent = new string(' ', indentLevel * 4);
        
        foreach (var child in children)
        {
            if (!child.isList)
            {
                sb.AppendLine($"{indent}{className}.{child.value},");
            }
            else
            {
                // 递归处理更深层的嵌套
                GenerateGetAllForNested(sb, child.children, indentLevel, tagType, $"{className}.{child.groupName}");
            }
        }
    }
}
#endif