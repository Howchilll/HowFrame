using System;
using System.Collections.Generic;
using HowEnum;
using UnityEngine;
namespace HowFrame
{

public static class EventAssistant 
{ 
    public static readonly Dictionary<string, Action> EventDictionary = new();
    
    public static void Subscribe(string eventName, Action listener)
    {
        if (!EventDictionary.ContainsKey(eventName)) EventDictionary[eventName] = listener;
        else EventDictionary[eventName] += listener;
    }
    public static void Unsubscribe(string eventName, Action listener)
    {
        if (EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] -= listener;
            if (EventDictionary[eventName] == null) EventDictionary.Remove(eventName);
        }
    }
    
    public static void Invoke(string eventName)
    {
        if (EventDictionary.TryGetValue(eventName, out var action)) action?.Invoke();
#if UNITY_EDITOR
        else Debug.LogWarning($"[EventAssistant] 事件未注册: {eventName}");
#endif
    }

    public static void ClearOne(string eventName)
    {
        if (EventDictionary.ContainsKey(eventName)) EventDictionary.Remove(eventName);
    }
    
    public static void ClearAll()
    {
        EventDictionary.Clear();
        EnumEventDictionary.Clear();
    }
    
    private static readonly Dictionary<EnumKeyBase, Action> EnumEventDictionary = new();

    public static void Subscribe(EnumKeyBase key, Action listener)
    {
        if (!EnumEventDictionary.ContainsKey(key)) EnumEventDictionary[key] = listener;
        else EnumEventDictionary[key] += listener;
    }

    public static void Unsubscribe(EnumKeyBase key, Action listener)
    {
        if (EnumEventDictionary.ContainsKey(key))
        {
            EnumEventDictionary[key] -= listener;
            if (EnumEventDictionary[key] == null) EnumEventDictionary.Remove(key);
        }
    }

    public static void Invoke(EnumKeyBase key)
    {
        if (EnumEventDictionary.TryGetValue(key, out var action)) action?.Invoke();
#if UNITY_EDITOR
        else Debug.LogWarning($"[EventAssistant] EnumKeyBase 事件未注册: {key}");
#endif
    }

    public static void ClearOne(EnumKeyBase key)
    {
        if (EnumEventDictionary.ContainsKey(key)) EnumEventDictionary.Remove(key);
    }

}


public static class EventAssistant<T, TResult>
{
    private static readonly Dictionary<string, Func<T, TResult>> EventDictionary = new();

    public static void Subscribe(string key, Func<T, TResult> func)
    {
        if (EventDictionary.ContainsKey(key))
            EventDictionary[key] += func;
        else
            EventDictionary[key] = func;
    }

    public static void Unsubscribe(string key, Func<T, TResult> func)
    {
        if (EventDictionary.ContainsKey(key))
        {
            EventDictionary[key] -= func;
            if (EventDictionary[key] == null)
                EventDictionary.Remove(key);
        }
    }

    public static TResult Invoke(string key, T arg)
    {
        if (EventDictionary.TryGetValue(key, out var func))
        {
            return func.Invoke(arg);
        }

#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning($"[EventAssistant<{typeof(T).Name}, {typeof(TResult).Name}>] 事件未注册: {key}");
#endif
        return default;
    }

    public static void ClearOne(string key)
    {
        if (EventDictionary.ContainsKey(key)) EventDictionary.Remove(key);
    }

    public static void ClearAll()
    {
        EventDictionary.Clear();
        EnumEventDictionary.Clear();
    }
    private static readonly Dictionary<EnumKeyBase, Func<T, TResult>> EnumEventDictionary = new();

    public static void Subscribe(EnumKeyBase key, Func<T, TResult> func)
    {
        if (EnumEventDictionary.ContainsKey(key))
            EnumEventDictionary[key] += func;
        else
            EnumEventDictionary[key] = func;
    }

    public static void Unsubscribe(EnumKeyBase key, Func<T, TResult> func)
    {
        if (EnumEventDictionary.ContainsKey(key))
        {
            EnumEventDictionary[key] -= func;
            if (EnumEventDictionary[key] == null)
                EnumEventDictionary.Remove(key);
        }
    }

    public static TResult Invoke(EnumKeyBase key, T arg)
    {
        if (EnumEventDictionary.TryGetValue(key, out var func))
        {
            return func.Invoke(arg);
        }

#if UNITY_EDITOR
        Debug.LogWarning($"[EventAssistant<{typeof(T).Name}, {typeof(TResult).Name}>] EnumKeyBase 事件未注册: {key}");
#endif
        return default;
    }

    public static void ClearOne(EnumKeyBase key)
    {
        if (EnumEventDictionary.ContainsKey(key)) EnumEventDictionary.Remove(key);
    }


}
}

