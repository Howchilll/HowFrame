using System;
using System.Reflection;
using UnityEngine;

public static class DebugAssistant
{
    public static void Log(this object obj, string prefix = "")
    {
#if UNITY_EDITOR
        if (obj == null)
        {
            Debug.Log($"{prefix}<null>");
            return;
        }
        Type type = obj.GetType();
        if (type.IsPrimitive || obj is string || obj is decimal)
        {
            Debug.Log($"{prefix}{obj}");
            return;
        }
        string result = $"{prefix}<{type.Name}>";
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(obj);
            result += $"\n  [Field] {field.Name} = {FormatValue(value)}";
        }
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

        Debug.Log(result);
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
            return $"<{type.Name}>"; // 不展开
        }
    }
}