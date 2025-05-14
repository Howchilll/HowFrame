using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;
using MessagePack;

public static class DataAssitant 
{
    
    public static void WriteData(object data, string fileName,bool isJson=false, string upperPath ="")
    {
        string directoryPath = string.IsNullOrEmpty(upperPath) 
            ? Application.persistentDataPath 
            : Path.Combine(Application.persistentDataPath, upperPath);

        Directory.CreateDirectory(directoryPath);
        string fullPath = Path.Combine(directoryPath, fileName + (isJson ? ".json" : ".dat"));

        if (isJson)
        {
            string jsonStr = JsonMapper.ToJson(data);
            File.WriteAllText(fullPath, jsonStr);
        }
        else
        {
            byte[] binary = MessagePackSerializer.Serialize(data);
            Encryption.XOR(binary);
            File.WriteAllBytes(fullPath, binary);
        }
    }

    public static T ReadData<T>(string fileName, bool isJson = false, string upperPath = "")
    {
        string directoryPath = string.IsNullOrEmpty(upperPath) 
            ? Application.persistentDataPath 
            : Path.Combine(Application.persistentDataPath, upperPath);
        string fullPath = Path.Combine(directoryPath, fileName + (isJson ? ".json" : ".dat"));
        if (File.Exists(fullPath))
        {
            if (isJson)
            {
                T data = JsonMapper.ToObject<T>(File.ReadAllText(fullPath));
                return data;
            }
            else
            {
                byte[] binary = File.ReadAllBytes(fullPath);
                Encryption.XOR(binary);
                return MessagePackSerializer.Deserialize<T>(binary);
            }
        }
        return default(T);
    }

    public static T LoadConfig<T>(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName)+".json";
        if (File.Exists(path))
        {
            T data = JsonMapper.ToObject<T>(File.ReadAllText(path));
            return data;
        }
        return default(T);
    }
    
    internal static void wake(){}
}

//WriteData 将对象写入本地化json/二进制
//ReadData 将本地化json/二进制解析为对象
//LoadConfig 将streamingAssets文件夹json转为对象