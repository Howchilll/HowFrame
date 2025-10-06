using System;
using System.Collections.Generic;
using HowEnum;
using HowFrame;

public class Ref<T>
{
    private T _value;
    private Func<T> _computer;
    private bool _isComputed;
    private bool _isDirty = true;
    public event Action<T> OnChanged;

    public T Value
    {
        get
        {
            if (_isComputed && _isDirty)
            {
                _value = _computer();
                _isDirty = false;
            }
            return _value;
        }
        set
        {
            if (_isComputed) return; // 计算属性不允许直接赋值
            
            _value = value;
            OnChanged?.Invoke(_value); // 无论是否相等，都触发
        }
    }

    // ---------------- 构造函数：可赋值 ----------------
    public Ref(T initialValue= default)
    {
        _value = initialValue;
    }

    // ---------------- 构造函数：绑定 string key ----------------
    public Ref(string key, Action<T> callback, PropertyHelper helper = null)
    {
        _value = default;

        if (helper != null)
            helper.SetObj<T>(key,this).OnChange(callback);
        else
            PropertyAssistant.SetObj<T>(key,this).OnChange(callback);
    }

    // ---------------- 构造函数：绑定 EnumKeyBase key ----------------
    public Ref(EnumKeyBase key, Action<T> callback, PropertyHelper helper = null)
    {
        _value = default;

        if (helper != null)
            helper.SetObj<T>(key,this).OnChange(callback);
        else
            PropertyAssistant.SetObj<T>(key,this).OnChange(callback);
    }
    // 创建计算属性
    
    public Ref(T value,string key, Action<T> callback, PropertyHelper helper = null)
    {
        _value = value;

        if (helper != null)
            helper.SetObj<T>(key,this).OnChange(callback);
        else
            PropertyAssistant.SetObj<T>(key,this).OnChange(callback);
    }

    // ---------------- 构造函数：绑定 EnumKeyBase key ----------------
    public Ref(T value,EnumKeyBase key, Action<T> callback, PropertyHelper helper = null)
    {
        _value = value;

        if (helper != null)
            helper.SetObj<T>(key, this).OnChange(callback);
        else
            PropertyAssistant.SetObj<T>(key,this).OnChange(callback);
    }
    // 创建计算属性
    public static Ref<T> Computed(Func<T> computer)
    {
        var computedRef = new Ref<T>();
        computedRef._computer = computer;
        computedRef._isComputed = true;
        computedRef._isDirty = true;
        return computedRef;
    }
    
    // 创建计算属性（延迟初始化版本）
    public static Ref<T> ComputedLazy(Func<T> computer)
    {
        var computedRef = new Ref<T>();
        computedRef._computer = computer;
        computedRef._isComputed = true;
        computedRef._isDirty = true;
        return computedRef;
    }
    
    // 标记为脏数据，下次访问时重新计算
    public void Invalidate() => _isDirty = true;
    
    // 隐式转换：Ref<T> -> T
    public static implicit operator T(Ref<T> refValue) => refValue.Value;
    
    // 隐式转换：T -> Ref<T>
   // public static implicit operator Ref<T>(T value) => new Ref<T>(value);
    
    public void SetSilent(T value)
    {
        _value = value;
    }

    // 运算符重载：hp >> 200 调用 SetSilent
    public static Ref<T> operator &(Ref<T> r, T value)
    {
        r.SetSilent(value);
        return r;
    }
}