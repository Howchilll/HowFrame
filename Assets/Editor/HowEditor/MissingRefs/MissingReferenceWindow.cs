using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>丢失引用扫描窗口：Tools/HowFrame/Missing References</summary>
public class MissingReferenceWindow : EditorWindow
{
    private string _folder = "Assets";
    private bool _prefabs = true;
    private bool _scriptableObjects = true;
    private bool _openScenes = true;
    private bool _allScenes;
    private List<MissingReferenceScanner.Finding> _findings;
    private Vector2 _scroll;
    private readonly HashSet<string> _collapsed = new HashSet<string>();

    [MenuItem("Tools/HowFrame/Missing References (丢失引用)", false, 3)]
    public static void Open()
    {
        var window = GetWindow<MissingReferenceWindow>("Missing References");
        window.minSize = new Vector2(520, 320);
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            _folder = EditorGUILayout.TextField("扫描目录", _folder);
            if (GUILayout.Button("选择", GUILayout.Width(44)))
            {
                string picked = EditorUtility.OpenFolderPanel("扫描目录", Application.dataPath, "");
                string rel = string.IsNullOrEmpty(picked) ? null : HowFrameSettings.ToProjectRelative(picked);
                if (rel != null && rel.StartsWith("Assets")) _folder = rel;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _prefabs = GUILayout.Toggle(_prefabs, "Prefab", GUILayout.Width(70));
            _scriptableObjects = GUILayout.Toggle(_scriptableObjects, "ScriptableObject", GUILayout.Width(130));
            _openScenes = GUILayout.Toggle(_openScenes, "已打开的场景", GUILayout.Width(100));
            _allScenes = GUILayout.Toggle(_allScenes, new GUIContent("目录下所有场景", "逐个附加打开扫描，扫描完自动关闭"));
        }

        if (GUILayout.Button("扫描", GUILayout.Height(26))) Scan();
        if (_findings == null) return;

        int scripts = _findings.Count(f => f.Kind == MissingReferenceScanner.Kind.MissingScript);
        int refs = _findings.Count - scripts;
        EditorGUILayout.HelpBox(_findings.Count == 0 ? "没有发现丢失的脚本或引用 ✔" : $"丢失脚本 {scripts} 处，丢失引用 {refs} 处",
            _findings.Count == 0 ? MessageType.Info : MessageType.Warning);

        if (scripts > 0 && GUILayout.Button("移除所有丢失脚本（Prefab 直接保存；场景需手动保存）"))
            RemoveMissingScripts();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var group in _findings.GroupBy(f => f.AssetPath))
        {
            bool open = !_collapsed.Contains(group.Key);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool now = EditorGUILayout.Foldout(open, $"{group.Key}（{group.Count()}）", true);
                if (now != open)
                {
                    if (now) _collapsed.Remove(group.Key);
                    else _collapsed.Add(group.Key);
                }
                if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40))) PingAsset(group.Key);
            }
            if (!open) continue;

            foreach (var f in group)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(16);
                    var icon = EditorGUIUtility.IconContent(f.Kind == MissingReferenceScanner.Kind.MissingScript ? "console.erroricon.sml" : "console.warnicon.sml");
                    GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
                    string text = f.Kind == MissingReferenceScanner.Kind.MissingScript
                        ? $"{f.ObjectPath}　丢失脚本"
                        : $"{f.ObjectPath}　{f.Component}.{f.Property}";
                    EditorGUILayout.LabelField(text);
                    if (GUILayout.Button("选中", EditorStyles.miniButton, GUILayout.Width(40))) SelectObject(f);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        if (!AssetDatabase.IsValidFolder(_folder))
        {
            EditorUtility.DisplayDialog("扫描", $"目录不存在: {_folder}", "好");
            return;
        }

        var findings = new List<MissingReferenceScanner.Finding>();
        try
        {
            if (_prefabs || _scriptableObjects) findings.AddRange(MissingReferenceScanner.ScanAssets(_folder, _prefabs, _scriptableObjects));
            if (_allScenes) findings.AddRange(MissingReferenceScanner.ScanAllScenes(_folder));
            else if (_openScenes) findings.AddRange(MissingReferenceScanner.ScanOpenScenes());
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 同一位置可能被两种方式扫到（打开的场景也在目录里）
        _findings = findings.GroupBy(f => f.ToString()).Select(g => g.First()).ToList();
    }

    private void RemoveMissingScripts()
    {
        var paths = _findings.Where(f => f.Kind == MissingReferenceScanner.Kind.MissingScript).Select(f => f.AssetPath).Distinct().ToList();
        if (!EditorUtility.DisplayDialog("移除丢失脚本",
                $"将从 {paths.Count} 个 Prefab / 场景中移除丢失的脚本组件。\nPrefab 会直接保存（只能通过版本管理恢复），场景中的修改可撤销、需手动保存。\n\n继续？",
                "移除", "取消"))
            return;

        int removed = 0;
        foreach (var path in paths)
        {
            if (path.EndsWith(".prefab"))
            {
                removed += MissingReferenceScanner.RemoveMissingScriptsInPrefab(path);
                continue;
            }
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded) removed += MissingReferenceScanner.RemoveMissingScriptsInScene(scene);
            else Debug.LogWarning($"[MissingReferences] 场景 {path} 未打开，跳过（打开后再扫描移除）");
        }
        Debug.Log($"[MissingReferences] 已移除 {removed} 个丢失的脚本组件");
        Scan();
    }

    private static void PingAsset(string path)
    {
        var obj = AssetDatabase.LoadMainAssetAtPath(path);
        if (obj == null) return;
        EditorGUIUtility.PingObject(obj);
    }

    private static void SelectObject(MissingReferenceScanner.Finding f)
    {
        if (f.AssetPath.EndsWith(".unity"))
        {
            var scene = SceneManager.GetSceneByPath(f.AssetPath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (!EditorUtility.DisplayDialog("选中", $"场景 {f.AssetPath} 未打开，是否打开？", "打开", "取消")) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(f.AssetPath);
            }
        }

        var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(f.ObjectId);
        if (obj == null)
        {
            PingAsset(f.AssetPath);
            return;
        }
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }
}
