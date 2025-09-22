using System;
using System.Collections.Generic;

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

    public Ref(T value = default) => _value = value;
    
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
    public static implicit operator Ref<T>(T value) => new Ref<T>(value);
}