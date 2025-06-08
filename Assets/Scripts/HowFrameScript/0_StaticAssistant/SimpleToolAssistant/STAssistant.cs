using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class STAssistant //simple tool Assistant
{
    private static readonly Random _rand = new Random();

    private static LayerMask allLayers = ~0;
    public static ( LayerMask layers, string[] tags) GetLayersAndTags(string[] layerAndTag)
    {
        if (layerAndTag == null || layerAndTag.Length == 0) return (allLayers, Array.Empty<string>());
        int splitIndex = Array.IndexOf(layerAndTag, "tags");
        if (splitIndex == -1)
        {
            return (LayerMask.GetMask(layerAndTag), Array.Empty<string>());
        }

        if (splitIndex == layerAndTag.Length - 1)
        {
            return (LayerMask.GetMask(layerAndTag.Take(layerAndTag.Length - 1).ToArray()), Array.Empty<string>());
        }
        
        var layers = layerAndTag.Take(splitIndex).ToArray();
        var tags = layerAndTag.Skip(splitIndex + 1).ToArray();
        if (layers.Length == 0){ return (allLayers, tags);}
        
        return (LayerMask.GetMask(layers), tags);
    }

    public static bool HasAnyTag(this GameObject obj, params string[] tags)
    {
        return tags.Contains(obj.tag);
    }
    
    public static bool HasAnyLayer(this GameObject obj, params string[] layers)
    {
        int objLayer = obj.layer;
        foreach (var layerName in layers)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex == -1) continue; // 忽略非法 layer
            if (layerIndex == objLayer) return true;
        }
        return false;
    }
    
    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]); // C# 7 的元组交换语法
        }
    }
    
    public static T RandomItem<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Cannot get random item from an empty or null list.");
    
        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }
    
    public static void ForEach<T>(IEnumerable<T> collection, Action<T> action)
    {
        if (collection == null || action == null) return;

        foreach (var item in collection)
        {
            action(item);
        }
    }
    
    public static int RandomInt(int min, int max)
    {
        return _rand.Next(min, max);
    }
    
    public static double RandomDouble()
    {
        return _rand.NextDouble();
    }
    
    public static bool RandomBool()
    {
        return _rand.Next(2) == 0;
    }
}
