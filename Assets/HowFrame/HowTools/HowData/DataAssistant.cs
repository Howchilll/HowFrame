using System;
using System.IO;
using System.Threading.Tasks;
using LitJson;
using MessagePack;
using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 数据持久化：
    /// - WriteData / ReadData：persistentDataPath 下的存档（MessagePack + XOR，或 JSON）
    /// - LoadConfig / LoadConfigAsync：StreamingAssets 下的只读配置（JSON）
    /// </summary>
    public static class DataAssistant
    {
        // 读写必须使用同一套序列化选项：Contractless 允许普通类（无 [MessagePackObject] 标注）直接序列化
        private static readonly MessagePackSerializerOptions PackOptions =
            MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

        private static string GetPath(string fileName, bool isJson, string upperPath)
        {
            string directoryPath = string.IsNullOrEmpty(upperPath)
                ? Application.persistentDataPath
                : Path.Combine(Application.persistentDataPath, upperPath);
            return Path.Combine(directoryPath, fileName + (isJson ? ".json" : ".dat"));
        }

        /// <summary>
        /// 写入存档。先写临时文件再替换，写入途中崩溃 / 断电不会损坏原存档。
        /// </summary>
        public static void WriteData(object data, string fileName, bool isJson = false, string upperPath = "")
        {
            string fullPath = GetPath(fileName, isJson, upperPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            byte[] bytes;
            if (isJson)
            {
                bytes = System.Text.Encoding.UTF8.GetBytes(JsonMapper.ToJson(data));
            }
            else
            {
                bytes = MessagePackSerializer.Serialize(data?.GetType() ?? typeof(object), data, PackOptions);
                Encryption.XOR(bytes);
            }

            string tempPath = fullPath + ".tmp";
            File.WriteAllBytes(tempPath, bytes);
            if (!File.Exists(fullPath))
            {
                File.Move(tempPath, fullPath);
                return;
            }
            try
            {
                File.Replace(tempPath, fullPath, null);
            }
            catch (PlatformNotSupportedException)
            {
                // 个别平台不支持 File.Replace，退化为 删除 + 移动
                File.Delete(fullPath);
                File.Move(tempPath, fullPath);
            }
        }

        /// <summary>读取存档；文件不存在或损坏时返回 default 并记录错误</summary>
        public static T ReadData<T>(string fileName, bool isJson = false, string upperPath = "")
        {
            string fullPath = GetPath(fileName, isJson, upperPath);
            if (!File.Exists(fullPath)) return default;

            try
            {
                if (isJson)
                    return JsonMapper.ToObject<T>(File.ReadAllText(fullPath));

                byte[] binary = File.ReadAllBytes(fullPath);
                Encryption.XOR(binary);
                return MessagePackSerializer.Deserialize<T>(binary, PackOptions);
            }
            catch (Exception e)
            {
                Debug.LogError($"DataAssistant: 读取存档失败 {fullPath}\n{e}");
                return default;
            }
        }

        public static bool HasData(string fileName, bool isJson = false, string upperPath = "")
        {
            return File.Exists(GetPath(fileName, isJson, upperPath));
        }

        /// <summary>删除存档，返回是否删除了文件</summary>
        public static bool DeleteData(string fileName, bool isJson = false, string upperPath = "")
        {
            string fullPath = GetPath(fileName, isJson, upperPath);
            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }

        /// <summary>
        /// 同步读取 StreamingAssets 下的 JSON 配置（fileName 不带扩展名）。
        /// 注意：Android / WebGL 上 StreamingAssets 不能直接按文件读取，请用 LoadConfigAsync。
        /// </summary>
        public static T LoadConfig<T>(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName) + ".json";
            if (!File.Exists(path)) return default;
            return JsonMapper.ToObject<T>(File.ReadAllText(path));
        }

        /// <summary>异步读取 StreamingAssets 下的 JSON 配置（全平台可用，fileName 不带扩展名）</summary>
        public static async Task<T> LoadConfigAsync<T>(string fileName)
        {
            var text = await AssetAssistant.ImportAsset<TextAsset>(fileName + ".json");
            if (text == null || string.IsNullOrEmpty(text.text)) return default;
            try
            {
                return JsonMapper.ToObject<T>(text.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"DataAssistant: 配置解析失败 {fileName}.json\n{e}");
                return default;
            }
        }

        internal static void Wake() { }
    }
}
