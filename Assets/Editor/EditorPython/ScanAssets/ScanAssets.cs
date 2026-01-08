using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LitJson;
using System;
public static class ScanAssets
{
    [MenuItem("Tools/PyFunctions/ScanAssets")]
    public static void Run()
    {
        ScanAssetsParameterWindow.ShowWindow();
    }

    public static void Execute(string scenePath, string resourcesPath, string streamingAssetsPath, string addressablePath, string artAssetPath)
    {
        if (string.IsNullOrEmpty(artAssetPath))
        {
            Debug.LogError("Art资源文件夹路径不能为空！");
            return;
        }

        // 转换为Unity资源路径格式（Assets/...）
        string artAssetPathUnity = ConvertToUnityPath(artAssetPath);
        if (string.IsNullOrEmpty(artAssetPathUnity))
        {
            Debug.LogError($"无效的Art资源路径: {artAssetPath}");
            return;
        }

        Debug.Log($"开始扫描资源依赖...");
        Debug.Log($"Art资源文件夹: {artAssetPathUnity}");

        // 获取所有入口资源（从 scenePath, resourcesPath, addressablePath）
        HashSet<string> entryAssets = new HashSet<string>();
        
        if (!string.IsNullOrEmpty(scenePath))
        {
            string scenePathUnity = ConvertToUnityPath(scenePath);
            if (!string.IsNullOrEmpty(scenePathUnity))
            {
                var assets = GetAllAssetsInPath(scenePathUnity);
                entryAssets.UnionWith(assets);
                Debug.Log($"从 scenePath 找到 {assets.Count} 个入口资源");
            }
        }

        if (!string.IsNullOrEmpty(resourcesPath))
        {
            string resourcesPathUnity = ConvertToUnityPath(resourcesPath);
            if (!string.IsNullOrEmpty(resourcesPathUnity))
            {
                var assets = GetAllAssetsInPath(resourcesPathUnity);
                entryAssets.UnionWith(assets);
                Debug.Log($"从 resourcesPath 找到 {assets.Count} 个入口资源");
            }
        }

        if (!string.IsNullOrEmpty(addressablePath))
        {
            string addressablePathUnity = ConvertToUnityPath(addressablePath);
            if (!string.IsNullOrEmpty(addressablePathUnity))
            {
                var assets = GetAllAssetsInPath(addressablePathUnity);
                entryAssets.UnionWith(assets);
                Debug.Log($"从 addressablePath 找到 {assets.Count} 个入口资源");
            }
        }

        // StreamingAssets 不含 GUID 引用，不需要加入入口（与 Python 脚本逻辑一致）

        Debug.Log($"总共找到 {entryAssets.Count} 个入口资源");

        // 获取所有依赖
        HashSet<string> allDependencies = new HashSet<string>();
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

        Debug.Log($"找到 {allDependencies.Count} 个被依赖的资源");

        // 获取Art文件夹中的所有资源
        HashSet<string> artAssets = GetAllAssetsInPath(artAssetPathUnity);
        Debug.Log($"Art文件夹中有 {artAssets.Count} 个资源");

        // 找出Art文件夹中未被依赖的资源
        List<string> unusedArtAssets = new List<string>();
        foreach (string artAsset in artAssets)
        {
            if (!allDependencies.Contains(artAsset))
            {
                unusedArtAssets.Add(artAsset);
            }
        }

        Debug.Log($"Art文件夹中未被依赖的资源: {unusedArtAssets.Count} 个");

        // 输出到 output.txt（与 Python 脚本输出格式一致）
        string outputPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanAssets", "output.txt");
        outputPath = Path.GetFullPath(outputPath);
        
        try
        {
            // 清空输出文件（与 Python 脚本逻辑一致）
            if (File.Exists(outputPath))
            {
                File.WriteAllText(outputPath, "");
            }

            // 获取 artAssetPath 的绝对路径，用于计算相对路径
            string artAssetPathAbsolute;
            if (artAssetPath.StartsWith("Assets/"))
            {
                // Unity 路径格式，转换为绝对路径
                artAssetPathAbsolute = Path.Combine(Application.dataPath, artAssetPath.Replace("Assets/", "").Replace('/', Path.DirectorySeparatorChar));
            }
            else
            {
                artAssetPathAbsolute = Path.GetFullPath(artAssetPath);
            }
            
            if (!artAssetPathAbsolute.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                !artAssetPathAbsolute.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                artAssetPathAbsolute += Path.DirectorySeparatorChar;
            }

            // 写入未引用的资源路径（相对路径，相对于 artAssetPath）
            using (StreamWriter writer = new StreamWriter(outputPath, true, System.Text.Encoding.UTF8))
            {
                foreach (string assetPath in unusedArtAssets)
                {
                    // 转换为绝对路径
                    string assetAbsolutePath;
                    if (assetPath.StartsWith("Assets/"))
                    {
                        // Unity 路径格式，转换为绝对路径
                        assetAbsolutePath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", "").Replace('/', Path.DirectorySeparatorChar));
                    }
                    else
                    {
                        assetAbsolutePath = Path.GetFullPath(assetPath);
                    }

                    // 计算相对路径
                    string relativePath = GetRelativePath(assetAbsolutePath, artAssetPathAbsolute);
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        // 统一使用正斜杠（与 Python 脚本输出格式一致）
                        relativePath = relativePath.Replace('\\', '/');
                        writer.WriteLine(relativePath);
                    }
                }
            }
            
            Debug.Log($"扫描完成，共发现未引用资源：{unusedArtAssets.Count}");
            Debug.Log($"结果已保存到: {outputPath}");
            
            // 刷新资源数据库
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存输出文件失败: {e.Message}\n{e.StackTrace}");
        }
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
    /// 获取指定路径下的所有资源文件
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
                    assets.Add(assetPath);
                }
            }
        }
        else
        {
            // 如果是单个文件，直接添加
            if (File.Exists(unityPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(unityPath) != null)
            {
                assets.Add(unityPath);
            }
        }

        return assets;
    }

    /// <summary>
    /// 计算相对路径
    /// </summary>
    private static string GetRelativePath(string filePath, string referencePath)
    {
        try
        {
            Uri fileUri = new Uri(filePath);
            Uri referenceUri = new Uri(referencePath);
            Uri relativeUri = referenceUri.MakeRelativeUri(fileUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }
        catch
        {
            // 如果 URI 方式失败，使用路径方式
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string refPath = Path.GetFullPath(referencePath);
                
                if (!refPath.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                    !refPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    refPath += Path.DirectorySeparatorChar;
                }

                if (fullPath.StartsWith(refPath))
                {
                    return fullPath.Substring(refPath.Length);
                }
            }
            catch { }
            
            return "";
        }
    }
}

