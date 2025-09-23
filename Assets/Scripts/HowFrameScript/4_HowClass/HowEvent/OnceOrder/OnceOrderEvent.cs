using System;
using System.Collections.Generic;

public class OnceOrderEvent<TArg, TResult>
{
    private readonly SortedDictionary<int, Func<TArg, TResult>> _funcs = new();
    private readonly object _lock = new();
    private bool _invoked;
    private readonly Action<string> _log;

    public OnceOrderEvent(Action<string> logger = null)
    {
        _log = logger;
    }

    public static OnceOrderEvent<TArg, TResult> operator +(OnceOrderEvent<TArg, TResult> order, (Func<TArg, TResult> func, int index) entry)
    {
        if (entry.func == null) throw new ArgumentNullException(nameof(entry.func));
        lock (order._lock)
        {
            if (order._invoked) return order;
            if (order._funcs.ContainsKey(entry.index))
            {
                order._log?.Invoke($"[OnceOrderEvent] Index {entry.index} ÒÑ´æÔÚ£¬¸²¸Ç¾ÉµÄ Func");
                order._funcs[entry.index] = entry.func;
            }
            else
            {
                order._funcs.Add(entry.index, entry.func);
            }
        }
        return order;
    }

    public static OnceOrderEvent<TArg, TResult> operator -(OnceOrderEvent<TArg, TResult> order, int index)
    {
        lock (order._lock)
        {
            if (!order._invoked)
                order._funcs.Remove(index);
        }
        return order;
    }

    public (TResult LastResult, List<TResult> AllResults) Invoke(TArg arg)
    {
        List<Func<TArg, TResult>> toInvoke;
        lock (_lock)
        {
            if (_invoked || _funcs.Count == 0)
                return (default!, new List<TResult>());

            toInvoke = new List<Func<TArg, TResult>>(_funcs.Values);
            _funcs.Clear();
            _invoked = true;
        }

        var results = new List<TResult>();
        foreach (var f in toInvoke)
            results.Add(f(arg));

        return (results.Count > 0 ? results[^1] : default!, results);
    }

    public bool IsInvoked
    {
        get { lock (_lock) { return _invoked; } }
    }
}