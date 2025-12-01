using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace HowFrame
{

public enum DebugColor
{
    White,
    Red,
    Green,
    Blue,
    Yellow,
    Cyan,
    Magenta,
    Gray,
    Black
}

public static class DebugAssistant
{
    private static readonly Dictionary<string, bool> TagFilter = new Dictionary<string, bool>();

    public static void SetTag(string tag, bool enabled)
    {
        if (string.IsNullOrEmpty(tag)) return;

        if (TagFilter.ContainsKey(tag))
            TagFilter[tag] = enabled;
        else
            TagFilter.Add(tag, enabled);
    }

    private static bool IsTagEnabled(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return true; // 空字符串不限制
        if (!TagFilter.TryGetValue(tag, value: out var enabled))
        {
            TagFilter[tag] = true; // 默认开启
            return true;
        }
        return enabled;
    }


    public static void Log(this object obj, string prefix = "", DebugColor color = DebugColor.White, string tag = "")
        => LogInternal(obj, prefix, color, LogType.Log, tag);

    public static void Warning(this object obj, string prefix = "", string tag = "")
        => LogInternal(obj, prefix, DebugColor.Yellow, LogType.Warning, tag);

    public static void Error(this object obj, string prefix = "", string tag = "")
        => LogInternal(obj, prefix, DebugColor.Red, LogType.Error, tag);
    

    // ---------------- 内部实现 ----------------
    private static void LogInternal(object obj, string prefix, DebugColor color, LogType type, string tag)
    {
#if UNITY_EDITOR
        if (!IsTagEnabled(tag)) return;

        string result;

        if (obj == null)
        {
            result = $"{prefix}<color={ToHtml(color)}><null></color>";
        }
        else
        {
            Type typeInfo = obj.GetType();
            if (typeInfo.IsPrimitive || obj is string || obj is decimal)
            {
                // 为基本类型和字符串应用颜色标签
                result = $"{prefix}<color={ToHtml(color)}>{obj}</color>";
            }
            else
            {
                result = $"{prefix}<color={ToHtml(color)}><{typeInfo.Name}></color>";
                FieldInfo[] fields = typeInfo.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(obj);
                    result += $"\n  [Field] {field.Name} = {FormatValue(value)}";
                }

                PropertyInfo[] properties = typeInfo.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (PropertyInfo property in properties)
                {
                    if (property.GetIndexParameters().Length == 0 && property.CanRead)
                    {
                        object value;
                        try
                        {
                            value = property.GetValue(obj);
                        }
                        catch
                        {
                            value = "<无法访问>";
                        }
                        result += $"\n  [Prop ] {property.Name} = {FormatValue(value)}";
                    }
                }
            }
        }

        switch (type)
        {
            case LogType.Warning:
                Debug.LogWarning(result);
                break;
            case LogType.Error:
                Debug.LogError(result);
                break;
            default:
                Debug.Log(result);
                break;
        }
#endif
    }

    private static string FormatValue(object value)
    {
        if (value == null) return "<null>";
        Type type = value.GetType();

        if (type.IsPrimitive || value is string || value is decimal)
        {
            return value.ToString();
        }
        else
        {
            return $"<{type.Name}>"; // 只展开一层
        }
    }

    private static string ToHtml(DebugColor color)
    {
        switch (color)
        {
            case DebugColor.White: return "white";
            case DebugColor.Red: return "red";
            case DebugColor.Green: return "green";
            case DebugColor.Blue: return "blue";
            case DebugColor.Yellow: return "yellow";
            case DebugColor.Cyan: return "cyan";
            case DebugColor.Magenta: return "magenta";
            case DebugColor.Gray: return "gray";
            case DebugColor.Black: return "black";
            default: return "white";
        }
    }
    
    public static void Wake(){}
}
}