public class ScanAssetsParameterWindow : EditorWindow
{
    private string scenePath = "";
    private string resourcesPath = "";
    private string streamingAssetsPath = "";
    private string addressablePath = "";
    private string artAssetPath = "";

    public static void ShowWindow()
    {
        ScanAssetsParameterWindow window = GetWindow<ScanAssetsParameterWindow>("ScanAssets 参数");
        window.Show();
    }

    private void OnEnable()
    {
        LoadParameters();
    }

    private void LoadParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanAssets", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var paramDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonContent);
                if (paramDict != null)
                {
                    if (paramDict.ContainsKey("scenePath"))
                        scenePath = paramDict["scenePath"];
                    if (paramDict.ContainsKey("resourcesPath"))
                        resourcesPath = paramDict["resourcesPath"];
                    if (paramDict.ContainsKey("streamingAssetsPath"))
                        streamingAssetsPath = paramDict["streamingAssetsPath"];
                    if (paramDict.ContainsKey("addressablePath"))
                        addressablePath = paramDict["addressablePath"];
                    if (paramDict.ContainsKey("artAssetPath"))
                        artAssetPath = paramDict["artAssetPath"];
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"读取参数配置文件失败: {e.Message}");
            }
        }
    }

    private void SaveParameters()
    {
        string jsonPath = Path.Combine(Application.dataPath, "Editor", "EditorPython", "ScanAssets", "parameters.json");
        jsonPath = Path.GetFullPath(jsonPath);

        try
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["scenePath"] = scenePath;
            paramDict["resourcesPath"] = resourcesPath;
            paramDict["streamingAssetsPath"] = streamingAssetsPath;
            paramDict["addressablePath"] = addressablePath;
            paramDict["artAssetPath"] = artAssetPath;
            string jsonContent = JsonMapper.ToJson(paramDict);
            File.WriteAllText(jsonPath, jsonContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存参数配置文件失败: {e.Message}");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("ScanAssets 参数设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        scenePath = EditorGUILayout.TextField("scenePath", scenePath);
        resourcesPath = EditorGUILayout.TextField("resourcesPath", resourcesPath);
        streamingAssetsPath = EditorGUILayout.TextField("streamingAssetsPath", streamingAssetsPath);
        addressablePath = EditorGUILayout.TextField("addressablePath", addressablePath);
        artAssetPath = EditorGUILayout.TextField("artAssetPath", artAssetPath);
        GUILayout.Space(20);
        if (GUILayout.Button("执行", GUILayout.Height(30)))
        {
            SaveParameters();
            ScanAssets.Execute(scenePath, resourcesPath, streamingAssetsPath, addressablePath, artAssetPath);
            Close();
        }
    }
}
