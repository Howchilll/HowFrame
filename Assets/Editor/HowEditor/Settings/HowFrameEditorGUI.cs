using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 各编辑器工具窗口共用的 GUI：只读展示来自 HowFrameSettings 的路径，并提供跳转到配置页的按钮。
/// </summary>
public static class HowFrameEditorGUI
{
    /// <summary>只读显示一个配置路径；目录不存在时标黄提示</summary>
    public static void PathInfo(string label, string path)
    {
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField(label, path);

        bool ok = !string.IsNullOrWhiteSpace(path) && Directory.Exists(HowFrameSettings.ToFullPath(path));
        var icon = EditorGUIUtility.IconContent(ok ? "TestPassed" : "console.warnicon.sml");
        icon.tooltip = ok ? HowFrameSettings.ToFullPath(path) : "目录不存在";
        GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>「修改路径」按钮，打开 Project Settings > HowFrame</summary>
    public static void SettingsButton(string text = "在 HowFrame 设置中修改路径")
    {
        if (GUILayout.Button(text, EditorStyles.miniButton))
            HowFrameSettingsProvider.Open();
    }
}
