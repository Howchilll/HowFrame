using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>HowFrame 健康检查窗口：Tools/HowFrame/Doctor</summary>
public class HowFrameDoctorWindow : EditorWindow
{
    private List<HowFrameDoctor.Issue> _issues;
    private Vector2 _scroll;
    private bool _showOk = true;

    [MenuItem("Tools/HowFrame/Doctor (健康检查)", priority = 1)]
    public static void Open()
    {
        var window = GetWindow<HowFrameDoctorWindow>("HowFrame Doctor");
        window.minSize = new Vector2(520, 320);
        window.Run();
    }

    private void Run()
    {
        try
        {
            EditorUtility.DisplayProgressBar("HowFrame Doctor", "检查中...", 0.5f);
            _issues = HowFrameDoctor.RunAll();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("重新检查", EditorStyles.toolbarButton, GUILayout.Width(70))) Run();
            _showOk = GUILayout.Toggle(_showOk, "显示通过项", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("HowFrame 设置", EditorStyles.toolbarButton, GUILayout.Width(90))) HowFrameSettingsProvider.Open();
        }

        if (_issues == null)
        {
            EditorGUILayout.HelpBox("点击「重新检查」开始", MessageType.Info);
            return;
        }

        int errors = _issues.Count(i => i.Level == HowFrameDoctor.Level.Error);
        int warnings = _issues.Count(i => i.Level == HowFrameDoctor.Level.Warning);
        EditorGUILayout.HelpBox(errors + warnings == 0 ? "全部通过 ✔" : $"{errors} 个错误，{warnings} 个警告",
            errors > 0 ? MessageType.Error : warnings > 0 ? MessageType.Warning : MessageType.Info);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var group in _issues.GroupBy(i => i.Category))
        {
            var shown = group.Where(i => _showOk || i.Level != HowFrameDoctor.Level.Ok).ToList();
            if (shown.Count == 0) continue;

            EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
            foreach (var issue in shown) DrawIssue(issue);
            EditorGUILayout.Space(4);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawIssue(HowFrameDoctor.Issue issue)
    {
        string iconName = issue.Level switch
        {
            HowFrameDoctor.Level.Error => "console.erroricon.sml",
            HowFrameDoctor.Level.Warning => "console.warnicon.sml",
            _ => "TestPassed",
        };

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(EditorGUIUtility.IconContent(iconName), GUILayout.Width(18), GUILayout.Height(18));
            EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);
            if (!string.IsNullOrEmpty(issue.AssetPath) && GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40)))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(issue.AssetPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }
    }
}
