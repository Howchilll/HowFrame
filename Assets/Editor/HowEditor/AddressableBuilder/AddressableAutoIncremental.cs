#define EDITOR
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public class AddressableAutoIncremental : AssetPostprocessor
{
    // 自动收录根目录，来自 HowFrame 设置（去掉结尾的 /）
    private static string RootFolder => (HowFrameSettings.instance.addressableRootDir ?? "").Replace('\\', '/').TrimEnd('/');

    private static bool InRoot(string assetPath) =>
        !string.IsNullOrEmpty(RootFolder) && assetPath.StartsWith(RootFolder + "/", System.StringComparison.Ordinal);

    private static bool IsCandidate(string assetPath) =>
        InRoot(assetPath) && !AssetDatabase.IsValidFolder(assetPath) &&
        !assetPath.EndsWith(".meta") && !assetPath.EndsWith(".cs") && !assetPath.EndsWith(".dll");

    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("⚠️ Addressable Settings 未找到，请先打开 Addressables 窗口创建 Settings.");
            return;
        }

        bool hasRelevantChange =
            importedAssets.Any(InRoot) || deletedAssets.Any(InRoot) ||
            movedAssets.Any(InRoot) || movedFromAssetPaths.Any(InRoot);

        if (!hasRelevantChange) return;

        // ---------------- 新增/修改 ----------------
        foreach (var assetPath in importedAssets)
        {
            if (!IsCandidate(assetPath)) continue;

            string groupName = GetGroupName(assetPath);
            var group = string.IsNullOrEmpty(groupName) ? settings.DefaultGroup : GetOrCreateGroup(settings, groupName);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
                // 只给新加进来的资源起地址：已有的可能被手动 / 菜单改过（如背景音乐固定叫 bgm_main），
                // 改个导入设置触发重新导入时不能冲掉
                entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
            else if (entry.parentGroup != group)
            {
                settings.MoveEntry(entry, group, false);
            }
        }

        // ---------------- 删除 ----------------
        foreach (var assetPath in deletedAssets)
        {
            if (!InRoot(assetPath)) continue;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) continue;

            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                settings.RemoveAssetEntry(guid);
            }
        }

        // ---------------- 移动/重命名 ----------------
        for (int i = 0; i < movedAssets.Length; i++)
        {
            string newPath = movedAssets[i];

            // 只处理最终落在 RootFolder 内的资源（移入或在内部移动）；移出的不处理
            if (!IsCandidate(newPath))
                continue;

            string guid = AssetDatabase.AssetPathToGUID(newPath);
            var entry = settings.FindAssetEntry(guid);

            string groupName = GetGroupName(newPath);
            var group = string.IsNullOrEmpty(groupName) ? settings.DefaultGroup : GetOrCreateGroup(settings, groupName);

            if (entry == null)
            {
                // 从外部拖入 GameRes 或全新资源
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
            }
            else if (entry.parentGroup != group)
            {
                settings.MoveEntry(entry, group, false);
            }

            entry.address = System.IO.Path.GetFileNameWithoutExtension(newPath);
        }

        // ---------------- 保存修改 ----------------
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, new List<AddressableAssetEntry>(), true);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Addressables 增量同步完成（自动加入并简化地址）");
    }

    private static string GetGroupName(string assetPath)
    {
        string relative = assetPath.Substring(RootFolder.Length + 1);
        string[] parts = relative.Split('/');
        return parts.Length > 1 ? parts[0] : null;
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group != null) return group;

        group = settings.CreateGroup(groupName, false, false, false, null,
            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
        if (bundleSchema != null)
            bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

        return group;
    }
}
#endif
