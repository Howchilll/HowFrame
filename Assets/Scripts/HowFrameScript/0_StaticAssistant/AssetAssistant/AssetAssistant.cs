using System;using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;using Object = UnityEngine.Object;

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
    public static async Task<T> ImportAsset<T>(string fileName)
    {
        if (typeof(T) == typeof(AudioClip))
        {
            string path = Path.Combine(Application.streamingAssetsPath,"Music/"+fileName);
            string audioPath = null;
            AudioType audioType = AudioType.UNKNOWN;

            if (File.Exists(path + ".wav"))
            {
                audioPath = path + ".wav";
                audioType = AudioType.WAV;
            }
            else if (File.Exists(path + ".mp3"))
            {
                audioPath = path + ".mp3";
                audioType = AudioType.MPEG;
            }
            if (string.IsNullOrEmpty(audioPath))
            {
                Debug.LogError("Audio file not found at: " + path);
                return default;
            }
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioPath, audioType))
            {
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load audio clip: {request.error}");
                    return default;
                }
                return (T)(object)DownloadHandlerAudioClip.GetContent(request);
            }
        }
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
    
    internal static void wake(){}
}
