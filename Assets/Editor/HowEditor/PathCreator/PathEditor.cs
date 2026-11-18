#define EDITOR
#if UNITY_EDITOR
using System;
using System.Reflection;

/// <summary>
/// 把 "类名.字段名"（如 GlobalPath.LangPath）解析成 PathCreator 生成的路径常量值。
/// </summary>
public static class PathEditor
{
    private static readonly System.Text.RegularExpressions.Regex IndexPattern =
        new System.Text.RegularExpressions.Regex(@"^[A-Za-z_][\w.]*\.[A-Za-z_]\w*$");

    /// <summary>
    /// 安全解析：形如 "类名.字段名" 且能找到对应常量时返回常量值，否则原样返回（普通文件路径不受影响）。
    /// </summary>
    public static string Resolve(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Contains("/") || raw.Contains("\\") || !IndexPattern.IsMatch(raw))
            return raw;
        int lastDot = raw.LastIndexOf('.');
        Type type = FindType(raw.Substring(0, lastDot));
        var field = type?.GetField(raw.Substring(lastDot + 1), BindingFlags.Public | BindingFlags.Static);
        return field?.GetValue(null) as string ?? raw;
    }

    private static Type FindType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            foreach (var t in types)
                if (t != null && t.Name == typeName) return t;
        }
        return null;
    }

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
