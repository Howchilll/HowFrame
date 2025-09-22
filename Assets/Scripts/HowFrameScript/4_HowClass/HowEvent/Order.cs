using System;
using System.Collections.Generic;

public class OrderAction
{
    private readonly SortedDictionary<int, Action> _actions = new();
    private readonly object _lock = new();

    public static OrderAction operator +(OrderAction order, (Action action, int index) entry)
    {
        lock (order._lock)
        {
            if (order._actions.ContainsKey(entry.index))
            {
                Console.WriteLine($"[OrderAction] Index {entry.index} 已存在，覆盖旧的 Action");
                order._actions[entry.index] = entry.action;
            }
            else
            {
                order._actions.Add(entry.index, entry.action);
            }
        }
        return order;
    }

    public static OrderAction operator -(OrderAction order, int index)
    {
        lock (order._lock)
        {
            order._actions.Remove(index);
        }
        return order;
    }

    public void Invoke()
    {
        List<Action> toInvoke;
        lock (_lock)
        {
            toInvoke = new List<Action>(_actions.Values);
        }

        foreach (var a in toInvoke)
            a?.Invoke();
    }
}

public class OrderEvent<TArg, TResult>
{
    private readonly SortedDictionary<int, Func<TArg, TResult>> _funcs = new();
    private readonly object _lock = new();

    public static OrderEvent<TArg, TResult> operator +(OrderEvent<TArg, TResult> order, (Func<TArg, TResult> func, int index) entry)
    {
        lock (order._lock)
        {
            if (order._funcs.ContainsKey(entry.index))
            {
                Console.WriteLine($"[OrderEvent] Index {entry.index} 已存在，覆盖旧的 Func");
                order._funcs[entry.index] = entry.func;
            }
            else
            {
                order._funcs.Add(entry.index, entry.func);
            }
        }
        return order;
    }

    public static OrderEvent<TArg, TResult> operator -(OrderEvent<TArg, TResult> order, int index)
    {
        lock (order._lock)
        {
            order._funcs.Remove(index);
        }
        return order;
    }

    public (TResult LastResult, List<TResult> AllResults) Invoke(TArg arg)
    {
        List<Func<TArg, TResult>> toInvoke;
        lock (_lock)
        {
            if (_funcs.Count == 0)
                return (default!, new List<TResult>());
            toInvoke = new List<Func<TArg, TResult>>(_funcs.Values);
        }

        var results = new List<TResult>();
        foreach (var f in toInvoke)
            results.Add(f(arg));

        return (results[^1], results);
    }
}
