using System;
using System.Collections.Generic;
using UnityEngine;
using HowEnum;

namespace HowFrame
{
    // ---------------- 无参数事件对象版 ----------------
    public class EventHelper
    {
        private readonly Dictionary<string, Action> _stringEvents = new();
        private readonly Dictionary<EnumKeyBase, Action> _enumEvents = new();

        // ---------- 字符串 Key ----------
        public void Subscribe(string key, Action listener)
        {
            if (!_stringEvents.ContainsKey(key)) _stringEvents[key] = listener;
            else _stringEvents[key] += listener;
        }

        public void Unsubscribe(string key, Action listener)
        {
            if (_stringEvents.ContainsKey(key))
            {
                _stringEvents[key] -= listener;
                if (_stringEvents[key] == null) _stringEvents.Remove(key);
            }
        }

        public void Invoke(string key)
        {
            if (_stringEvents.TryGetValue(key, out var action)) action?.Invoke();
#if UNITY_EDITOR
            else Debug.LogWarning($"[EventHelper] 字符串事件未注册: {key}");
#endif
        }

        public void ClearOne(string key)
        {
            if (_stringEvents.ContainsKey(key)) _stringEvents.Remove(key);
        }

        public void ClearAll() => _stringEvents.Clear();

        // ---------- EnumKeyBase Key ----------
        public void Subscribe(EnumKeyBase key, Action listener)
        {
            if (!_enumEvents.ContainsKey(key)) _enumEvents[key] = listener;
            else _enumEvents[key] += listener;
        }

        public void Unsubscribe(EnumKeyBase key, Action listener)
        {
            if (_enumEvents.ContainsKey(key))
            {
                _enumEvents[key] -= listener;
                if (_enumEvents[key] == null) _enumEvents.Remove(key);
            }
        }

        public void Invoke(EnumKeyBase key)
        {
            if (_enumEvents.TryGetValue(key, out var action)) action?.Invoke();
#if UNITY_EDITOR
            else Debug.LogWarning($"[EventHelper] EnumKeyBase 事件未注册: {key}");
#endif
        }

        public void ClearOne(EnumKeyBase key)
        {
            if (_enumEvents.ContainsKey(key)) _enumEvents.Remove(key);
        }

        public void ClearAllEnum() => _enumEvents.Clear();
    }

    // ---------------- 泛型事件对象版 ----------------
    public class EventHelper<T, TResult>
    {
        private readonly Dictionary<string, Func<T, TResult>> _stringEvents = new();
        private readonly Dictionary<EnumKeyBase, Func<T, TResult>> _enumEvents = new();

        // ---------- 字符串 Key ----------
        public void Subscribe(string key, Func<T, TResult> func)
        {
            if (_stringEvents.ContainsKey(key))
                _stringEvents[key] += func;
            else
                _stringEvents[key] = func;
        }

        public void Unsubscribe(string key, Func<T, TResult> func)
        {
            if (_stringEvents.ContainsKey(key))
            {
                _stringEvents[key] -= func;
                if (_stringEvents[key] == null) _stringEvents.Remove(key);
            }
        }

        public TResult Invoke(string key, T arg)
        {
            if (_stringEvents.TryGetValue(key, out var func)) return func.Invoke(arg);

#if UNITY_EDITOR
            Debug.LogWarning($"[EventHelper<{typeof(T).Name},{typeof(TResult).Name}>] 字符串事件未注册: {key}");
#endif
            return default;
        }

        public void ClearOne(string key)
        {
            if (_stringEvents.ContainsKey(key)) _stringEvents.Remove(key);
        }

        public void ClearAll() => _stringEvents.Clear();

        // ---------- EnumKeyBase Key ----------
        public void Subscribe(EnumKeyBase key, Func<T, TResult> func)
        {
            if (_enumEvents.ContainsKey(key))
                _enumEvents[key] += func;
            else
                _enumEvents[key] = func;
        }

        public void Unsubscribe(EnumKeyBase key, Func<T, TResult> func)
        {
            if (_enumEvents.ContainsKey(key))
            {
                _enumEvents[key] -= func;
                if (_enumEvents[key] == null) _enumEvents.Remove(key);
            }
        }

        public TResult Invoke(EnumKeyBase key, T arg)
        {
            if (_enumEvents.TryGetValue(key, out var func)) return func.Invoke(arg);

#if UNITY_EDITOR
            Debug.LogWarning($"[EventHelper<{typeof(T).Name},{typeof(TResult).Name}>] EnumKeyBase 事件未注册: {key}");
#endif
            return default;
        }

        public void ClearOne(EnumKeyBase key)
        {
            if (_enumEvents.ContainsKey(key)) _enumEvents.Remove(key);
        }

        public void ClearAllEnum() => _enumEvents.Clear();
    }
}
