using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRuntimeGet
{
}


public static class TypeAssistant
{
    private static readonly Dictionary<string, Type> TypeDic;

    static TypeAssistant()
    {
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
    }

    public static void wake(){}
    
    public static object GetInstance(string typeName)
    {
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