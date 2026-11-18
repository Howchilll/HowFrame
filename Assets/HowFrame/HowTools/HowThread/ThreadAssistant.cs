using System;
using System.Collections.Generic;
using System.Threading;
using HowEnum;

namespace HowFrame
{
    /// <summary>
    /// 后台线程循环 / 延迟任务。
    /// ⚠ 回调运行在后台线程：不能直接调用 Unity API（GameObject、Transform、UI 等），
    ///   需要回到主线程时请用 ThreadHelper / 主线程派发；本类的 Start / Stop 也请只在主线程调用。
    /// </summary>
    public static class ThreadAssistant
    {
        private static readonly Dictionary<string, ThreadHelper> _threads = new();

        #region 字符串 Key 版本

        public static void StartLoop(string name, int intervalMillis, Action onTick, Action onStart = null)
        {
            Stop(name);
            var helper = new ThreadHelper(intervalMillis, onTick, onStart);
            _threads[name] = helper;
            helper.Start();
        }

        public static void DelayInvoke(string name, int delayMillis, Action onComplete)
        {
            Stop(name);
            var helper = new ThreadHelper(delayMillis, null, null, onComplete);
            _threads[name] = helper;
            helper.Start();
        }

        public static void Stop(string name)
        {
            if (_threads.TryGetValue(name, out var helper))
            {
                helper.Stop();
                _threads.Remove(name);
            }
        }

        #endregion

        #region EnumKeyBase Key 版本

        public static void StartLoop(EnumKeyBase key, int intervalMillis, Action onTick, Action onStart = null)
        {
            StartLoop(key.name, intervalMillis, onTick, onStart);
        }

        public static void DelayInvoke(EnumKeyBase key, int delayMillis, Action onComplete)
        {
            DelayInvoke(key.name, delayMillis, onComplete);
        }

        public static void Stop(EnumKeyBase key)
        {
            Stop(key.name);
        }

        #endregion

        #region 内部线程辅助类

        private class ThreadHelper
        {
            private readonly Thread _thread;
            private readonly int _intervalMillis;
            private readonly Action _onTick;
            private readonly Action _onStart;
            private readonly Action _onComplete;
            private volatile bool _running; // 跨线程读写

            // 循环任务构造
            public ThreadHelper(int intervalMillis, Action onTick, Action onStart = null)
            {
                _intervalMillis = intervalMillis;
                _onTick = onTick;
                _onStart = onStart;
                _thread = new Thread(RunLoop) { IsBackground = true };
            }

            // 延迟任务构造
            public ThreadHelper(int delayMillis, Action onTick, Action onStart, Action onComplete)
            {
                _intervalMillis = delayMillis;
                _onComplete = onComplete;
                _thread = new Thread(RunDelay) { IsBackground = true };
            }

            public void Start()
            {
                _running = true;
                _thread.Start();
            }

            public void Stop()
            {
                _running = false;
            }

            private void RunLoop()
            {
                try
                {
                    _onStart?.Invoke();
                    while (_running)
                    {
                        _onTick?.Invoke();
                        Thread.Sleep(_intervalMillis);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            private void RunDelay()
            {
                try
                {
                    Thread.Sleep(_intervalMillis);
                    if (_running) _onComplete?.Invoke(); // Stop() 后不再执行
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        #endregion
    }
}
