using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace HowFrame
{



public static class TypeAssistant
    {
        private static readonly Dictionary<string, Type> TypeDic = new();
        private static bool _initialized;

        public static void Wake()
        {
            if (_initialized) return;

            TypeDic.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (!type.IsClass || type.IsAbstract) continue;

                    var attr = type.GetCustomAttributes(typeof(RuntimeGetAttribute), false);
                    if (attr.Length == 0) continue;

                    var runtimeAttr = (RuntimeGetAttribute)attr[0];
                    var key = string.IsNullOrEmpty(runtimeAttr.Key)
                        ? type.Name
                        : runtimeAttr.Key;

                    if (TypeDic.ContainsKey(key))
                    {
                        Debug.LogWarning($"TypeAssistant: 重复 Key = {key}，类型 = {type.FullName}");
                        continue;
                    }

                    TypeDic.Add(key, type);
                }
            }

            _initialized = true;
        }

        public static object GetInstance(string key)
        {
            if (!_initialized)
            {
                Debug.LogError("TypeAssistant: 未初始化，请先调用 Wake()");
                return null;
            }

            if (!TypeDic.TryGetValue(key, out var type))
            {
                Debug.LogError($"TypeAssistant: 未找到类型 Key = {key}");
                return null;
            }

            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                Debug.LogError($"TypeAssistant: 不能直接实例化 MonoBehaviour，请使用 GetType() 获取类型后使用 AddComponent()");
                return null;
            }

            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Debug.LogError($"TypeAssistant: 创建实例失败 {type.FullName}\n{e}");
                return null;
            }
        }

        public static Type GetType(string key)
        {
            if (!_initialized)
            {
                Debug.LogError("TypeAssistant: 未初始化，请先调用 Wake()");
                return null;
            }

            if (TypeDic.TryGetValue(key, out var type))
            {
                return type;
            }
            Debug.LogError($"TypeAssistant: 未找到类型 Key = {key}");
            return null;
        }

        public static T GetInstance<T>(string key) where T : class
        {
            return GetInstance(key) as T;
        }
    }
}
