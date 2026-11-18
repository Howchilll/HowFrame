using System.Linq;
using HowEnum;
using UnityEngine;

namespace HowFrame
{
    /// <summary>控制台内置命令（与框架各模块对接）</summary>
    internal static class HowConsoleBuiltins
    {
        public static void Register()
        {
            HowConsole.Register("help", "列出命令；help <命令> 查看用法", args =>
            {
                if (args.Length > 0)
                {
                    var cmd = HowConsole.Commands.FirstOrDefault(c => string.Equals(c.Name, args[0], System.StringComparison.OrdinalIgnoreCase));
                    return cmd == null ? $"没有命令 {args[0]}" : $"{cmd.Usage}\n  {cmd.Description}";
                }
                return string.Join("\n", HowConsole.Commands.Select(c => $"{c.Name,-16} {c.Description}"));
            }, "help [命令]");

            HowConsole.Register("clear", "清空控制台", _ => HowConsole.Clear());
            HowConsole.Register("echo", "原样输出", args => (object)string.Join(" ", args), "echo <文本>");
            HowConsole.Register("history", "最近输入过的命令", _ => (object)HowConsole.History);

            HowConsole.Register("timescale", "查看 / 设置 Time.timeScale", args =>
            {
                if (args.Length > 0 && HowConsole.TryConvert(args[0], typeof(float), out var v))
                    Time.timeScale = Mathf.Max(0f, (float)v);
                return $"timeScale = {Time.timeScale}";
            }, "timescale [值]");

            HowConsole.Register("lang", "查看 / 切换语言", args =>
            {
                var all = LangTypeEnum.GetAll();
                if (args.Length == 0)
                    return $"当前: {LangManager.CurrentLanguage.Value?.name ?? "未加载"}　可选: {string.Join(", ", all.Select(l => l.name))}";
                var target = all.FirstOrDefault(l => string.Equals(l.name, args[0], System.StringComparison.OrdinalIgnoreCase));
                if (target == null) return $"没有语言 {args[0]}，可选: {string.Join(", ", all.Select(l => l.name))}";
                _ = LangManager.SetLanguage(target);
                return $"切换到 {target.name}";
            }, "lang [语言]");

            HowConsole.Register("lang.missing", "运行中缺失的语言 key", _ =>
                LangManager.MissingCount == 0
                    ? "没有缺失的 key"
                    : (object)string.Join("\n", LangManager.MissingKeys.SelectMany(p => p.Value.Select(k => $"{p.Key}/{k}"))));

            HowConsole.Register("ui.show", "显示面板", args =>
            {
                if (args.Length == 0) return "用法: ui.show <面板名> [层级]";
                short level = 0;
                if (args.Length > 1 && HowConsole.TryConvert(args[1], typeof(int), out var l)) level = (short)(int)l;
                UIManager.Show(args[0], level);
                return null;
            }, "ui.show <面板名> [层级]");

            HowConsole.Register("ui.hide", "隐藏面板", args =>
            {
                if (args.Length == 0) return "用法: ui.hide <面板名>";
                UIManager.Hide(args[0]);
                return null;
            }, "ui.hide <面板名>");

            HowConsole.Register("ui.back", "导航栈返回上一个面板", _ => UIManager.Back() ? null : "导航栈为空");

            HowConsole.Register("ui.list", "已创建的面板与导航栈", _ =>
            {
                var panels = UIManager.Panels().Select(p => $"{p.name}{(p.visible ? " (显示)" : "")}").ToList();
                return (panels.Count == 0 ? "没有已创建的面板" : string.Join("\n", panels)) +
                       "\n栈: " + (UIManager.StackCount == 0 ? "空" : string.Join(" → ", UIManager.StackNames));
            });

            HowConsole.Register("event", "触发全局事件；不带参数时列出事件", args =>
            {
                if (args.Length == 0)
                    return EventAssistant.EventDictionary.Count == 0 ? "没有已订阅的事件" : (object)string.Join("\n", EventAssistant.EventDictionary.Keys.OrderBy(k => k));
                EventAssistant.Invoke(args[0]);
                return $"已触发 {args[0]}";
            }, "event [事件名]");

            HowConsole.Register("assets", "已加载的 Label 与缓存数量", _ =>
                (object)$"Label: {string.Join(", ", AssetAssistant.LoadedLabels)}　缓存资源: {AssetAssistant.CachedCount}");

            HowConsole.Register("shader.list", "ShaderManager 中的 Shader 与材质", _ =>
                (object)("Shader: " + string.Join(", ", ShaderManager.Shaders.Keys.Distinct().OrderBy(k => k)) +
                         "\n材质: " + string.Join(", ", ShaderManager.MaterialKeys.OrderBy(k => k))));

            HowConsole.Register("shader.set", "按字符串设置材质参数（key 为材质 key 或 Shader 名）", args =>
            {
                if (args.Length < 3) return "用法: shader.set <材质key|Shader名> <属性> <值>";
                return ShaderManager.TrySetFromString(args[0], args[1], string.Join(" ", args.Skip(2)), out var error)
                    ? $"{args[0]}.{args[1]} = {string.Join(" ", args.Skip(2))}"
                    : error;
            }, "shader.set <材质key|Shader名> <属性> <值>");

            HowConsole.Register("shader.keyword", "开关材质关键字", args =>
            {
                if (args.Length < 3 || !HowConsole.TryConvert(args[2], typeof(bool), out var on)) return "用法: shader.keyword <材质key|Shader名> <关键字> <on|off>";
                int n = ShaderManager.SetKeyword(args[0], args[1], (bool)on);
                return $"已修改 {n} 个材质";
            }, "shader.keyword <材质key|Shader名> <关键字> <on|off>");

            HowConsole.Register("shader.global", "设置全局 float 参数，或开关全局关键字", args =>
            {
                if (args.Length < 2) return "用法: shader.global <属性> <float值>　或　shader.global <关键字> <on|off>";
                // 数字 → float 参数；on/off/true/false → 关键字
                if (HowConsole.TryConvert(args[1], typeof(float), out var f))
                {
                    ShaderManager.SetGlobalFloat(args[0], (float)f);
                    return $"{args[0]} = {f}";
                }
                if (HowConsole.TryConvert(args[1], typeof(bool), out var on))
                {
                    ShaderManager.SetGlobalKeyword(args[0], (bool)on);
                    return $"关键字 {args[0]} {((bool)on ? "开" : "关")}";
                }
                return $"无法解析 {args[1]}";
            }, "shader.global <属性|关键字> <值|on|off>");

            HowConsole.Register("shader.clone", "克隆材质（来源为材质 key、材质资源名或 Shader 名）", args =>
            {
                if (args.Length < 2) return "用法: shader.clone <新key> <来源>";
                return ShaderManager.CloneMaterial(args[0], args[1]) != null ? $"已创建材质 {args[0]}" : $"克隆失败: {args[1]}";
            }, "shader.clone <新key> <来源>");

            HowConsole.Register("fps","当前帧率", _ => (object)$"{1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F1} fps");
        }
    }
}
