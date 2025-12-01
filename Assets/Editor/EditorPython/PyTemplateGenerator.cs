#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Linq;

public class PyTemplateGenerator : EditorWindow
{
    [Serializable]
    public class ParameterEntry
    {
        public string Name = "";
    }

    private string extensionName = "";
    private List<ParameterEntry> parameters = new List<ParameterEntry>();
    private Vector2 scrollPos;
    private const string EditorPrefsKey = "PyTemplateGenerator_Config";

    [MenuItem("Tools/PyFunctions/生成 Python 扩展模板")]
    public static void ShowWindow()
    {
        GetWindow<PyTemplateGenerator>("Python 扩展模板生成器");
    }

    private void OnEnable()
    {
        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            string json = EditorPrefs.GetString(EditorPrefsKey);
            var config = JsonUtility.FromJson<ConfigData>(json);
            if (config != null)
            {
                extensionName = config.extensionName ?? "";
                parameters = config.parameters ?? new List<ParameterEntry>();
            }
        }
    }

    private void OnDisable()
    {
        var config = new ConfigData
        {
            extensionName = extensionName,
            parameters = parameters
        };
        string json = JsonUtility.ToJson(config);
        EditorPrefs.SetString(EditorPrefsKey, json);
    }

    [Serializable]
    private class ConfigData
    {
        public string extensionName;
        public List<ParameterEntry> parameters;
    }

    private void OnGUI()
    {
        GUILayout.Label("Python 扩展模板生成器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        extensionName = EditorGUILayout.TextField("扩展名称", extensionName);
        
        GUILayout.Space(10);
        GUILayout.Label("参数列表", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        for (int i = 0; i < parameters.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            parameters[i].Name = EditorGUILayout.TextField($"参数 {i + 1}", parameters[i].Name);
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                parameters.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("添加参数"))
        {
            parameters.Add(new ParameterEntry());
        }

        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(extensionName));
        if (GUILayout.Button("生成模板", GUILayout.Height(30)))
        {
            GenerateTemplate();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void GenerateTemplate()
    {
        if (string.IsNullOrEmpty(extensionName))
        {
            EditorUtility.DisplayDialog("错误", "请输入扩展名称", "确定");
            return;
        }

        // 规范化名称
        string normalizedName = NormalizeName(extensionName);
        string pythonScriptName = ToSnakeCase(normalizedName);

        // 创建文件夹路径
        string basePath = Path.Combine(Application.dataPath, "Editor", "EditorPython", normalizedName);
        
        if (Directory.Exists(basePath))
        {
            if (!EditorUtility.DisplayDialog("确认", $"文件夹 {normalizedName} 已存在，是否覆盖？", "是", "否"))
            {
                return;
            }
        }
        else
        {
            Directory.CreateDirectory(basePath);
        }

        // 生成 .cs 文件
        string csPath = Path.Combine(basePath, $"{normalizedName}.cs");
        string csContent = GenerateCsFile(normalizedName, pythonScriptName, parameters);
        File.WriteAllText(csPath, csContent, Encoding.UTF8);

        // 生成 .py 文件
        string pyPath = Path.Combine(basePath, $"{pythonScriptName}.py");
        string pyContent = GeneratePyFile(parameters);
        File.WriteAllText(pyPath, pyContent, Encoding.UTF8);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("成功", $"模板已生成到:\n{normalizedName}/", "确定");
    }

    private string GenerateCsFile(string className, string pythonScriptName, List<ParameterEntry> paramsList)
    {
        bool hasParams = paramsList != null && paramsList.Count > 0;
        var validParams = paramsList?.Where(p => !string.IsNullOrEmpty(p.Name))
            .Select(p => new ParameterEntry { Name = NormalizeParameterName(p.Name) })
            .ToList() ?? new List<ParameterEntry>();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();

        if (hasParams && validParams.Count > 0)
        {
            // 有参数的情况：生成带参数面板的类
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");
            sb.AppendLine($"    [MenuItem(\"Tools/PyFunctions/{className}\")]");
            sb.AppendLine("    public static void Run()");
            sb.AppendLine("    {");
            sb.AppendLine($"        {className}ParameterWindow.ShowWindow();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public static void Execute({string.Join(", ", validParams.Select(p => $"string {p.Name}"))})");
            sb.AppendLine("    {");
            sb.AppendLine($"        string scriptPath = Path.Combine(Application.dataPath, \"Editor\", \"EditorPython\", \"{className}\", \"{pythonScriptName}.py\");");
            sb.AppendLine("        scriptPath = Path.GetFullPath(scriptPath);");
            sb.AppendLine();
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            PyCaller pyCaller = new PyCaller();");
            sb.AppendLine();
            sb.AppendLine("            string[] args = new string[]");
            sb.AppendLine("            {");
            foreach (var param in validParams)
            {
                sb.AppendLine($"                {param.Name},");
            }
            sb.AppendLine("            };");
            sb.AppendLine();
            sb.AppendLine("            pyCaller.RunPythonScript(scriptPath, args, OnPyDone);");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (System.Exception e)");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogError($\"执行 Python 脚本时出错: {e.Message}\\n{e.StackTrace}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    private static void OnPyDone(int exitCode)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (exitCode == 0)");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.Log(\"Python 脚本执行成功！\");");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogWarning($\"Python 脚本执行完成，但退出码不为 0: {exitCode}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"public class {className}ParameterWindow : EditorWindow");
            sb.AppendLine("{");
            foreach (var param in validParams)
            {
                sb.AppendLine($"    private string {param.Name} = \"\";");
            }
            sb.AppendLine();
            sb.AppendLine($"    public static void ShowWindow()");
            sb.AppendLine("    {");
            sb.AppendLine($"        {className}ParameterWindow window = GetWindow<{className}ParameterWindow>(\"{className} 参数\");");
            sb.AppendLine("        window.Show();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    private void OnGUI()");
            sb.AppendLine("    {");
            sb.AppendLine($"        GUILayout.Label(\"{className} 参数设置\", EditorStyles.boldLabel);");
            sb.AppendLine("        GUILayout.Space(10);");
            foreach (var param in validParams)
            {
                sb.AppendLine($"        {param.Name} = EditorGUILayout.TextField(\"{param.Name}\", {param.Name});");
            }
            sb.AppendLine("        GUILayout.Space(20);");
            sb.AppendLine("        if (GUILayout.Button(\"执行\", GUILayout.Height(30)))");
            sb.AppendLine("        {");
            sb.AppendLine($"            {className}.Execute({string.Join(", ", validParams.Select(p => p.Name))});");
            sb.AppendLine("            Close();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        else
        {
            // 无参数的情况：直接触发
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");
            sb.AppendLine($"    [MenuItem(\"Tools/PyFunctions/{className}\")]");
            sb.AppendLine("    public static void Run()");
            sb.AppendLine("    {");
            sb.AppendLine($"        string scriptPath = Path.Combine(Application.dataPath, \"Editor\", \"EditorPython\", \"{className}\", \"{pythonScriptName}.py\");");
            sb.AppendLine("        scriptPath = Path.GetFullPath(scriptPath);");
            sb.AppendLine();
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            PyCaller pyCaller = new PyCaller();");
            sb.AppendLine();
            sb.AppendLine("            pyCaller.RunPythonScript(scriptPath, OnPyDone);");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (System.Exception e)");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogError($\"执行 Python 脚本时出错: {e.Message}\\n{e.StackTrace}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    private static void OnPyDone(int exitCode)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (exitCode == 0)");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.Log(\"Python 脚本执行成功！\");");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogWarning($\"Python 脚本执行完成，但退出码不为 0: {exitCode}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private string GeneratePyFile(List<ParameterEntry> paramsList)
    {
        var validParams = paramsList?.Where(p => !string.IsNullOrEmpty(p.Name))
            .Select(p => ToSnakeCase(NormalizeParameterName(p.Name)))
            .ToList() ?? new List<string>();
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("import sys");
        sb.AppendLine();

        if (validParams.Count > 0)
        {
            sb.AppendLine("if len(sys.argv) > 1:");
            sb.AppendLine("    print(\"参数列表:\")");
            sb.AppendLine("    for i, arg in enumerate(sys.argv[1:], 1):");
            sb.AppendLine("        print(f\"  参数 {i}: {arg}\")");
            sb.AppendLine();
            
            // 将参数赋值给变量
            for (int i = 0; i < validParams.Count; i++)
            {
                sb.AppendLine($"    {validParams[i]} = sys.argv[{i + 1}] if len(sys.argv) > {i + 1} else \"\"");
            }
            sb.AppendLine();
        }

        sb.AppendLine("# 在这里编写你的 Python 代码");
        sb.AppendLine();
        sb.AppendLine("print(\"Python 执行完毕！\")");

        return sb.ToString();
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        
        // 移除空格和特殊字符，保留字母数字
        StringBuilder sb = new StringBuilder();
        bool nextUpper = true;
        
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                if (nextUpper)
                {
                    sb.Append(char.ToUpper(c));
                    nextUpper = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (c == ' ' || c == '_' || c == '-')
            {
                nextUpper = true;
            }
        }
        
        return sb.ToString();
    }

    private string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('_');
            }
            sb.Append(char.ToLower(c));
        }
        
        return sb.ToString();
    }

    private string NormalizeParameterName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        
        StringBuilder sb = new StringBuilder();
        bool nextUpper = false;
        
        // 确保第一个字符是字母
        bool firstChar = true;
        foreach (char c in name)
        {
            if (char.IsLetter(c))
            {
                if (firstChar)
                {
                    sb.Append(char.ToLower(c));
                    firstChar = false;
                }
                else if (nextUpper)
                {
                    sb.Append(char.ToUpper(c));
                    nextUpper = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (char.IsDigit(c) && !firstChar)
            {
                sb.Append(c);
            }
            else if (c == ' ' || c == '_' || c == '-')
            {
                nextUpper = true;
            }
        }
        
        // 如果为空或不是以字母开头，添加前缀
        if (sb.Length == 0 || !char.IsLetter(sb[0]))
        {
            return "param" + sb.ToString();
        }
        
        return sb.ToString();
    }
}
#endif

