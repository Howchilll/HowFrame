
using System;
using System.Collections.Generic;

public class OnceOrderAction
{
    private readonly SortedDictionary<int, Action> _actions = new();
    private readonly object _lock = new();
    private bool _invoked;
    private readonly Action<string> _log;

    public OnceOrderAction(Action<string> logger = null)
    {
        _log = logger;
    }

    public static OnceOrderAction operator +(OnceOrderAction order, (Action action, int index) entry)
    {
        if (entry.action == null) throw new ArgumentNullException(nameof(entry.action));
        lock (order._lock)
        {
            if (order._invoked) return order;
            if (order._actions.ContainsKey(entry.index))
            {
                order._log?.Invoke($"[OnceOrderAction] Index {entry.index} ÒÑ´æÔÚ£¬¸²¸Ç¾ÉµÄ Action");
                order._actions[entry.index] = entry.action;
            }
            else
            {
                order._actions.Add(entry.index, entry.action);
            }
        }
        return order;
    }

    public static OnceOrderAction operator -(OnceOrderAction order, int index)
    {
        lock (order._lock)
        {
            if (!order._invoked)
                order._actions.Remove(index);
        }
        return order;
    }

    public void Invoke()
    {
        List<Action> toInvoke;
        lock (_lock)
        {
            if (_invoked) return;
            toInvoke = new List<Action>(_actions.Values);
            _actions.Clear();
            _invoked = true;
        }

        foreach (var a in toInvoke)
            a?.Invoke();
    }

    public bool IsInvoked
    {
        get { lock (_lock) { return _invoked; } }
    }
}

