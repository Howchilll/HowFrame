using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 把静态方法注册为控制台命令：
    /// <code>[HowCommand("gold.add", "加金币")] static void AddGold(int amount) { ... }</code>
    /// 支持参数类型：string / int / long / float / double / bool / 枚举，可带默认值；有返回值时会打印出来。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HowCommandAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Description;

        public HowCommandAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// 运行时命令控制台：命令注册 / 解析执行 / 历史 / 日志收集。
    /// 界面由 HowConsoleView 提供（默认 ` 或 F1 键开关），逻辑本身不依赖界面，可直接调用 Execute。
    /// 默认只在编辑器和 Development Build 中启用（Enabled）。
    /// </summary>
    public static class HowConsole
    {
        public class Command
        {
            public string Name;
            public string Description;
            public string Usage;
            internal Func<string[], object> Invoke;
        }

        public enum LineType { Input, Output, Log, Warning, Error }

        public readonly struct Line
        {
            public readonly LineType Type;
            public readonly string Text;

            public Line(LineType type, string text)
            {
                Type = type;
                Text = text;
            }
        }

        /// <summary>是否启用控制台界面（默认：编辑器 / Development Build）</summary>
        public static bool Enabled = Debug.isDebugBuild;

        /// <summary>日志最多保留的行数</summary>
        public static int MaxLines = 300;

        /// <summary>是否把 Unity 日志（Debug.Log 等）也收进控制台</summary>
        public static bool CaptureUnityLog = true;

        public static event Action<Line> OnLine;

        private static readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Line> _lines = new List<Line>();
        private static readonly List<string> _history = new List<string>();
        private static bool _scanned;
        private static bool _initialized;

        public static IReadOnlyList<Line> Lines => _lines;
        public static IReadOnlyList<string> History => _history;

        public static IEnumerable<Command> Commands
        {
            get
            {
                EnsureScanned();
                return _commands.Values.OrderBy(c => c.Name);
            }
        }

        #region 注册

        /// <summary>手动注册命令（args 为空格分隔后的参数），返回值非空时会打印</summary>
        public static void Register(string name, string description, Func<string[], object> handler, string usage = null)
        {
            if (string.IsNullOrWhiteSpace(name) || handler == null) return;
            _commands[name.Trim()] = new Command
            {
                Name = name.Trim(),
                Description = description ?? "",
                Usage = usage ?? name.Trim(),
                Invoke = handler,
            };
        }

        public static void Register(string name, string description, Action<string[]> handler, string usage = null)
        {
            if (handler == null) return;
            Register(name, description, args => { handler(args); return null; }, usage);
        }

        public static void Unregister(string name)
        {
            if (name != null) _commands.Remove(name);
        }

        /// <summary>注册一个静态方法（参数按类型自动转换）</summary>
        public static void RegisterMethod(string name, string description, MethodInfo method)
        {
            if (method == null || !method.IsStatic) return;
            var parameters = method.GetParameters();
            string usage = name + string.Concat(parameters.Select(p =>
                p.HasDefaultValue ? $" [{p.Name}:{TypeName(p.ParameterType)}={p.DefaultValue}]" : $" <{p.Name}:{TypeName(p.ParameterType)}>"));

            Register(name, description, args =>
            {
                var values = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    if (i < args.Length)
                    {
                        // 最后一个 string 参数吃掉剩余全部参数，方便传带空格的文本
                        string raw = i == parameters.Length - 1 && p.ParameterType == typeof(string) && args.Length > parameters.Length
                            ? string.Join(" ", args.Skip(i))
                            : args[i];
                        if (!TryConvert(raw, p.ParameterType, out values[i]))
                            throw new ArgumentException($"参数 {p.Name} 需要 {TypeName(p.ParameterType)}，收到 \"{raw}\"\n用法: {usage}");
                    }
                    else if (p.HasDefaultValue) values[i] = p.DefaultValue;
                    else throw new ArgumentException("参数不足\n用法: " + usage);
                }
                return method.Invoke(null, values);
            }, usage);
        }

        /// <summary>扫描所有引用了 HowTools 的程序集中带 [HowCommand] 的静态方法（首次使用时自动执行）</summary>
        public static void ScanAttributes()
        {
            _scanned = true;
            var self = typeof(HowConsole).Assembly;
            string selfName = self.GetName().Name;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                if (assembly != self && assembly.GetReferencedAssemblies().All(a => a.Name != selfName)) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var attr = method.GetCustomAttribute<HowCommandAttribute>();
                    if (attr != null) RegisterMethod(attr.Name, attr.Description, method);
                }
            }
        }

        private static void EnsureScanned()
        {
            if (!_scanned) ScanAttributes();
        }

        #endregion

        #region 执行

        /// <summary>执行一行命令；返回是否成功。输出写入 Lines。</summary>
        public static bool Execute(string input)
        {
            EnsureScanned();
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();

            if (_history.Count == 0 || _history[_history.Count - 1] != input) _history.Add(input);
            if (_history.Count > 50) _history.RemoveAt(0);
            Write(LineType.Input, "> " + input);

            var tokens = Tokenize(input);
            if (tokens.Count == 0) return false;
            if (!_commands.TryGetValue(tokens[0], out var command))
            {
                var similar = Complete(tokens[0]).Take(5).ToList();
                Write(LineType.Error, $"未知命令: {tokens[0]}" + (similar.Count > 0 ? "　你是不是想输入: " + string.Join(", ", similar) : "　输入 help 查看全部命令"));
                return false;
            }

            try
            {
                object result = command.Invoke(tokens.Skip(1).ToArray());
                if (result != null) Write(LineType.Output, FormatResult(result));
                return true;
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                Write(LineType.Error, e.InnerException.Message);
                return false;
            }
            catch (Exception e)
            {
                Write(LineType.Error, e.Message);
                return false;
            }
        }

        /// <summary>以 prefix 开头的命令名（不区分大小写），用于 Tab 补全</summary>
        public static IEnumerable<string> Complete(string prefix)
        {
            EnsureScanned();
            prefix ??= "";
            return _commands.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k);
        }

        /// <summary>按空格分词，支持 "带 空格" 的参数</summary>
        public static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(input)) return tokens;

            var sb = new StringBuilder();
            bool inQuote = false, hasToken = false;
            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuote = !inQuote;
                    hasToken = true;
                }
                else if (char.IsWhiteSpace(c) && !inQuote)
                {
                    if (hasToken) tokens.Add(sb.ToString());
                    sb.Clear();
                    hasToken = false;
                }
                else
                {
                    sb.Append(c);
                    hasToken = true;
                }
            }
            if (hasToken) tokens.Add(sb.ToString());
            return tokens;
        }

        public static bool TryConvert(string raw, Type type, out object value)
        {
            value = null;
            var inv = CultureInfo.InvariantCulture;
            if (type == typeof(string)) { value = raw; return true; }
            if (type == typeof(int) && int.TryParse(raw, NumberStyles.Integer, inv, out var i)) { value = i; return true; }
            if (type == typeof(long) && long.TryParse(raw, NumberStyles.Integer, inv, out var l)) { value = l; return true; }
            if (type == typeof(float) && float.TryParse(raw, NumberStyles.Float, inv, out var f)) { value = f; return true; }
            if (type == typeof(double) && double.TryParse(raw, NumberStyles.Float, inv, out var d)) { value = d; return true; }
            if (type == typeof(bool))
            {
                switch (raw?.ToLowerInvariant())
                {
                    case "1": case "true": case "on": case "yes": value = true; return true;
                    case "0": case "false": case "off": case "no": value = false; return true;
                }
                return false;
            }
            if (type.IsEnum)
            {
                try
                {
                    value = Enum.Parse(type, raw, true);
                    return Enum.IsDefined(type, value) || int.TryParse(raw, out _);
                }
                catch { return false; }
            }
            return false;
        }

        #endregion

        #region 输出

        public static void Write(LineType type, string text)
        {
            var line = new Line(type, text ?? "");
            _lines.Add(line);
            if (_lines.Count > MaxLines) _lines.RemoveRange(0, _lines.Count - MaxLines);
            OnLine?.Invoke(line);
        }

        public static void Print(string text) => Write(LineType.Output, text);

        public static void Clear() => _lines.Clear();

        private static string FormatResult(object result)
        {
            if (result is string s) return s;
            if (result is System.Collections.IEnumerable list)
                return string.Join("\n", list.Cast<object>().Select(o => o?.ToString() ?? "null"));
            return result.ToString();
        }

        private static string TypeName(Type t)
        {
            if (t == typeof(int)) return "int";
            if (t == typeof(long)) return "long";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            return t.IsEnum ? string.Join("|", Enum.GetNames(t)) : t.Name;
        }

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!CaptureUnityLog) return;
            var lineType = type switch
            {
                LogType.Warning => LineType.Warning,
                LogType.Log => LineType.Log,
                _ => LineType.Error,
            };
            Write(lineType, condition);
        }

        #endregion

        /// <summary>初始化：注册内置命令、收集日志，启用时创建界面</summary>
        public static void Wake()
        {
            if (_initialized) return;
            _initialized = true;
            HowConsoleBuiltins.Register();
            Application.logMessageReceived += OnUnityLog;
            if (Enabled && Application.isPlaying) HowConsoleView.Create();
        }
    }
}
