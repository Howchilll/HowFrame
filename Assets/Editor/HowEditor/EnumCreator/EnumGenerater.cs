using System.IO;
using System.Text;

public static class EnumGenerater
{
    public static void Generate(EnumRoot root, string namespaceName, string className, string outputDir)
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

        sb.AppendLine($"    public static class {className}Enum");
        sb.AppendLine("    {");

        foreach (var elem in root.elements)
        {
            AppendElementCode(sb, elem, 2);
        }

        sb.AppendLine("    }");

        if (useNamespace)
            sb.AppendLine("}");

        string filePath = Path.Combine(outputDir, className + "Enum.cs");
        File.WriteAllText(filePath, sb.ToString());
        UnityEngine.Debug.Log("生成 Enum 脚本：" + filePath);
    }

    private static void AppendElementCode(StringBuilder sb, EnumElement elem, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 4);

        if (elem.isList)
        {
            // 子集合 → 静态类
            sb.AppendLine($"{indent}public static class {elem.groupName}");
            sb.AppendLine($"{indent}{{");

            foreach (var child in elem.children)
            {
                AppendElementCode(sb, child, indentLevel + 1);
            }

            sb.AppendLine($"{indent}}}");
        }
        else
        {
            // 普通元素 → 静态字段
            sb.AppendLine($"{indent}public static readonly EnumKey {elem.value} = new();");
        }
    }

}