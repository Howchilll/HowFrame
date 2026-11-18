using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Shader 常量生成窗口：Tools/HowFrame/Shader Keys Generator</summary>
public class ShaderKeysWindow : EditorWindow
{
    private List<ShaderKeysGenerator.ShaderInfo> _shaders;
    private List<string> _warnings;
    private Vector2 _scroll;

    [MenuItem("Tools/HowFrame/Shader Keys Generator", false, 10)]
    public static void Open()
    {
        var window = GetWindow<ShaderKeysWindow>("Shader Keys");
        window.minSize = new Vector2(460, 300);
        window.Rescan();
    }

    private void Rescan() => _shaders = ShaderKeysGenerator.CollectShaders(out _warnings);

    private void OnGUI()
    {
        var s = HowFrameSettings.instance;
        EditorGUILayout.LabelField("扫描目录", string.Join(", ", s.shaderScanDirs ?? new List<string>()));
        HowFrameEditorGUI.PathInfo("输出目录", s.shaderCsDir);
        HowFrameEditorGUI.SettingsButton("在 HowFrame 设置中修改扫描目录 / 额外 Shader");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("重新扫描")) Rescan();
            using (new EditorGUI.DisabledScope(_shaders == null || _shaders.Count == 0))
            {
                if (GUILayout.Button("生成 ShaderKeys.cs", GUILayout.Height(24)))
                {
                    string file = ShaderKeysGenerator.Generate();
                    if (file != null)
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(HowFrameSettings.ToProjectRelative(file));
                        if (asset != null) EditorGUIUtility.PingObject(asset);
                    }
                }
            }
        }

        if (_shaders == null) return;
        foreach (var w in _warnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
        EditorGUILayout.LabelField($"将生成 {_shaders.Count} 个 Shader 的常量", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var info in _shaders)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(info.Shader.name, GUILayout.MinWidth(200));
                EditorGUILayout.LabelField($"{info.Shader.GetPropertyCount()} 个属性", GUILayout.Width(80));
                EditorGUILayout.LabelField(info.IncludeGlobalKeywords ? "项目" : "额外", GUILayout.Width(40));
                if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40))) EditorGUIUtility.PingObject(info.Shader);
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
