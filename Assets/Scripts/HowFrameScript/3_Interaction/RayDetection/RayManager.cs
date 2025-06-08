using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class RayManager 
{
    private static Camera Camera=>Camera.main;
    public static LayerMask DefaultMask = (LayerMask)(-1);

    public static void Raycast(System.Action<RaycastHit> onHit, Vector3 origin, Vector3 direction, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, DefaultMask, triggerInteraction))
        {
            onHit?.Invoke(hit);
        }
    }

    public static void RaycastAll(System.Action<RaycastHit[]> onHit, Vector3 origin, Vector3 direction, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Ray ray = new Ray(origin, direction);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, DefaultMask, triggerInteraction);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        if (hits.Length > 0) onHit?.Invoke(hits);
    }
    
    public static RaycastHit? Raycast(Vector3 origin, Vector3 direction, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, DefaultMask, triggerInteraction))
            return hit;
        return null;
    }
    
    public static RaycastHit[] RaycastAllHits(Vector3 origin, Vector3 direction, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Ray ray = new Ray(origin, direction);
        var hits = Physics.RaycastAll(ray, maxDistance, DefaultMask, triggerInteraction);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }
    
    public static RaycastHit? RaycastWithTag(Vector3 origin, Vector3 direction, float maxDistance, string tag)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, DefaultMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag(tag))
                return hit;
        }
        return null;
    }
    
    public static List<RaycastHit> RaycastAllWithTag(Vector3 origin, Vector3 direction, float maxDistance, string tag)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, DefaultMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        var results = new List<RaycastHit>();
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag(tag))
                results.Add(hit);
        }
        return results;
    }
    public static Vector3 GetPoint(Vector3 start, Vector3 end, float distance)
    {
        Vector3 direction = (end - start).normalized; // 计算方向
        Ray ray = new Ray(start, direction);          // 构造射线

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, distance))
        {
            return hit.point; // 命中，返回碰撞点
        }

        return ray.GetPoint(distance); // 未命中，返回延伸点
    }

    
    public static Vector3 GetPoint(Vector3 screenPoint, float distance)
    {
        Ray ray = Camera.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            return hit.point;
        }
        return ray.GetPoint(distance);
    }
    
    
    

    public static void Raycast(System.Action<RaycastHit> onHit, Vector3 screenPoint, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        if (Camera == null) return ;

        Ray ray = Camera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, DefaultMask, triggerInteraction))
        {
            onHit?.Invoke(hit);
        }
    }

    public static void RaycastAll(System.Action<RaycastHit[]> onHit, Vector3 screenPoint, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        if (Camera == null) return ;

        Ray ray = Camera.ScreenPointToRay(screenPoint);
        var hits = Physics.RaycastAll(ray, maxDistance, DefaultMask, triggerInteraction);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        if (hits.Length > 0)
            onHit?.Invoke(hits);
    }

    public static RaycastHit? Raycast(Vector3 screenPoint, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        if (Camera == null) { return null;}

        Ray ray = Camera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, DefaultMask, triggerInteraction))
            return hit; 
        return null;
        
      
    }

    public static RaycastHit[] RaycastAll(Vector3 screenPoint, float maxDistance, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        if (Camera==null ) return Array.Empty<RaycastHit>();

        Ray ray = Camera.ScreenPointToRay(screenPoint);
        var hits = Physics.RaycastAll(ray, maxDistance, DefaultMask, triggerInteraction);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }

    public static RaycastHit? RaycastWithTag(Vector3 screenPoint, float maxDistance, string tag)
    {
        if (Camera==null) return null;

        Ray ray = Camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, DefaultMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag(tag))
                return hit;
        }
        return null;
    }

    public static List<RaycastHit> RaycastAllWithTag(Vector3 screenPoint, float maxDistance, string tag)
    {
        if (Camera==null) return new List<RaycastHit>();

        Ray ray = Camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, DefaultMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var results = new List<RaycastHit>();
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag(tag))
                results.Add(hit);
        }
        return results;
    }
}
    
    
    

