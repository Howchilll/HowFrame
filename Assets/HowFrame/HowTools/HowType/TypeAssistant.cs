using System;
using System.Collections.Generic;
using UnityEngine;
namespace HowFrame
{

public interface IRuntimeGet
{
}


public static class TypeAssistant
{
    private static Dictionary<string, Type> TypeDic;
    private static bool _initialized = false;

    public static void Wake()
    {
        if (_initialized) return; // 防止重复初始化

        TypeDic = new Dictionary<string, Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            types = assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.IsAbstract) continue;

                if (typeof(IRuntimeGet).IsAssignableFrom(type))
                {
                    TypeDic[type.Name] = type;
                }
            }
        }
        _initialized = true;
    }
    
    public static object GetInstance(string typeName)
    {
        if (TypeDic == null)
        {
            Debug.LogError("TypeAssistant: 未初始化，请先调用 Wake()");
            return null;
        }

        if (TypeDic.TryGetValue(typeName, out var type))
        {
            return Activator.CreateInstance(type);
        }
        else
        {
            "Not found".Log();
            return null;
        }
    }
}
}