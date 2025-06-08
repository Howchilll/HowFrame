using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public static class LineManager
{
    static readonly Dictionary<string, Queue<LineRenderer>> pool = new Dictionary<string, Queue<LineRenderer>>();

    static readonly Dictionary<string, Queue<LineRenderer>> DontKillPool = new Dictionary<string, Queue<LineRenderer>>();

    private static readonly Dictionary<string, Queue<Coroutine>> coroutines = new Dictionary<string, Queue<Coroutine>>();

    private static GameObject _lineManagerObject;
    private static FakeMono _fakeMono;

    static LineManager()
    {
        Init();
    }

    private static void Init()
    {
        if (_lineManagerObject == null)
        {
            _lineManagerObject = new GameObject("LineManager");
            _fakeMono = _lineManagerObject.AddComponent<FakeMono>();
            Object.DontDestroyOnLoad(_lineManagerObject);
        }
    }

    #region 对象池

    private static void ApplyLineInfo(LineRenderer line, LineDataInfo info)
    {
        line.startColor = info.startColor;
        line.endColor = info.endColor;
        line.startWidth = info.startWidth;
        line.endWidth = info.endWidth;
        info.WhenInit(line);
    }

    private static LineRenderer CreateNewLineRenderer(LineDataInfo lineInfo)
    {
        GameObject LineObj = new GameObject("LineRenderer");
        LineObj.transform.SetParent(_lineManagerObject.transform);
        var line = LineObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        line.material = new Material(Shader.Find("Sprites/Default"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        ApplyLineInfo(line, lineInfo);
        return line;
    }

    private static LineRenderer GetLineRenderer(LineDataInfo lineInfo)
    {
        string name = lineInfo.name;
        if (!pool.ContainsKey(name))
            pool[name] = new Queue<LineRenderer>();

        if (pool[name].Count > 0)
            return pool[name].Dequeue();

        // 尝试从默认池中拿
        if (!pool.ContainsKey("")) pool[""] = new Queue<LineRenderer>();
        if (pool[""].Count > 0)
        {
            LineRenderer lineRenderer = pool[""].Dequeue();
            ApplyLineInfo(lineRenderer, lineInfo);
            return lineRenderer;
        }


        // 实在没了就新建一个
        return CreateNewLineRenderer(lineInfo);
    }

    #endregion

    #region ClearLine

    static IEnumerator ClearLine(string name, LineRenderer lineRenderer, float duration)
    {
        if (duration == 0)
            yield return null; // 等待一帧
        else
            yield return new WaitForSeconds(duration); // 等待指定时间

        lineRenderer.positionCount = 0;

        if (!pool.ContainsKey(name))
            pool[name] = new Queue<LineRenderer>();

        pool[name].Enqueue(lineRenderer);
    }

    #endregion

    #region KillLine

    static IEnumerator KillLine(Queue<LineRenderer> lineRenderers, float duration)
    {
        foreach (var line in lineRenderers)
        {
            line.positionCount = 0;
            yield return new WaitForSeconds(duration);
        }
    }

    public static void KillLines(string lineName, float duration = 0, System.Action<LineRenderer> onRecycle = null)
    {
        if (!DontKillPool.TryGetValue(lineName, out var from)) return;
        if (duration != 0) _fakeMono.StartCoroutine(KillLine(from, duration));
        else
            foreach (LineRenderer Lr in from)
            {
                Lr.positionCount = 0;
            }

        if (!pool.ContainsKey("")) pool[""] = new Queue<LineRenderer>();
        Queue<LineRenderer> to = pool[""];
        while (from.Count > 0)
        {
            LineRenderer line = from.Dequeue();
            onRecycle?.Invoke(line);
            to.Enqueue(line);
        }

        DontKillPool.Remove(lineName);
    }

    #endregion

    #region UpdateRope

    static IEnumerator UpdateRope(LineDataInfo lineInfo, LineRenderer lineRenderer, Transform[] points)
    {
        float elapsed = 0f;

        while (elapsed < lineInfo.time)
        {
            lineRenderer.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                lineRenderer.SetPosition(i, points[i].position);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.positionCount = 0;
        if (!pool.ContainsKey(lineInfo.name))
            pool[lineInfo.name] = new Queue<LineRenderer>();
        if (!pool[lineInfo.name].Contains(lineRenderer))
            pool[lineInfo.name].Enqueue(lineRenderer);
    }

    static IEnumerator UpdateRope(LineDataInfo lineInfo, LineRenderer lineRenderer, Transform startPoint, Vector3 endPoint)
    {
        float elapsed = 0f;

        while (elapsed < lineInfo.time)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint.position);
            lineRenderer.SetPosition(1, endPoint);
            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.positionCount = 0;
        if (!pool.ContainsKey(lineInfo.name))
            pool[lineInfo.name] = new Queue<LineRenderer>();
        if (!pool[lineInfo.name].Contains(lineRenderer))
            pool[lineInfo.name].Enqueue(lineRenderer);
    }

    static IEnumerator UpdateRope(LineDataInfo lineInfo, LineRenderer lineRenderer, Vector3 startPoint, Transform endPoint)
    {
        float elapsed = 0f;

        while (elapsed < lineInfo.time)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.positionCount = 0;
        if (!pool.ContainsKey(lineInfo.name))
            pool[lineInfo.name] = new Queue<LineRenderer>();
        if (!pool[lineInfo.name].Contains(lineRenderer))
            pool[lineInfo.name].Enqueue(lineRenderer);
    }

    #endregion

    #region KillRope

    static IEnumerator KillRope(Queue<LineRenderer> lineRenderers, Queue<Coroutine> coroutines, float duration)
    {
        for (int i = 0; i < lineRenderers.Count; i++)
        {
            _fakeMono.StopCoroutine(coroutines.Dequeue());
            lineRenderers.Dequeue().positionCount = 0;
            yield return new WaitForSeconds(duration);
        }
    }

    public static void KillRope(string lineName, float duration = 0, System.Action<LineRenderer> onRecycle = null)
    {
        if (!DontKillPool.TryGetValue(lineName, out var from)) return;
        if (!coroutines.TryGetValue(lineName, out var cos)) return;
        if (!pool.ContainsKey("")) pool[""] = new Queue<LineRenderer>();
        if (duration != 0)
        {
            _fakeMono.StartCoroutine(KillRope(from, cos, duration));
        }
        else
            while (cos.Count > 0 && from.Count > 0)
            {
                _fakeMono.StopCoroutine(cos.Dequeue());
                LineRenderer lr = from.Dequeue();
                lr.positionCount = 0;
                onRecycle?.Invoke(lr);
                pool[""].Enqueue(lr);
            }

        DontKillPool.Remove(lineName);
        coroutines.Remove(lineName);
    }

    #endregion

    #region KeepRope

    static IEnumerator KeepRope(LineDataInfo lineInfo, LineRenderer lineRenderer, Transform[] points)
    {
        while (true)
        {
            if (points == null || points.Length < 2)
            {
                lineRenderer.positionCount = 0;
            }
            else
            {
                lineRenderer.positionCount = points.Length;
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i] != null)
                        lineRenderer.SetPosition(i, points[i].position);
                    else
                        lineRenderer.SetPosition(i, Vector3.zero); // 或者其他占位策略
                }
            }

            yield return null;
        }
    }

    static IEnumerator KeepRope(LineDataInfo lineInfo, LineRenderer lineRenderer, Vector3 startPoint,
        Transform endPoint)
    {
        while (true)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint.position);
            yield return null;
        }
    }

    static IEnumerator KeepRope(LineDataInfo lineInfo, LineRenderer lineRenderer, Transform startPoint,
        Vector3 endPoint)
    {
        while (true)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint.position);
            lineRenderer.SetPosition(1, endPoint);
            yield return null;
        }
    }

    #endregion

    #region DrawLine

    public static void DrawLine(System.Action<LineRenderer> thisDraw, LineDataInfo lineInfo, params Vector3[] points)
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        LineRenderer lineRenderer = GetLineRenderer(lineInfo);

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineInfo.WhenDraw(lineRenderer);
        thisDraw.Invoke(lineRenderer);

        if (lineInfo.time != 0) _fakeMono.StartCoroutine(ClearLine(lineInfo.name, lineRenderer, lineInfo.time));
        else
        {
            if (!DontKillPool.ContainsKey(lineInfo.name)) DontKillPool[lineInfo.name] = new Queue<LineRenderer>();
            DontKillPool[lineInfo.name].Enqueue(lineRenderer);
        }
    }

    public static void DrawLine(LineDataInfo lineInfo, params Vector3[] points)
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        LineRenderer lineRenderer = GetLineRenderer(lineInfo);

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineInfo.WhenDraw(lineRenderer);


        if (lineInfo.time >= 0) _fakeMono.StartCoroutine(ClearLine(lineInfo.name, lineRenderer, lineInfo.time));
        else
        {
            if (!DontKillPool.ContainsKey(lineInfo.name)) DontKillPool[lineInfo.name] = new Queue<LineRenderer>();
            DontKillPool[lineInfo.name].Enqueue(lineRenderer);
        }
    }

    #endregion

    #region DrawRope

    public static void DrawRope(System.Action<LineRenderer> thisDraw, LineDataInfo lineInfo, params Transform[] points)
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        LineRenderer lineRenderer = GetLineRenderer(lineInfo);
        lineInfo.WhenDraw(lineRenderer);
        thisDraw.Invoke(lineRenderer);
        if (lineInfo.time >= 0) _fakeMono.StartCoroutine(UpdateRope(lineInfo, lineRenderer, points));
        else
        {
            if (!coroutines.ContainsKey(lineInfo.name)) coroutines[lineInfo.name] = new Queue<Coroutine>();
            Coroutine co = _fakeMono.StartCoroutine(KeepRope(lineInfo, lineRenderer, points));
            coroutines[lineInfo.name].Enqueue(co);
            if (!DontKillPool.ContainsKey(lineInfo.name)) DontKillPool[lineInfo.name] = new Queue<LineRenderer>();
            DontKillPool[lineInfo.name].Enqueue(lineRenderer);
        }
    }

    public static void DrawRope(LineDataInfo lineInfo, params Transform[] points)
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        LineRenderer lineRenderer = GetLineRenderer(lineInfo);
        lineInfo.WhenDraw(lineRenderer);
        if (lineInfo.time >= 0) _fakeMono.StartCoroutine(UpdateRope(lineInfo, lineRenderer, points));
        else
        {
            if (!coroutines.ContainsKey(lineInfo.name)) coroutines[lineInfo.name] = new Queue<Coroutine>();
            Coroutine co = _fakeMono.StartCoroutine(KeepRope(lineInfo, lineRenderer, points));
            coroutines[lineInfo.name].Enqueue(co);
            if (!DontKillPool.ContainsKey(lineInfo.name)) DontKillPool[lineInfo.name] = new Queue<LineRenderer>();
            DontKillPool[lineInfo.name].Enqueue(lineRenderer);
        }
    }

    public static void DrawRope(LineDataInfo lineInfo, Vector3 startPoint, Transform endPoint,
        System.Action<LineRenderer> thisDraw = null)
    {
        LineRenderer lineRenderer = GetLineRenderer(lineInfo);
        lineInfo.WhenDraw(lineRenderer);
        if (lineInfo.time >= 0) _fakeMono.StartCoroutine(UpdateRope(lineInfo, lineRenderer, startPoint, endPoint));
        else
        {
            if (!coroutines.ContainsKey(lineInfo.name)) coroutines[lineInfo.name] = new Queue<Coroutine>();
            Coroutine co = _fakeMono.StartCoroutine(KeepRope(lineInfo, lineRenderer, startPoint, endPoint));
            coroutines[lineInfo.name].Enqueue(co);
            if (!DontKillPool.ContainsKey(lineInfo.name)) DontKillPool[lineInfo.name] = new Queue<LineRenderer>();
            DontKillPool[lineInfo.name].Enqueue(lineRenderer);
        }
    }

    public static void DrawRope(LineDataInfo lineInfo, Transform startPoint, Vector3 endPoint,
        System.Action<LineRenderer> thisDraw = null)
    {
        LineRenderer lineRenderer = GetLineRenderer(lineInfo);
        lineInfo.WhenDraw(lineRenderer);
        if (lineInfo.time >= 0) _fakeMono.StartCoroutine(UpdateRope(lineInfo, lineRenderer, startPoint, endPoint));
        else
        {
            if (!coroutines.ContainsKey(lineInfo.name)) coroutines[lineInfo.name] = new Queue<Coroutine>();
            Coroutine co = _fakeMono.StartCoroutine(KeepRope(lineInfo, lineRenderer, startPoint, endPoint));
            coroutines[lineInfo.name].Enqueue(co);
            if (!DontKillPool.ContainsKey(lineInfo.name)) DontKillPool[lineInfo.name] = new Queue<LineRenderer>();
            DontKillPool[lineInfo.name].Enqueue(lineRenderer);
        }
    }

    #endregion

    #region Clean

    public static void PoolClean(string lineName, System.Action<LineRenderer> onRecycle = null)
    {
        if (!pool.ContainsKey(lineName)) return;
        if (!pool.ContainsKey("")) pool[""] = new Queue<LineRenderer>();
        Queue<LineRenderer> from = pool[lineName];
        Queue<LineRenderer> to = pool[""];
        while (from.Count > 0)
        {
            LineRenderer line = from.Dequeue();
            onRecycle?.Invoke(line);
            to.Enqueue(line);
        }

        pool.Remove(lineName);
    }


    public static void DestroyAll()
    {
        foreach (var kv in pool)
        {
            var queue = kv.Value;
            while (queue.Count > 0)
            {
                var lr = queue.Dequeue();
                if (lr.material != null) Object.Destroy(lr.material);
                if (lr != null) Object.Destroy(lr.gameObject); // 或者 lr 视情况而定
            }
        }

        foreach (var co in coroutines)
        {
            var queue = co.Value;
            while (queue.Count > 0)
            {
                _fakeMono.StopCoroutine(queue.Dequeue());
            }
        }

        foreach (var kv in DontKillPool)
        {
            var queue = kv.Value;
            while (queue.Count > 0)
            {
                var lr = queue.Dequeue();
                if (lr.material != null) Object.Destroy(lr.material);
                if (lr != null) Object.Destroy(lr.gameObject);
            }
        }

        pool.Clear();
        DontKillPool.Clear();
    }

    #endregion

    private class FakeMono : MonoBehaviour
    {
    }

    public static void wake()
    {
    }
}