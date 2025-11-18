using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace HowFrame
{
    /// <summary>
    /// 资源加载助手类
    /// 提供三种资源加载方式：
    /// 1. ImportAsset - 从 StreamingAssets 文件夹异步加载资源（支持跨平台）
    /// 2. LoadAsset - 从 Resources 文件夹同步加载资源
    /// 3. AddressAsset - 从 Addressables 系统异步加载资源（支持按 Label 批量加载、缓存、卸载）
    /// </summary>
    public static class AssetAssistant
    {
        #region StreamingAssets / Resources / 单资源 Addressables
        public static async Task<T> ImportAsset<T>(string relativePath) where T : Object
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            string extension = Path.GetExtension(fullPath).ToLower();

            if (!fullPath.StartsWith("jar:") && !fullPath.StartsWith("file:"))
                fullPath = "file://" + fullPath;

            if (typeof(T) == typeof(AudioClip))
            {
                AudioType audioType = AudioType.UNKNOWN;
                switch (extension)
                {
                    case ".wav": audioType = AudioType.WAV; break;
                    case ".mp3": audioType = AudioType.MPEG; break;
                    case ".ogg": audioType = AudioType.OGGVORBIS; break;
                    default:
                        Debug.LogError($"Unsupported audio format: {extension}");
                        return default;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType))
                {
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Audio load failed: {request.error}");
                        return default;
                    }
                    return (T)(object)DownloadHandlerAudioClip.GetContent(request);
                }
            }

            if (typeof(T) == typeof(Texture2D))
            {
                using (var request = UnityWebRequestTexture.GetTexture(fullPath))
                {
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Texture load failed: {request.error}");
                        return default;
                    }
                    return (T)(object)DownloadHandlerTexture.GetContent(request);
                }
            }

            if (typeof(T) == typeof(TextAsset))
            {
                using (var request = UnityWebRequest.Get(fullPath))
                {
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Text load failed: {request.error}");
                        return default;
                    }
                    string text = request.downloadHandler.text;
                    return (T)(object)new TextAsset(text);
                }
            }

            Debug.LogError($"Unsupported type {typeof(T).Name} for file: {relativePath}");
            return default;
        }

        public static T LoadAsset<T>(string fileName) where T : Object
        {
            return Resources.Load<T>(fileName);
        }

        public static async Task<T> AddressAsset<T>(string address, float delaySeconds = 0f) where T : Object
        {
            T asset;
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            try
            {
                asset = await handle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"Addressables加载失败: {address} \n{e}");
                return null;
            }

            if (delaySeconds > 0)
                _ = DelayRelease(handle, delaySeconds);
            return asset;
        }

        private static async Task DelayRelease<T>(AsyncOperationHandle<T> handle, float delaySeconds) where T : Object
        {
            await Task.Delay((int)(delaySeconds * 1000));
            Addressables.Release(handle);
        }
        #endregion

        #region 批量 Label 加载 + 缓存 + 卸载
        // 缓存 key = asset.name
        private static Dictionary<string, Object> _cache = new Dictionary<string, Object>();
        private static Dictionary<string, List<AsyncOperationHandle>> _handlesByLabel = new Dictionary<string, List<AsyncOperationHandle>>();
        
        public static async Task LoadLabelsAsync(Action onComplete = null, params string[] labels)
        {
            foreach (var label in labels)
            {
                AsyncOperationHandle<IList<Object>> handle = Addressables.LoadAssetsAsync<Object>(
                    label,
                    null
                );

                if (!_handlesByLabel.TryGetValue(label, out var list))
                {
                    list = new List<AsyncOperationHandle>();
                    _handlesByLabel[label] = list;
                }
                list.Add(handle);

                try
                {
                    var assets = await handle.Task;
                    foreach (var asset in assets)
                    {
                        string key = asset.name;
                        if (!_cache.ContainsKey(key))
                        {
                            _cache[key] = asset;
                            Debug.Log($"[Addressable Loaded] {label} / {key}");
                        }
                        else
                        {
                            Debug.LogWarning($"资源重复: {label} / {key}, 已跳过");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载 Label {label} 失败: {e}");
                }
            }

            onComplete?.Invoke();
        }

        public static Task LoadLabelsAsync(params string[] labels)
        {
            // 直接返回 Task，保证可以 await
            return LoadLabelsAsync(null, labels);
        }
        
        public static void ReleaseLabels(Action onComplete = null, params string[] labels)
        {
            foreach (var label in labels)
            {
                if (_handlesByLabel.TryGetValue(label, out var list))
                {
                    foreach (var handle in list)
                        Addressables.Release(handle);
                    list.Clear();
                    _handlesByLabel.Remove(label);
                }
                // 缓存保留，避免资源同时属于多个 Label 被误清
            }

            onComplete?.Invoke();
        }
        public static void ReleaseLabels(params string[] labels)
        {
            ReleaseLabels(null, labels);
        }
        public static T AddressableGet<T>(string name) where T : Object
        {
            if (_cache.TryGetValue(name, out var obj))
                return obj as T;
            Debug.LogWarning($"AssetAssistant 没有找到资源: {name}");
            return null;
        }
        public static void ReleaseAll()
        {
            foreach (var list in _handlesByLabel.Values)
            {
                foreach (var handle in list)
                    Addressables.Release(handle);
            }
            _handlesByLabel.Clear();
            _cache.Clear();
        }
        #endregion

        internal static void Wake() { }
    }
}
