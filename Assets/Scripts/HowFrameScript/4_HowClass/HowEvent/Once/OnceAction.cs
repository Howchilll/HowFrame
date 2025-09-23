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
