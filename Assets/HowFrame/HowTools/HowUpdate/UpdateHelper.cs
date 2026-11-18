using System;

namespace HowFrame
{
    public class UpdateHelper : IDisposable
    {
        public event Action OnUpdate;

        internal readonly int Fps;
        internal readonly bool IsSystemUpdate;
        private bool _disposed;

        // 每隔多少个基础帧（Updater.BaseFPS = 60）触发一次
        private readonly int _interval;

        /// <param name="fps">1~60 之间的任意值；会按 60 / fps 取整成触发间隔（如 20 → 每 3 帧一次）</param>
        public UpdateHelper(int fps = 60, bool isSystemUpdate = false)
        {
            if (fps < 1 || fps > Updater.BaseFPS)
            {
                UnityEngine.Debug.LogWarning($"UpdateHelper: fps={fps} 超出 1~{Updater.BaseFPS}，已限制到范围内");
                fps = Math.Clamp(fps, 1, Updater.BaseFPS);
            }
            Fps = fps;
            _interval = Math.Max(1, (int)Math.Round((double)Updater.BaseFPS / fps));
            IsSystemUpdate = isSystemUpdate;
            Updater.Instance.Register(this);
        }

        internal bool IsActiveForCurrentFrame(int frameCount)
        {
            return frameCount % _interval == 0;
        }

        internal void InvokeInternal()
        {
            if (!_disposed)
                OnUpdate?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Updater.Instance.Unregister(this);
        }
    }
}