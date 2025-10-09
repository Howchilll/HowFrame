using System;

namespace HowFrame
{
    public class UpdateHelper : IDisposable
    {
        public event Action OnUpdate;

        internal readonly int Fps;
        internal readonly bool IsSystemUpdate;
        private bool _disposed;

        public UpdateHelper(int fps = 60, bool isSystemUpdate = false)
        {
            Fps = fps;
            IsSystemUpdate = isSystemUpdate;
            Updater.Instance.Register(this);
        }

        internal bool IsActiveForCurrentFrame(int frameCount)
        {
            return Fps switch
            {
                60 => true,
                30 => frameCount % 2 == 0,
                15 => frameCount % 4 == 0,
                1 => frameCount % 60 == 0,
                _ => false
            };
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