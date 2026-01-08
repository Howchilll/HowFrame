using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class UISciptCreator : EditorWindow
{
    private GameObject prefab;
    private string folderPath = "Assets";

    [MenuItem("Assets/Create/CustomScript/UIPanelScript")]
    private static void OpenWindow()
    {
        UISciptCreator window = GetWindow<UISciptCreator>();
        window.titleContent = new GUIContent("UIPanel Script Generator");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("UIPanel Script Generator", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("UI Prefab", prefab, typeof(GameObject), false);
        // 自动使用 Project 视图选中的文件夹
        var obj = Selection.activeObject;
        if (obj != null)
        {
            string selPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(selPath))
            {
                folderPath = selPath;
            }
        }
        EditorGUILayout.LabelField("Output Folder", folderPath);


        if (GUILayout.Button("Create"))
        {
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a prefab.", "OK");
                return;
            }
            GenerateScript();
        }
    }

    // ================================================================
    // 生成核心
    // ================================================================
    private void GenerateScript()
    {
        string scriptName = prefab.name;
        string scriptPath = Path.Combine(folderPath, scriptName + ".cs");

        Dictionary<string, string> definitions = new();
        Dictionary<string, string> inits = new();
        List<string> textObjects = new();
        Dictionary<string, string> callbackMethods = new();

        // 扫描组件
        ScanComponents(prefab.transform, "", definitions, inits, textObjects, callbackMethods);

        if (!File.Exists(scriptPath))
        {
            CreateNewScript(scriptPath, scriptName, definitions, inits, textObjects, callbackMethods);
        }
        else
        {
            UpdateExistingScript(scriptPath, definitions, inits, textObjects, callbackMethods);
        }

        AssetDatabase.Refresh();
    }

    // ====================================================================
    // 扫描组件
    // ====================================================================
    private void ScanComponents(Transform node, string path,
        Dictionary<string, string> defs,
        Dictionary<string, string> inits,
        List<string> texts,
        Dictionary<string, string> callbacks)
    {
        string curPath = string.IsNullOrEmpty(path) ? node.name : $"{path}/{node.name}";

        // Button
        Button btn = node.GetComponent<Button>();
        if (btn)
        {
            string varName = node.name;
            defs[varName] = $"private Button {varName};";
            inits[varName] =
                $"this.transform.Find(\"{curPath}\").GetComponent<Button>()";

            // callback
            string cb = $"On{varName}Click";
            callbacks[cb] = $"private void {cb}() {{ }}";
        }

        // TextMeshProUGUI
        var tmp = node.GetComponent<TextMeshProUGUI>();
        if (tmp)
        {
            string varName = node.name;
            defs[varName] = $"private TextMeshProUGUI {varName};";
            inits[varName] =
                $"this.transform.Find(\"{curPath}\").GetComponent<TextMeshProUGUI>()";

            if (!tmp.text.StartsWith("//"))
                texts.Add(varName);
        }

        // Slider
        var slider = node.GetComponent<Slider>();
        if (slider)
        {
            string varName = node.name;
            defs[varName] = $"private Slider {varName};";
            inits[varName] =
                $"this.transform.Find(\"{curPath}\").GetComponent<Slider>()";

            string cb = $"On{varName}Changed";
            callbacks[cb] = $"private void {cb}(float value) {{ }}";
        }

        // Input
        var input = node.GetComponent<TMP_InputField>();
        if (input)
        {
            string varName = node.name;
            defs[varName] = $"private TMP_InputField {varName};";
            inits[varName] =
                $"this.transform.Find(\"{curPath}\").GetComponent<TMP_InputField>()";

            string cb = $"On{varName}End";
            callbacks[cb] = $"private void {cb}(string text) {{ }}";
        }

        // 继续递归
        foreach (Transform child in node)
            ScanComponents(child, curPath, defs, inits, texts, callbacks);
    }

    // ====================================================================
    // 创建全新的脚本
    // ====================================================================
    private void CreateNewScript(
        string path, string className,
        Dictionary<string, string> defs,
        Dictionary<string, string> inits,
        List<string> texts,
        Dictionary<string, string> callbacks)
    {
        StringBuilder sb = new();

        sb.AppendLine("using System;");
        sb.AppendLine("using HowEnum;");
        sb.AppendLine("using HowFrame;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine();
        sb.AppendLine($"public class {className} : PanelBase");
        sb.AppendLine("{");

        // ------------------ 定义区 ------------------
        sb.AppendLine("\t//定义");
        foreach (var d in defs.Values)
            sb.AppendLine("\t" + d);
        sb.AppendLine("\tprivate string[] TransContent;");
        sb.AppendLine("\t//end定义\n");

        // ------------------ Init -------------------
        sb.AppendLine("\tprotected override void Init()");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\t//初始化");

        foreach (var kv in inits)
        {
            string varName = kv.Key;
            string stmt = kv.Value;
            sb.AppendLine($"\t\t{varName} = {stmt};");
        }

        // 统一绑定回调
        foreach (var cb in callbacks.Keys)
        {
            string ctrl = cb.Replace("On", "").Replace("Click", "").Replace("Changed", "").Replace("End", "");
            if (inits.ContainsKey(ctrl))
            {
                sb.AppendLine($"\t\t{ctrl}.onClick?.AddListener({cb});");
            }
        }

        sb.AppendLine("\t\t//end初始化");
        sb.AppendLine("\t}\n");

        // ---------------- WhenShow -------------------
        sb.AppendLine("\tprotected override void WhenShow()");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\t//多语言");
        sb.AppendLine("\t\tvar rawContent = LangManager.GetLangContent(LangModuleEnum.UI, \"" + className + "Content\");");
        sb.AppendLine("\t\tTransContent = rawContent.Split(\",\");\n");

        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"\t\t{texts[i]}.text = TransContent[{i}];");

        sb.Append("\t\t//文本: ");
        foreach (string t in texts)
            sb.Append(t + " ");
        sb.AppendLine();
        sb.AppendLine("\t\t//end多语言");
        sb.AppendLine("\t}\n");

        sb.AppendLine("\tprotected override void WhenHide() { }\n");

        // ---------------- Callbacks -------------------
        foreach (var cb in callbacks.Values)
            sb.AppendLine("\t" + cb + "\n");

        sb.AppendLine("}");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    // ====================================================================
    // 更新已有脚本（只改区块）
    // ====================================================================
    private void UpdateExistingScript(
        string path,
        Dictionary<string, string> defs,
        Dictionary<string, string> inits,
        List<string> texts,
        Dictionary<string, string> callbacks)
    {
        string content = File.ReadAllText(path);

        // 更新定义区块
        content = ReplaceRegion(content, "定义", defs, extra: "private string[] TransContent;");

        // 更新初始化区块：赋值 + 回调绑定
        StringBuilder initSb = new();
        initSb.AppendLine("\t//初始化");

        foreach (var kv in inits)
        {
            string varName = kv.Key;
            string stmt = kv.Value;
            initSb.AppendLine($"\t\t{varName} = {stmt};");
        }

        // 绑定回调
        foreach (var cb in callbacks.Keys)
        {
            string ctrl = cb.Replace("On", "").Replace("Click", "").Replace("Changed", "").Replace("End", "");
            if (inits.ContainsKey(ctrl))
            {
                if (ctrl.EndsWith("Btn"))
                    initSb.AppendLine($"\t\t{ctrl}.onClick?.AddListener({cb});");
                else
                    initSb.AppendLine($"\t\t// TODO: add listener for {ctrl} if needed");
            }
        }

        initSb.AppendLine("\t//end初始化");

        content = ReplaceRawRegion(content, "//初始化", "//end初始化", initSb.ToString());

        // 更新多语言区块
        content = ReplaceLangRegion(content, texts);

        // 添加缺失回调到类体内
        foreach (var cb in callbacks)
        {
            if (!content.Contains(cb.Key + "("))
            {
                int insertIndex = content.LastIndexOf("}");
                if (insertIndex > 0)
                {
                    content = content.Insert(insertIndex, "\n\t" + cb.Value + "\n");
                }
            }
        }

        File.WriteAllText(path, content, Encoding.UTF8);
    }
    
    private string ReplaceRawRegion(string content, string startTag, string endTag, string replacement)
    {
        int i1 = content.IndexOf(startTag);
        int i2 = content.IndexOf(endTag);
        if (i1 < 0 || i2 < 0) return content;
        return content.Substring(0, i1) + replacement + content.Substring(i2 + endTag.Length);
    }
    // ====================================================================
    // 区块替换
    // ====================================================================
    private string ReplaceRegion(string content, string tag, Dictionary<string, string> map, string extra = null)
    {
        string start = $"//{tag}";
        string end = $"//end{tag}";

        int i1 = content.IndexOf(start);
        int i2 = content.IndexOf(end);
        if (i1 < 0 || i2 < 0) return content;

        StringBuilder sb = new();
        sb.AppendLine(start);
        foreach (var v in map.Values)
            sb.AppendLine("\t" + v);
        if (extra != null)
            sb.AppendLine("\t" + extra);
        sb.Append(end);

        return content.Substring(0, i1) + sb + content.Substring(i2 + end.Length);
    }

    // 多语言区块
    private string ReplaceLangRegion(string content, List<string> texts)
    {
        string start = "//多语言";
        string end = "//end多语言";

        int i1 = content.IndexOf(start);
        int i2 = content.IndexOf(end);
        if (i1 < 0 || i2 < 0) return content;

        StringBuilder sb = new();
        sb.AppendLine(start);
        sb.AppendLine("\t\tvar rawContent = LangManager.GetLangContent(LangModuleEnum.UI, \"XXXContent\");");
        sb.AppendLine("\t\tTransContent = rawContent.Split(\",\");");

        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"\t\t{texts[i]}.text = TransContent[{i}];");

        sb.Append("\t\t//文本: ");
        foreach (string t in texts)
            sb.Append(t + " ");
        sb.AppendLine();
        sb.Append(end);

        return content.Substring(0, i1) + sb + content.Substring(i2 + end.Length);
    }
}
