
    using HowFrame;
    using UnityEngine;
    using System.Collections.Generic;
    using JObject = Newtonsoft.Json.Linq.JObject;

    public static class JObjAssistant
    {
        private static readonly Dictionary<string, JObject> _cache = new();

        public static JObject Get(string name)
        {
            if (_cache.TryGetValue(name, out var cached))
                return cached;

            var asset = AssetAssistant.AddressableGet<TextAsset>(name);

            if (asset == null)
            {
                Debug.LogError($"[JObjAssistant] Config not found: {name}");
                return new JObject();
            }

            var jObj = JObject.Parse(asset.text);
            _cache[name] = jObj;

            return (JObject)jObj.DeepClone();
        }
    }
