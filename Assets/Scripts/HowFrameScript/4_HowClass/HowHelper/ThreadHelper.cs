using System;
using System.Threading;

public class ThreadHelper
{
    private Thread _thread;
    private CancellationTokenSource _cts;

    public bool IsRunning => _thread != null && _thread.IsAlive;

    // ---------- Start a repeating loop ----------
    public void StartLoop(int intervalMillis, Action onTick, Action onStart = null)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _thread = new Thread(() => RunLoop(intervalMillis, onTick, onStart, _cts.Token));
        _thread.IsBackground = true;
        _thread.Start();
    }

    // ---------- Start a delayed invoke ----------
    public void DelayInvoke(int delayMillis, Action onComplete)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _thread = new Thread(() => DelayAction(delayMillis, onComplete, _cts.Token));
        _thread.IsBackground = true;
        _thread.Start();
    }

    // ---------- Start a finite loop ----------
    public void StartLoop(int loopCount, int intervalMillis, Action onLoop, Action onStart = null, Action onComplete = null)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _thread = new Thread(() => LoopAction(loopCount, intervalMillis, onLoop, onStart, onComplete, _cts.Token));
        _thread.IsBackground = true;
        _thread.Start();
    }

    // ---------- Stop the thread ----------
    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        if (_thread != null && _thread.IsAlive)
        {
            _thread.Join();
        }
        _thread = null;
    }

    // ---------- Private methods ----------
    private void RunLoop(int intervalMillis, Action onTick, Action onStart, CancellationToken token)
    {
        onStart?.Invoke();
        while (!token.IsCancellationRequested)
        {
            onTick?.Invoke();
            if (token.WaitHandle.WaitOne(intervalMillis)) break;
        }
    }

    private void LoopAction(int loopCount, int intervalMillis, Action onLoop, Action onStart, Action onComplete, CancellationToken token)
    {
        onStart?.Invoke();
        for (int i = 0; i < loopCount && !token.IsCancellationRequested; i++)
        {
            onLoop?.Invoke();
            if (token.WaitHandle.WaitOne(intervalMillis)) break;
        }
        onComplete?.Invoke();
    }

    private void DelayAction(int delayMillis, Action onComplete, CancellationToken token)
    {
        if (!token.WaitHandle.WaitOne(delayMillis))
        {
            onComplete?.Invoke();
        }
    }
}
