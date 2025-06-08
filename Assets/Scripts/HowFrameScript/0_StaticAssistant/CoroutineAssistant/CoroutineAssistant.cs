using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

public static class CoroutineAssistant
{
    private static readonly Dictionary<string, Coroutine> _coroutines = new();
    private static readonly FakeMono Runner;
    
    static CoroutineAssistant()
    {
        var go = new GameObject("[CoroutineAssistant]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        Runner = go.AddComponent<FakeMono>();
    }

    public static void StartLoop(string name, float interval, Action onTick, Action onStart = null)
    {
        if (_coroutines.TryGetValue(name, out Coroutine existing))
        {
            Runner.StopCoroutine(existing);
        }

        Coroutine coroutine = Runner.StartCoroutine(RunLoop(interval, onTick, onStart));
        _coroutines[name] = coroutine;
    }
    public static void StartLoop(string name, int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
    {
        if (_coroutines.TryGetValue(name, out Coroutine existing))
        {
            Runner.StopCoroutine(existing);
        }

        Coroutine coroutine =
            Runner.StartCoroutine(LoopCoroutine(name, loopCount, interval, onLoop, onStart, onComplete));
        _coroutines[name] = coroutine;
    }
    public static void StartLoopUnscaled(string name, float interval, Action onTick, Action onStart = null, Action onComplete = null)
    {
        if (_coroutines.TryGetValue(name, out Coroutine existing))
        {
            Runner.StopCoroutine(existing);
        }

        Coroutine coroutine = Runner.StartCoroutine(RunLoopUnscaled(interval, onTick, onStart, onComplete));
        _coroutines[name] = coroutine;
    }
    public static void StartLoopUnscaled(string name, int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
    {
        if (_coroutines.TryGetValue(name, out Coroutine existing))
        {
            Runner.StopCoroutine(existing);
        }

        Coroutine coroutine = Runner.StartCoroutine(LoopCoroutineUnscaled(name, loopCount, interval, onLoop, onStart, onComplete));
        _coroutines[name] = coroutine;
    }
    public static void Stop(string name)
    {
        if (_coroutines.TryGetValue(name, out Coroutine coroutine))
        {
            if (Runner != null)
            {
                Runner.StopCoroutine(coroutine);
            }
            _coroutines.Remove(name);
        }

    }
    public static void StartIterate<T>(string name, IEnumerable<T> collection, float interval, Action<T> onItem, Action onStart = null, Action onComplete = null)
    {
        if (_coroutines.TryGetValue(name, out var existing))
        {
            Runner.StopCoroutine(existing);
        }

        var coroutine = Runner.StartCoroutine(IterateCoroutine(name, collection, interval, onItem, onStart, onComplete));
        _coroutines[name] = coroutine;
    }
    public static void DelayInvoke(string name, float delay, Action onComplete)
    {
        if (_coroutines.TryGetValue(name, out Coroutine existing))
        {
            Runner.StopCoroutine(existing);
            _coroutines.Remove(name);
        }
        if (delay <= 0f)
            delay = Time.deltaTime; // 延迟一帧

        Coroutine coroutine = Runner.StartCoroutine(DelayCoroutine(name, delay, onComplete));
        _coroutines[name] = coroutine;
    }
    public static void DelayInvokeRealtime(string name, float delay, Action onComplete)
    {
        if (_coroutines.TryGetValue(name, out Coroutine existing))
        {
            Runner.StopCoroutine(existing);
            _coroutines.Remove(name);
        }

        if (delay <= 0f)
            delay = Time.unscaledDeltaTime; // 延迟一帧，使用真实时间间隔

        Coroutine coroutine = Runner.StartCoroutine(DelayCoroutineRealtime(name, delay, onComplete));
        _coroutines[name] = coroutine;
    }

    
    private static IEnumerator DelayCoroutineRealtime(string name, float delay, Action onComplete)
    {
        yield return new WaitForSecondsRealtime(delay);
        onComplete?.Invoke();
        _coroutines.Remove(name);
    }
    private static IEnumerator DelayCoroutine(string name, float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
        _coroutines.Remove(name);
    }
    private static IEnumerator IterateCoroutine<T>(string name, IEnumerable<T> collection, float interval, Action<T> onItem, Action onStart, Action onComplete)
    {
        onStart?.Invoke();

        foreach (var item in collection)
        {
            onItem?.Invoke(item);
            yield return new WaitForSeconds(interval);
        }

        onComplete?.Invoke();
        _coroutines.Remove(name);
    }
    private static IEnumerator LoopCoroutine(string name, int loopCount, float interval, Action onLoop, Action onStart, Action onComplete)
    {
        onStart?.Invoke();

        for (int i = 0; i < loopCount; i++)
        {
            onLoop?.Invoke();
            yield return new WaitForSeconds(interval);
        }

        onComplete?.Invoke();
        _coroutines.Remove(name);
    }
    private static IEnumerator RunLoop(float interval, Action onTick, Action onStart)
    {
        onStart?.Invoke();
        while (true)
        {
            onTick?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }
    private static IEnumerator RunLoopUnscaled(float interval, Action onTick, Action onStart, Action onComplete)
    {
        onStart?.Invoke();
        var wait = new WaitForSecondsRealtime(interval);
        while (true)
        {
            onTick?.Invoke();
            yield return wait;
        }
    }
    private static IEnumerator LoopCoroutineUnscaled(string name, int loopCount, float interval, Action onLoop, Action onStart, Action onComplete)
    {
        onStart?.Invoke();
        var wait = new WaitForSecondsRealtime(interval);

        for (int i = 0; i < loopCount; i++)
        {
            onLoop?.Invoke();
            yield return wait;
        }

        onComplete?.Invoke();
        _coroutines.Remove(name);
    }
    private class FakeMono : MonoBehaviour
    {
    }
}