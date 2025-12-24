using System;
using System.Collections.Generic;
using UnityEngine;

namespace HowFrame
{
    public class Defer :IDisposable
    {
        private readonly Stack<Action> _actions = new();
        
        public void Add(Action action)
        {
            _actions.Push(action);
        }
        
        public void Dispose()
        {
            while (_actions.Count > 0)
            {
                _actions.Pop()();
            }
        }
    }

}
