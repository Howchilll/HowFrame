using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 丢失引用扫描：找出 Prefab / ScriptableObject / 场景里的
/// 1) 丢失的脚本（Missing Script）；2) 指向已删除资源的引用（Missing，不是 None）。
/// </summary>
public static class MissingReferenceScanner
{
    public enum Kind { MissingScript, MissingReference }

    public class Finding
    {
        public Kind Kind;
        /// <summary>资源路径（prefab / asset / 场景）</summary>
        public string AssetPath;
        /// <summary>层级路径，如 Root/Child</summary>
        public string ObjectPath;
        public string Component;
        public string Property;
        /// <summary>场景中的对象（场景打开时可直接定位）</summary>
        public GlobalObjectId ObjectId;

        public override string ToString() => Kind == Kind.MissingScript
            ? $"{AssetPath} > {ObjectPath}: 丢失脚本"
            : $"{AssetPath} > {ObjectPath} > {Component}.{Property}: 引用丢失";
    }

    #region 扫描

    /// <summary>扫描文件夹下的 Prefab 与 ScriptableObject</summary>
    public static List<Finding> ScanAssets(string folder, bool prefabs = true, bool scriptableObjects = true)
    {
        var findings = new List<Finding>();
        var filters = new List<string>();
        if (prefabs) filters.Add("t:Prefab");
        if (scriptableObjects) filters.Add("t:ScriptableObject");

        var paths = filters
            .SelectMany(f => AssetDatabase.FindAssets(f, new[] { folder }))
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/"))
            .Distinct()
            .ToList();

        for (int i = 0; i < paths.Count; i++)
        {
            if (EditorUtility.DisplayCancelableProgressBar("扫描丢失引用", paths[i], (float)i / paths.Count)) break;
            findings.AddRange(ScanAsset(paths[i]));
        }
        EditorUtility.ClearProgressBar();
        return findings;
    }

    public static List<Finding> ScanAsset(string path)
    {
        var findings = new List<Finding>();
        if (path.EndsWith(".prefab"))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root != null) ScanHierarchy(root, path, findings);
        }
        else
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is ScriptableObject so) ScanObject(so, path, so.name, findings);
        }
        return findings;
    }

    /// <summary>扫描当前已打开的场景</summary>
    public static List<Finding> ScanOpenScenes()
    {
        var findings = new List<Finding>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) findings.AddRange(ScanScene(scene));
        }
        return findings;
    }

    /// <summary>逐个以附加方式打开 Build Settings / 项目中的场景扫描，扫描完关闭原本没打开的场景</summary>
    public static List<Finding> ScanAllScenes(string folder)
    {
        var findings = new List<Finding>();
        var paths = AssetDatabase.FindAssets("t:Scene", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/"))
            .ToList();

        for (int i = 0; i < paths.Count; i++)
        {
            if (EditorUtility.DisplayCancelableProgressBar("扫描场景", paths[i], (float)i / paths.Count)) break;
            var existing = SceneManager.GetSceneByPath(paths[i]);
            bool wasOpen = existing.IsValid() && existing.isLoaded;
            var scene = wasOpen ? existing : EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
            findings.AddRange(ScanScene(scene));
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
        EditorUtility.ClearProgressBar();
        return findings;
    }

    public static List<Finding> ScanScene(Scene scene)
    {
        var findings = new List<Finding>();
        foreach (var root in scene.GetRootGameObjects())
            ScanHierarchy(root, scene.path, findings);
        return findings;
    }

    private static void ScanHierarchy(GameObject root, string assetPath, List<Finding> findings)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var go = t.gameObject;
            string objectPath = GetPath(t, root.transform);

            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            {
                findings.Add(new Finding
                {
                    Kind = Kind.MissingScript,
                    AssetPath = assetPath,
                    ObjectPath = objectPath,
                    ObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go),
                });
            }

            foreach (var component in go.GetComponents<Component>())
                if (component != null) ScanObject(component, assetPath, objectPath, findings, go);
        }
    }

    private static void ScanObject(Object target, string assetPath, string objectPath, List<Finding> findings, GameObject owner = null)
    {
        var so = new SerializedObject(target);
        var prop = so.GetIterator();
        while (prop.Next(true))
        {
            if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (prop.objectReferenceValue != null || !IsMissing(prop)) continue;
            if (prop.name == "m_CorrespondingSourceObject" || prop.name == "m_PrefabInstance" || prop.name == "m_PrefabAsset") continue;

            findings.Add(new Finding
            {
                Kind = Kind.MissingReference,
                AssetPath = assetPath,
                ObjectPath = objectPath,
                Component = target.GetType().Name,
                Property = prop.propertyPath,
                ObjectId = GlobalObjectId.GetGlobalObjectIdSlow(owner != null ? owner : target),
            });
        }
    }

    /// <summary>引用值为空但序列化里还记着一个对象 → 指向的资源被删了（Inspector 里显示 Missing）</summary>
    private static bool IsMissing(SerializedProperty prop)
    {
#pragma warning disable CS0618 // 新版本有替代 API，但这个在各版本都可用
        return prop.objectReferenceInstanceIDValue != 0;
#pragma warning restore CS0618
    }

    private static string GetPath(Transform t, Transform root)
    {
        var names = new List<string>();
        for (var cur = t; cur != null; cur = cur.parent)
        {
            names.Add(cur.name);
            if (cur == root) break;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    #endregion

    #region 修复

    /// <summary>移除 Prefab 中所有丢失的脚本组件，返回移除数量</summary>
    public static int RemoveMissingScriptsInPrefab(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            int removed = RemoveMissingScripts(root);
            if (removed > 0) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return removed;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>移除已打开场景中丢失的脚本组件（场景会被标记为已修改，需要手动保存），返回移除数量</summary>
    public static int RemoveMissingScriptsInScene(Scene scene)
    {
        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
            removed += RemoveMissingScripts(root, true);
        if (removed > 0) EditorSceneManager.MarkSceneDirty(scene);
        return removed;
    }

    private static int RemoveMissingScripts(GameObject root, bool undo = false)
    {
        int removed = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0) continue;
            if (undo) Undo.RegisterCompleteObjectUndo(t.gameObject, "移除丢失脚本");
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
        return removed;
    }

    #endregion
}
