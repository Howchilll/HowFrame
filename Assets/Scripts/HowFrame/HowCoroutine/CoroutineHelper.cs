using System;
using System.Collections;
using UnityEngine;
namespace HowFrame
{

public class CoroutineHelper
{
    private Coroutine _coroutine;
    private readonly MonoBehaviour _runner;

    public CoroutineHelper(MonoBehaviour runner)
    {
        _runner = runner;
    }

    public bool IsRunning => _coroutine != null;

    // ---------- Start a repeating loop ----------
    public void StartLoop(float interval, Action onTick, Action onStart = null)
    {
        Stop();
        _coroutine = _runner.StartCoroutine(RunLoop(interval, onTick, onStart));
    }

    // ---------- Start a delayed invoke ----------
    public void DelayInvoke(float delay, Action onComplete)
    {
        Stop();
        _coroutine = _runner.StartCoroutine(DelayCoroutine(delay, onComplete));
    }

    // ---------- Start a finite loop ----------
    public void StartLoop(int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
    {
        Stop();
        _coroutine = _runner.StartCoroutine(LoopCoroutine(loopCount, interval, onLoop, onStart, onComplete));
    }

    // ---------- Stop the coroutine ----------
    public void Stop()
    {
        if (_coroutine != null)
        {
            _runner.StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    // ---------- Private coroutines ----------
    private IEnumerator RunLoop(float interval, Action onTick, Action onStart)
    {
        onStart?.Invoke();
        while (true)
        {
            onTick?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator LoopCoroutine(int loopCount, float interval, Action onLoop, Action onStart, Action onComplete)
    {
        onStart?.Invoke();
        for (int i = 0; i < loopCount; i++)
        {
            onLoop?.Invoke();
            yield return new WaitForSeconds(interval);
        }
        onComplete?.Invoke();
        _coroutine = null;
    }

    private IEnumerator DelayCoroutine(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(Mathf.Max(delay, Time.deltaTime));
        onComplete?.Invoke();
        _coroutine = null;
    }

 
}
}
