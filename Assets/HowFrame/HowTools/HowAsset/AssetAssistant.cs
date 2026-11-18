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
                        Debug.LogWarning($"Text load failed: {request.error}");
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
        // 缓存 key = asset.name；同一资源可能同时属于多个 Label，用 _keyOwners 记录归属，
        // 只有所有归属 Label 都卸载后才移出缓存，避免缓存里留下已释放的资源引用。
        private static readonly Dictionary<string, Object> _cache = new Dictionary<string, Object>();
        private static readonly Dictionary<string, HashSet<string>> _keyOwners = new Dictionary<string, HashSet<string>>();
        private static readonly Dictionary<string, List<AsyncOperationHandle>> _handlesByLabel = new Dictionary<string, List<AsyncOperationHandle>>();
        private static readonly Dictionary<string, Task> _loadingLabels = new Dictionary<string, Task>();

        /// <summary>某个 Label 是否已加载完成（已在缓存中）</summary>
        public static bool IsLabelLoaded(string label) => _handlesByLabel.ContainsKey(label);

        // ---- 只读状态（调试窗口 / 健康检查用） ----

        /// <summary>已加载完成的 Label</summary>
        public static IEnumerable<string> LoadedLabels => _handlesByLabel.Keys;

        /// <summary>正在加载中的 Label</summary>
        public static IEnumerable<string> LoadingLabels => _loadingLabels.Keys;

        /// <summary>缓存中的资源数量</summary>
        public static int CachedCount => _cache.Count;

        /// <summary>遍历缓存：资源名、资源、归属 Label</summary>
        public static IEnumerable<(string key, Object asset, IReadOnlyCollection<string> labels)> CacheEntries()
        {
            foreach (var pair in _cache)
            {
                IReadOnlyCollection<string> owners = _keyOwners.TryGetValue(pair.Key, out var set)
                    ? set
                    : (IReadOnlyCollection<string>)Array.Empty<string>();
                yield return (pair.Key, pair.Value, owners);
            }
        }

        /// <summary>按 Label 批量加载并缓存。已加载 / 正在加载的 Label 不会重复加载。</summary>
        public static Task LoadLabelsAsync(Action onComplete = null, params string[] labels)
        {
            return LoadLabelsAsync(onComplete, null, labels);
        }

        public static Task LoadLabelsAsync(params string[] labels)
        {
            return LoadLabelsAsync(null, null, labels);
        }

        /// <summary>
        /// 按 Label 批量加载并缓存，onProgress 回调 0~1 的总体进度（按 Label 数量平均分配）。
        /// </summary>
        public static async Task LoadLabelsAsync(Action onComplete, Action<float> onProgress, params string[] labels)
        {
            var list = new List<string>();
            if (labels != null)
            {
                foreach (var label in labels)
                    if (!string.IsNullOrEmpty(label) && !list.Contains(label)) list.Add(label);
            }

            for (int i = 0; i < list.Count; i++)
            {
                int index = i;
                await LoadLabelAsync(list[i], p => onProgress?.Invoke((index + p) / list.Count));
            }

            onProgress?.Invoke(1f);
            onComplete?.Invoke();
        }

        private static Task LoadLabelAsync(string label, Action<float> onProgress)
        {
            if (_handlesByLabel.ContainsKey(label)) return Task.CompletedTask;
            if (_loadingLabels.TryGetValue(label, out var loading)) return loading;

            var task = LoadLabelCore(label, onProgress);
            if (!task.IsCompleted) _loadingLabels[label] = task;
            return task;
        }

        private static async Task LoadLabelCore(string label, Action<float> onProgress)
        {
            try
            {
                AsyncOperationHandle<IList<Object>> handle = Addressables.LoadAssetsAsync<Object>(label, null);
                while (!handle.IsDone)
                {
                    onProgress?.Invoke(handle.PercentComplete);
                    await Task.Yield();
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"加载 Label {label} 失败: {handle.OperationException}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    return;
                }

                _handlesByLabel[label] = new List<AsyncOperationHandle> { handle };
                foreach (var asset in handle.Result)
                {
                    if (asset == null) continue;
                    string key = asset.name;
                    if (!_cache.TryGetValue(key, out var existing))
                    {
                        _cache[key] = asset;
                        _keyOwners[key] = new HashSet<string> { label };
                    }
                    else if (existing == asset)
                    {
                        _keyOwners[key].Add(label);
                    }
                    else
                    {
                        Debug.LogWarning($"资源重名: {label} / {key}（{asset.GetType().Name}），已跳过，按名字取资源时只会拿到先加载的那个");
                    }
                }
                onProgress?.Invoke(1f);
            }
            catch (Exception e)
            {
                Debug.LogError($"加载 Label {label} 失败: {e}");
            }
            finally
            {
                _loadingLabels.Remove(label);
            }
        }

        /// <summary>卸载 Label：释放句柄，并把只属于这些 Label 的资源移出缓存。</summary>
        public static void ReleaseLabels(Action onComplete = null, params string[] labels)
        {
            foreach (var label in labels)
            {
                if (!_handlesByLabel.TryGetValue(label, out var list)) continue;

                foreach (var key in new List<string>(_keyOwners.Keys))
                {
                    var owners = _keyOwners[key];
                    if (!owners.Remove(label) || owners.Count > 0) continue;
                    _keyOwners.Remove(key);
                    _cache.Remove(key);
                }

                foreach (var handle in list)
                    if (handle.IsValid()) Addressables.Release(handle);
                _handlesByLabel.Remove(label);
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

        /// <summary>从缓存取资源，取不到时不打警告</summary>
        public static bool TryAddressableGet<T>(string name, out T asset) where T : Object
        {
            asset = _cache.TryGetValue(name, out var obj) ? obj as T : null;
            return asset != null;
        }

        public static void ReleaseAll()
        {
            foreach (var list in _handlesByLabel.Values)
            {
                foreach (var handle in list)
                    if (handle.IsValid()) Addressables.Release(handle);
            }
            _handlesByLabel.Clear();
            _keyOwners.Clear();
            _cache.Clear();
        }
        #endregion

        internal static void Wake() { }
    }
}
