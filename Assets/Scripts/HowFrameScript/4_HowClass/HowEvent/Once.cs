using System;
using System.Collections.Generic;

public class OnceAction
{
    private Action _action;

    public Action Action
    {
        get => _action;
        set => _action = value;
    }

    public void Invoke()
    {
        var temp = _action;
        _action = null;
        temp?.Invoke();
    }

    public bool IsInvoked => _action == null;
}

public class OnceEvent<TArg, TResult>
{
    private Func<TArg, TResult> _func;

    public Func<TArg, TResult> Func
    {
        get => _func;
        set => _func = value;
    }

    public (TResult LastResult, List<TResult> AllResults) Invoke(TArg arg)
    {
        var temp = _func;
        _func = null;

        if (temp == null)
            return (default!, new List<TResult>());

        var results = new List<TResult>();
        foreach (Func<TArg, TResult> f in temp.GetInvocationList())
        {
            results.Add(f(arg));
        }

        return (results[^1], results);
    }

    public bool IsInvoked => _func == null;
}