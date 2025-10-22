using System;using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using UnityEngine.AddressableAssets;

namespace HowFrame
{

public enum E_AssetType
{
    Audio,
    UI,
    Prefab,
    SO,
    Instance
}





public static class AssetAssistant
{
 public static async Task<T> ImportAsset<T>(string relativePath) where T : Object
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
        string extension = Path.GetExtension(fullPath).ToLower();

        // Android 上 streamingAssetsPath 是一个 URI，例如 "jar:file:///data/app/xxx.apk!/assets/"
        // 所以路径要统一成 URI 格式
        if (!fullPath.StartsWith("jar:") && !fullPath.StartsWith("file:"))
            fullPath = "file://" + fullPath;

        // 🧩 音频文件
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

        // 🧩 图片文件
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

        // 🧩 文本文件（json、txt、xml等）
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

    public static T LoadAsset<T>(string fileName,E_AssetType type) where T : Object
    {
        switch (type)
        {
            case E_AssetType.Audio:
                return Resources.Load<T>("Sounds/" + fileName);
            case E_AssetType.UI:
                return Resources.Load<T>("UI/" + fileName);
            case E_AssetType.Prefab:
                return Resources.Load<T>("Prefabs/" + fileName);
            case E_AssetType.SO:
                return Resources.Load<T>("ScriptableObject/" + fileName);
            case E_AssetType.Instance:
                return Resources.Load<T>("Instance/" + fileName);
          
        }   
       return default;
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

        if(delaySeconds>0)
             _ = DelayRelease(handle, delaySeconds);
        return asset;
    }

    private static async Task DelayRelease<T>(AsyncOperationHandle<T> handle, float delaySeconds) where T : Object
    {
        await Task.Delay((int)(delaySeconds * 1000));
        Addressables.Release(handle);
    }
    
    internal static void wake(){}
}
}
