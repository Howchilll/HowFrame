using System;
using System.Collections.Generic;

public static class PropertyAssistant<T>
{
    private class Entry
    {
        public Ref<T> Obj;
        public Action<T> Callback;
    }

    private static readonly Dictionary<string, Entry> _dict = new Dictionary<string, Entry>();

    public static void SetEvent(string key, Action<T> callback)
    {
        if (!_dict.ContainsKey(key))
            _dict[key] = new Entry();

        var entry = _dict[key];
        entry.Callback = callback;

        // 如果已经有对象，立即绑定
        if (entry.Obj != null)
            entry.Obj.OnChanged += callback;
    }

    public static void SetObj(string key, Ref<T> obj)
    {
        if (!_dict.ContainsKey(key))
            _dict[key] = new Entry();

        var entry = _dict[key];
        entry.Obj = obj;

        // 如果已有回调，立即绑定
        if (entry.Callback != null)
            obj.OnChanged += entry.Callback;
    }

    public static void Remove(string key)
    {
        if (_dict.ContainsKey(key))
            _dict.Remove(key);
    }

    public static void ClearAll()
    {
        _dict.Clear();
    }
}

