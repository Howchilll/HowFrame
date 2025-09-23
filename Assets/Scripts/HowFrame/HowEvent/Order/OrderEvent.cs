using System;
using System.Collections.Generic;
namespace HowFrame
{

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
                Console.WriteLine($"[OrderEvent] Index {entry.index} �Ѵ��ڣ����Ǿɵ� Func");
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
}