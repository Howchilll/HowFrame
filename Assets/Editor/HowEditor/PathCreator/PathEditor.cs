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

        // 在所有已加载程序集里找类型
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType(typeName);
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
            UnityEngine.Debug.LogError($"找不到字段: {fieldName} in {typeName}");
            return null;
        }

        return field.GetValue(null) as string;
    }
}
