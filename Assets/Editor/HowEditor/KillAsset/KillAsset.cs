#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using LitJson;

/// <summary>
/// 统一的资源清理工具：扫描未使用的资源并删除
/// 配置通过 rule.json 文件管理
/// </summary>
public static class KillAsset
{
    private static string RuleJsonPath => Path.Combine(Application.dataPath, "Editor", "HowEditor", "KillAsset", "rule.json");

    [MenuItem("Tools/KillAsset")]
    public static void Execute()
    {
        try
        {
            // 读取配置
            KillAssetRule rule = LoadRule();
            if (rule == null)
            {
                Debug.LogError($"无法加载配置文件: {RuleJsonPath}");
                return;
            }

            // 验证配置
            if (string.IsNullOrEmpty(rule.targetAssetPath))
            {
                Debug.LogError("配置文件中 targetAssetPath 不能为空！");
                return;
            }

            Debug.Log("=".PadRight(60, '='));
            Debug.Log("开始执行 KillAsset...");
            Debug.Log($"目标资源文件夹: {rule.targetAssetPath}");
            Debug.Log($"起点文件夹数量: {rule.entryPaths?.Count ?? 0}");
            Debug.Log($"忽略规则数量: {rule.ignoreRules?.Count ?? 0}");
            Debug.Log("=".PadRight(60, '='));

            // 1. 扫描未使用的资源
            List<string> unusedAssets = ScanUnusedAssets(rule);
            if (unusedAssets == null || unusedAssets.Count == 0)
            {
                Debug.Log("没有找到未使用的资源，退出。");
                return;
            }

            // 2. 应用忽略规则
            List<string> filteredAssets = ApplyIgnoreRules(unusedAssets, rule.ignoreRules, rule.targetAssetPath);
            if (filteredAssets == null || filteredAssets.Count == 0)
            {
                Debug.Log("应用忽略规则后，没有需要删除的资源，退出。");
                return;
            }

            // 安全检查：如果待删除的资源数量过多，需要确认
            string targetAssetPathUnity = ConvertToUnityPath(rule.targetAssetPath);
            if (!string.IsNullOrEmpty(targetAssetPathUnity))
            {
                HashSet<string> targetAssets = GetAllAssetsInPath(targetAssetPathUnity);
                float deleteRatio = (float)filteredAssets.Count / targetAssets.Count;
                
                if (deleteRatio > 0.5f)
                {
                    string message = $"警告：将要删除 {filteredAssets.Count} 个资源，占目标文件夹的 {deleteRatio:P1}！\n\n" +
                                   $"目标文件夹: {targetAssetPathUnity}\n" +
                                   $"总资源数: {targetAssets.Count}\n" +
                                   $"待删除数: {filteredAssets.Count}\n\n" +
                                   $"是否继续？";
                    
                    if (!EditorUtility.DisplayDialog("危险操作确认", message, "继续删除", "取消"))
                    {
                        Debug.Log("用户取消了删除操作。");
                        return;
                    }
                }
            }

            // 3. 删除资源
            DeleteAssets(filteredAssets, rule.targetAssetPath);

            Debug.Log("=".PadRight(60, '='));
            Debug.Log("KillAsset 执行完成！");
            Debug.Log("=".PadRight(60, '='));

            // 刷新资源数据库
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"KillAsset 执行失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    private static KillAssetRule LoadRule()
    {
        string jsonPath = RuleJsonPath;
        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning($"配置文件不存在，创建默认配置: {jsonPath}");
            CreateDefaultRule(jsonPath);
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            KillAssetRule rule = JsonMapper.ToObject<KillAssetRule>(jsonContent);
            return rule;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取配置文件失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 创建默认配置文件
    /// </summary>
    private static void CreateDefaultRule(string jsonPath)
    {
        KillAssetRule defaultRule = new KillAssetRule
        {
            entryPaths = new List<string>
            {
                "Assets/Scenes",
                "Assets/Resources",
                "Assets/GameRes"
            },
            targetAssetPath = "Assets/Art",
            ignoreRules = new List<string>
            {
                "*.mat",
                "*.meta"
            }
        };

        try
        {
            string jsonContent = JsonMapper.ToJson(defaultRule);
            File.WriteAllText(jsonPath, jsonContent);
            Debug.Log($"已创建默认配置文件: {jsonPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"创建默认配置文件失败: {e.Message}");
        }
    }

    /// <summary>
    /// 扫描未使用的资源
    /// </summary>
    private static List<string> ScanUnusedAssets(KillAssetRule rule)
    {
        // 转换为Unity资源路径格式
        string targetAssetPathUnity = ConvertToUnityPath(rule.targetAssetPath);
        if (string.IsNullOrEmpty(targetAssetPathUnity))
        {
            Debug.LogError($"无效的目标资源路径: {rule.targetAssetPath}");
            return new List<string>();
        }

        Debug.Log($"开始扫描资源依赖...");
        Debug.Log($"目标资源文件夹: {targetAssetPathUnity}");

        // 获取所有入口资源
        HashSet<string> entryAssets = new HashSet<string>();

        if (rule.entryPaths != null && rule.entryPaths.Count > 0)
        {
            foreach (string entryPath in rule.entryPaths)
            {
                if (string.IsNullOrEmpty(entryPath))
                {
                    Debug.LogWarning("发现空的入口路径，跳过");
                    continue;
                }

                string entryPathUnity = ConvertToUnityPath(entryPath);
                if (string.IsNullOrEmpty(entryPathUnity))
                {
                    Debug.LogWarning($"无法转换入口路径为 Unity 路径: {entryPath}，跳过");
                    continue;
                }

                // 检查路径是否存在
                if (!AssetDatabase.IsValidFolder(entryPathUnity) && 
                    !File.Exists(entryPathUnity) && 
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entryPathUnity) == null)
                {
                    Debug.LogWarning($"入口路径不存在或无效: {entryPathUnity}，跳过");
                    continue;
                }

                var assets = GetAllAssetsInPath(entryPathUnity);
                entryAssets.UnionWith(assets);
                Debug.Log($"从 {entryPathUnity} 找到 {assets.Count} 个入口资源");
            }
        }
        else
        {
            // 如果没有指定入口路径，扫描整个项目
            Debug.Log("未指定入口路径，扫描整个项目");
            entryAssets = GetAllAssetsInPath("Assets");
        }

        Debug.Log($"总共找到 {entryAssets.Count} 个入口资源");

        // 安全检查：如果没有入口资源，报错并停止
        if (entryAssets.Count == 0)
        {
            Debug.LogError("错误：没有找到任何入口资源！请检查 entryPaths 配置是否正确。");
            return new List<string>();
        }

        // 获取所有依赖
        HashSet<string> allDependencies = new HashSet<string>();
        
        // 重要：先将所有入口资源本身标记为已使用（防止入口资源被误删）
        foreach (string entryAsset in entryAssets)
        {
            allDependencies.Add(entryAsset);
        }

        int processed = 0;

        foreach (string entryAsset in entryAssets)
        {
            processed++;
            if (processed % 100 == 0)
            {
                EditorUtility.DisplayProgressBar("扫描依赖", $"处理中... {processed}/{entryAssets.Count}", (float)processed / entryAssets.Count);
            }

            // 获取该资源的所有依赖（包括递归依赖）
            string[] dependencies = AssetDatabase.GetDependencies(entryAsset, true);
            foreach (string dep in dependencies)
            {
                allDependencies.Add(dep);
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"找到 {allDependencies.Count} 个被依赖的资源（包含 {entryAssets.Count} 个入口资源）");

        // 安全检查：如果依赖数量异常少，可能是配置错误
        if (allDependencies.Count < entryAssets.Count + 10)
        {
            Debug.LogWarning($"警告：找到的依赖数量异常少（{allDependencies.Count}），可能是入口路径配置错误！");
            Debug.LogWarning("建议检查 entryPaths 配置的路径是否存在且包含资源。");
        }

        // 获取目标文件夹中的所有资源
        HashSet<string> targetAssets = GetAllAssetsInPath(targetAssetPathUnity);
        Debug.Log($"目标文件夹中有 {targetAssets.Count} 个资源");

        // 找出未被依赖的资源
        List<string> unusedAssets = new List<string>();
        foreach (string targetAsset in targetAssets)
        {
            if (!allDependencies.Contains(targetAsset))
            {
                unusedAssets.Add(targetAsset);
            }
        }

        Debug.Log($"目标文件夹中未被依赖的资源: {unusedAssets.Count} 个");

        return unusedAssets;
    }

    /// <summary>
    /// 应用忽略规则
    /// </summary>
    private static List<string> ApplyIgnoreRules(List<string> assets, List<string> ignoreRules, string targetAssetPath)
    {
        if (ignoreRules == null || ignoreRules.Count == 0)
        {
            return assets;
        }

        List<string> filtered = new List<string>();
        int ignoredCount = 0;

        foreach (string assetPath in assets)
        {
            // 计算相对于目标文件夹的相对路径
            string relativePath = GetRelativeUnityPath(assetPath, ConvertToUnityPath(targetAssetPath));
            if (string.IsNullOrEmpty(relativePath))
            {
                // 如果无法计算相对路径，使用完整路径
                relativePath = assetPath;
            }

            // 检查是否匹配忽略规则
            if (ShouldIgnore(relativePath, ignoreRules))
            {
                ignoredCount++;
                continue;
            }

            filtered.Add(assetPath);
        }

        Debug.Log($"应用忽略规则后: {filtered.Count} 个资源待删除，{ignoredCount} 个资源被忽略");

        return filtered;
    }

    /// <summary>
    /// 判断是否应该忽略
    /// </summary>
    private static bool ShouldIgnore(string path, List<string> ignoreRules)
    {
        // 统一使用正斜杠
        path = path.Replace('\\', '/');

        foreach (string rule in ignoreRules)
        {
            if (string.IsNullOrEmpty(rule))
                continue;

            string normalizedRule = rule.Trim();

            // 1. 目录规则（以 / 或 \ 结尾）
            if (normalizedRule.EndsWith("/") || normalizedRule.EndsWith("\\"))
            {
                string dirRule = normalizedRule.TrimEnd('/', '\\');
                if (path.StartsWith(dirRule + "/") || path == dirRule)
                {
                    return true;
                }
            }

            // 2. 通配符规则
            if (normalizedRule.Contains("*") || normalizedRule.Contains("?"))
            {
                // 转换为正则表达式
                string pattern = "^" + Regex.Escape(normalizedRule)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                if (Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }

                // 也检查文件名是否匹配
                string fileName = Path.GetFileName(path);
                if (Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            // 3. 完全匹配
            if (path == normalizedRule)
            {
                return true;
            }

            // 4. 子字符串匹配
            if (path.Contains(normalizedRule))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 删除资源
    /// </summary>
    private static void DeleteAssets(List<string> assets, string targetAssetPath)
    {
        if (assets == null || assets.Count == 0)
        {
            Debug.Log("没有需要删除的资源");
            return;
        }

        Debug.Log($"开始删除 {assets.Count} 个资源...");

        int deletedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < assets.Count; i++)
        {
            string assetPath = assets[i];

            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("删除资源", $"删除中... {i}/{assets.Count}", (float)i / assets.Count);
            }

            try
            {
                // 重要：再次检查，确保不删除文件夹
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    Debug.LogWarning($"跳过文件夹（不删除）: {assetPath}");
                    continue;
                }

                // 使用 AssetDatabase.DeleteAsset 删除资源（会自动删除 .meta 文件）
                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    deletedCount++;
                }
                else
                {
                    Debug.LogWarning($"删除失败: {assetPath}");
                    failedCount++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"删除资源时出错: {assetPath} -> {e.Message}");
                failedCount++;
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"删除完成: 成功 {deletedCount} 个，失败 {failedCount} 个");
    }

    /// <summary>
    /// 将绝对路径转换为Unity资源路径（Assets/...）
    /// </summary>
    private static string ConvertToUnityPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "";

        // 如果已经是Unity路径格式，直接返回
        if (path.StartsWith("Assets/"))
            return path;

        // 转换为绝对路径
        string fullPath = Path.GetFullPath(path);
        string assetsPath = Path.GetFullPath(Application.dataPath);

        // 检查是否在Assets目录下
        if (fullPath.StartsWith(assetsPath))
        {
            string relativePath = fullPath.Substring(assetsPath.Length);
            relativePath = relativePath.Replace('\\', '/');
            if (relativePath.StartsWith("/"))
                relativePath = relativePath.Substring(1);
            return "Assets/" + relativePath;
        }

        return "";
    }

    /// <summary>
    /// 获取指定路径下的所有资源文件（不包括文件夹）
    /// </summary>
    private static HashSet<string> GetAllAssetsInPath(string unityPath)
    {
        HashSet<string> assets = new HashSet<string>();

        if (string.IsNullOrEmpty(unityPath))
            return assets;

        // 如果是文件夹，获取文件夹下的所有资源
        if (AssetDatabase.IsValidFolder(unityPath))
        {
            string[] guids = AssetDatabase.FindAssets("", new[] { unityPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // 重要：只添加文件，不添加文件夹
                    // AssetDatabase.IsValidFolder 可以判断是否为文件夹
                    if (!AssetDatabase.IsValidFolder(assetPath))
                    {
                        assets.Add(assetPath);
                    }
                }
            }
        }
        else
        {
            // 如果是单个文件，检查不是文件夹后再添加
            if (!AssetDatabase.IsValidFolder(unityPath))
            {
                if (File.Exists(unityPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(unityPath) != null)
                {
                    assets.Add(unityPath);
                }
            }
        }

        return assets;
    }

    /// <summary>
    /// 计算Unity路径格式的相对路径（相对于参考路径）
    /// </summary>
    private static string GetRelativeUnityPath(string assetPath, string referencePath)
    {
        if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(referencePath))
            return "";

        // 确保都是Unity路径格式
        if (!assetPath.StartsWith("Assets/") || !referencePath.StartsWith("Assets/"))
            return "";

        // 标准化路径（移除末尾斜杠）
        string normalizedRef = referencePath.TrimEnd('/');
        string normalizedAsset = assetPath.TrimEnd('/');

        // 如果资源路径就是参考路径本身，返回空
        if (normalizedAsset == normalizedRef)
            return "";

        // 如果资源路径以参考路径开头，计算相对路径
        if (normalizedAsset.StartsWith(normalizedRef + "/"))
        {
            string relative = normalizedAsset.Substring(normalizedRef.Length + 1);
            return relative;
        }

        return "";
    }
}

/// <summary>
/// KillAsset 配置规则
/// </summary>
[Serializable]
public class KillAssetRule
{
    /// <summary>
    /// 起点文件夹列表（可选），用于扫描依赖的入口
    /// 如果为空，将扫描整个项目
    /// </summary>
    public List<string> entryPaths;

    /// <summary>
    /// 目标资源文件夹（必需），要清理的资源所在文件夹
    /// </summary>
    public string targetAssetPath;

    /// <summary>
    /// 忽略规则列表（可选），类似 gitignore 的规则
    /// 支持：
    /// - 通配符: *.mat, *.meta
    /// - 目录匹配: art/ui/, effect/
    /// - 完整路径: test.fbx
    /// - 部分匹配: 包含指定字符串的路径
    /// </summary>
    public List<string> ignoreRules;
}
#endif

