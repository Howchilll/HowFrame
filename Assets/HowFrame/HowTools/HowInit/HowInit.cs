using System;
using System.Threading.Tasks;
using HowFrame;
using UnityEngine;

/// <summary>
/// 框架总初始化：先加载 Instance 标签下的框架资源（Canvas / AudioMixer / InputActions ...），
/// 再依次 Wake 各助手。
/// - 多次调用 Init() 共享同一个初始化任务，不会重复执行；
/// - 单个模块 Wake 抛异常只记录错误，不会中断其余模块的初始化；
/// - 业务侧可用 IsReady / WhenReady / Ready 等待框架就绪。
/// </summary>
public static class HowInit
{
      /// <summary>框架自身资源所在的 Addressables 标签</summary>
      public const string InstanceLabel = "Instance";

      /// <summary>框架是否已初始化完成</summary>
      public static bool IsReady { get; private set; }

      /// <summary>初始化过程中 Wake 失败的模块数量</summary>
      public static int FailedModules { get; private set; }

      private static readonly System.Collections.Generic.List<string> _failedModuleNames = new();

      /// <summary>Wake 失败的模块名（调试窗口用）</summary>
      public static System.Collections.Generic.IReadOnlyList<string> FailedModuleNames => _failedModuleNames;

      /// <summary>初始化是否已开始（含进行中）</summary>
      public static bool IsStarted => _initTask != null;

      /// <summary>初始化完成时触发（只触发一次）。已就绪后再订阅请用 WhenReady。</summary>
      public static event Action OnReady;

      private static Task _initTask;

      /// <summary>可 await 的初始化任务；尚未开始初始化时会启动它</summary>
      public static Task Ready => _initTask ?? Init();

      public static Task Init()
      {
            return _initTask ??= InitCore();
      }

      /// <summary>框架就绪后执行；已就绪则立即执行</summary>
      public static void WhenReady(Action action)
      {
            if (action == null) return;
            if (IsReady) action();
            else OnReady += action;
      }

      private static async Task InitCore()
      {
            Debug.Log("框架初始化开始");
            AssetAssistant.Wake();
            await AssetAssistant.LoadLabelsAsync(InstanceLabel);

            FailedModules = 0;
            _failedModuleNames.Clear();
            // 基础设施优先，UI / 输入等依赖资源的模块放后面
            WakeSafe(nameof(DebugAssistant), DebugAssistant.Wake);
            WakeSafe(nameof(TypeAssistant), TypeAssistant.Wake);
            WakeSafe(nameof(MonoAssistant), MonoAssistant.Wake);
            WakeSafe(nameof(CoroutineAssistant), CoroutineAssistant.Wake);
            WakeSafe(nameof(UpdateAssistant), UpdateAssistant.Wake);
            WakeSafe(nameof(PropertyAssistant), PropertyAssistant.Wake);
            WakeSafe(nameof(DataAssistant), DataAssistant.Wake);
            WakeSafe(nameof(LangManager), LangManager.Wake);
            WakeSafe(nameof(ShaderManager), ShaderManager.Wake);
            WakeSafe(nameof(AudioManager), AudioManager.Wake);
            WakeSafe(nameof(InputAssistant), InputAssistant.Wake);
            WakeSafe(nameof(SceneLoadAssistant), SceneLoadAssistant.Wake);
            WakeSafe(nameof(UIManager), UIManager.Wake);
            WakeSafe(nameof(HowConsole), HowConsole.Wake);

            IsReady = true;
            if (FailedModules > 0)
                Debug.LogWarning($"框架初始化完成，但有 {FailedModules} 个模块 Wake 失败，详见上方错误日志");
            else
                Debug.Log("框架初始化完成");

            var callbacks = OnReady;
            OnReady = null;
            if (callbacks == null) return;
            foreach (Action callback in callbacks.GetInvocationList())
            {
                try { callback(); }
                catch (Exception e) { Debug.LogError($"[HowInit] OnReady 回调异常: {e}"); }
            }
      }

      private static void WakeSafe(string moduleName, Action wake)
      {
            try
            {
                  wake();
            }
            catch (Exception e)
            {
                  FailedModules++;
                  _failedModuleNames.Add(moduleName);
                  Debug.LogError($"[HowInit] {moduleName}.Wake 失败: {e}");
            }
      }
}
