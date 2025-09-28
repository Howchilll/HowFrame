using System;
using System.Collections.Generic;
using System.Threading;
using HowEnum;

namespace HowFrame
{
    public static class ThreadAssistant
    {
        private static readonly Dictionary<string, ThreadHelper> _threads = new();
        private static readonly Dictionary<EnumKeyBase, ThreadHelper> _enumThreads = new();

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
            Stop(key);
            var helper = new ThreadHelper(intervalMillis, onTick, onStart);
            _enumThreads[key] = helper;
            helper.Start();
        }

        public static void DelayInvoke(EnumKeyBase key, int delayMillis, Action onComplete)
        {
            Stop(key);
            var helper = new ThreadHelper(delayMillis, null, null, onComplete);
            _enumThreads[key] = helper;
            helper.Start();
        }

        public static void Stop(EnumKeyBase key)
        {
            if (_enumThreads.TryGetValue(key, out var helper))
            {
                helper.Stop();
                _enumThreads.Remove(key);
            }
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
            private bool _running;

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
                _onStart?.Invoke();
                while (_running)
                {
                    _onTick?.Invoke();
                    Thread.Sleep(_intervalMillis);
                }
            }

            private void RunDelay()
            {
                Thread.Sleep(_intervalMillis);
                _onComplete?.Invoke();
            }
        }

        #endregion
    }
}
