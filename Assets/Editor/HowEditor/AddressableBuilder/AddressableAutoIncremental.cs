#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public class AddressableAutoIncremental : AssetPostprocessor
{
    private static readonly string RootFolder = "Assets/GameRes";

    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        // ---------------- 新增/修改 ----------------
        foreach (var assetPath in importedAssets)
        {
            if (!assetPath.StartsWith(RootFolder)) continue;
            if (assetPath.EndsWith(".meta") || assetPath.EndsWith(".cs") || assetPath.EndsWith(".dll")) continue;
            if (AssetDatabase.IsValidFolder(assetPath)) continue;

            string groupName = GetGroupName(assetPath);
            var group = string.IsNullOrEmpty(groupName) ? settings.DefaultGroup : GetOrCreateGroup(settings, groupName);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);

            // 取文件名（不带扩展名）作为地址
            entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }

        // ---------------- 删除 ----------------
        foreach (var assetPath in deletedAssets)
        {
            if (!assetPath.StartsWith(RootFolder)) continue;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(guid))
                settings.RemoveAssetEntry(guid);
        }

        // ---------------- 移动/重命名 ----------------
        for (int i = 0; i < movedAssets.Length; i++)
        {
            string newPath = movedAssets[i];
            string oldPath = movedFromAssetPaths[i];

            if (!newPath.StartsWith(RootFolder)) continue;

            string guid = AssetDatabase.AssetPathToGUID(newPath);
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                // 更新地址（只保留文件名，不带扩展名）
                entry.address = System.IO.Path.GetFileNameWithoutExtension(newPath);

                // 更新分组
                string groupName = GetGroupName(newPath);
                var group = string.IsNullOrEmpty(groupName) ? settings.DefaultGroup : GetOrCreateGroup(settings, groupName);
                if (entry.parentGroup != group)
                    settings.MoveEntry(entry, group, false);
            }
        }

        if (importedAssets.Length + deletedAssets.Length + movedAssets.Length > 0)
        {
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            Debug.Log("✅ Addressables 增量同步完成（地址已简化）");
        }
    }

    private static string GetGroupName(string assetPath)
    {
        string relative = assetPath.Substring(RootFolder.Length + 1); // 去掉 RootFolder + "/"
        string[] parts = relative.Split('/');
        return parts.Length > 1 ? parts[0] : null; // 子文件夹 -> 独立组，根目录 -> 默认组
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group != null) return group;

        group = settings.CreateGroup(groupName, false, false, false, null,
            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        return group;
    }
}
#endif
