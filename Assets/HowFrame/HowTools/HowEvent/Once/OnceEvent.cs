using System;
using System.Collections.Generic;
namespace HowFrame
{


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
        foreach (var @delegate in temp.GetInvocationList())
        {
            var f = (Func<TArg, TResult>)@delegate;
            results.Add(f(arg));
        }

        return (results[^1], results);
    }

    public bool IsInvoked => _func == null;
}
}