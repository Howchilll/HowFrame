using System;
using System.Collections.Generic;
namespace HowFrame
{

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
                Console.WriteLine($"[OrderAction] Index {entry.index} �Ѵ��ڣ����Ǿɵ� Action");
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
    
    public void Clear()
    {
        lock (_lock)
        {
            _actions.Clear();
        }
    }
}
}

