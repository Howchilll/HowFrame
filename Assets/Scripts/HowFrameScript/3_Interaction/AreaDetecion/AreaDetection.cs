using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AreaManager
{
    public static List<GameObject> SphereDetect(Vector3 center, float radius, params string[] layersAndTags)
    {
        var (mask, tags) = STAssistant.GetLayersAndTags(layersAndTags);
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, mask);

        if (tags.Length == 0)
        {
            return hitColliders.Select(c => c.gameObject).ToList();
        }
        else
        {
            return FilterByTags(hitColliders, tags);
        }
    }

    public static List<GameObject> SphereDetect(Action<GameObject> onDetect, Vector3 center, float radius,
        params string[] layersAndTags)
    {
        var (mask, tags) = STAssistant.GetLayersAndTags(layersAndTags);
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, mask);

        List<GameObject> result;
        if (tags.Length == 0)
        {
            result = hitColliders.Select(c => c.gameObject).ToList();
        }
        else
        {
            result = FilterByTags(hitColliders, tags);
        }

        foreach (var go in result)
        {
            onDetect?.Invoke(go);
        }

        return result;
    }

    private static List<GameObject> FilterByTags(Collider[] colliders, string[] tags)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (var collider in colliders)
        {
            if (tags.Contains(collider.tag))
            {
                result.Add(collider.gameObject);
                break;
            }
        }

        return result;
    }

    public static List<GameObject> CubeDetect(Transform father, Vector3 localCenter, Vector3 size,
        params string[] layersAndTags)
    {
        var (mask, tags) = STAssistant.GetLayersAndTags(layersAndTags);

        // 把局部坐标转为世界坐标
        Vector3 worldCenter = father.TransformPoint(localCenter);
        Quaternion rotation = father.rotation;

        // OverlapBox 中 size 是半边长 ×2，所以我们直接传 size 就行
        Collider[] hitColliders = Physics.OverlapBox(worldCenter, size * 0.5f, rotation, mask);

        // 没有 tag 筛选，直接返回全部
        if (tags.Length == 0)
            return hitColliders.Select(c => c.gameObject).ToList();
        // 否则，进行 tag 筛选
        List<GameObject> result = FilterByTags(hitColliders, tags);
        return result;
    }

    public static List<GameObject> CubeDetect(System.Action<GameObject> onDetect, Transform father, Vector3 localCenter,
        Vector3 size, params string[] layersAndTags)
    {
        var (mask, tags) = STAssistant.GetLayersAndTags(layersAndTags);

        // 把局部坐标转为世界坐标
        Vector3 worldCenter = father.TransformPoint(localCenter);
        Quaternion rotation = father.rotation;

        // OverlapBox 中 size 是半边长 ×2，所以我们直接传 size 就行
        Collider[] hitColliders = Physics.OverlapBox(worldCenter, size * 0.5f, rotation, mask);

        // 没有 tag 筛选，直接返回全部
        if (tags.Length == 0)
            return hitColliders.Select(c => c.gameObject).ToList();
        // 否则，进行 tag 筛选
        List<GameObject> result = FilterByTags(hitColliders, tags);
        foreach (var go in result)
        {
            onDetect?.Invoke(go);
        }

        return result;
    }
}