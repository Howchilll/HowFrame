#if UNITY_EDITOR
using System;
using System.Reflection;

public static class PathEditor
{
    public static string FindPath(string pathVariable)
    {
        if (string.IsNullOrEmpty(pathVariable)) return null;

        int lastDot = pathVariable.LastIndexOf('.');
        if (lastDot < 0) return null;

        string typeName = pathVariable.Substring(0, lastDot);
        string fieldName = pathVariable.Substring(lastDot + 1);

        Type type = null;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 先尝试完整匹配
            type = asm.GetType(typeName);
            if (type != null) break;

            // 如果用户没写命名空间，就在程序集里搜同名类
            foreach (var t in asm.GetTypes())
            {
                if (t.Name == typeName)
                {
                    type = t;
                    break;
                }
            }

            if (type != null) break;
        }

        if (type == null)
        {
            UnityEngine.Debug.LogError("找不到类型: " + typeName);
            return null;
        }

        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        if (field == null)
        {
            UnityEngine.Debug.LogError($"找不到字段: {fieldName} in {type.FullName}");
            return null;
        }

        return field.GetValue(null) as string;
    }
}
#endif
