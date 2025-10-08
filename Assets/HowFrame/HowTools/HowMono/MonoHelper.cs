using System;
using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 普通类想要获得 Update / FixedUpdate / LateUpdate 的能力，
    /// 可以直接持有一个 MonoTicker 实例。
    /// </summary>
    public class MonoHelper: IDisposable
    {
        private Action _onUpdate;
        private Action _onFixedUpdate;
        private Action _onLateUpdate;
        private bool _isDisposed;

        public MonoHelper(Action onUpdate = null, Action onFixedUpdate = null, Action onLateUpdate = null)
        {
            _onUpdate = onUpdate;
            _onFixedUpdate = onFixedUpdate;
            _onLateUpdate = onLateUpdate;

            // 注册进全局 MonoAssistant
            if (_onUpdate != null) MonoAssistant.AddUpdate(_onUpdate);
            if (_onFixedUpdate != null) MonoAssistant.AddFixedUpdate(_onFixedUpdate);
            if (_onLateUpdate != null) MonoAssistant.AddLateUpdate(_onLateUpdate);
        }

        /// <summary>
        /// 手动取消注册（或者让 GC 析构调用）
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_onUpdate != null) MonoAssistant.RemoveUpdate(_onUpdate);
            if (_onFixedUpdate != null) MonoAssistant.RemoveFixedUpdate(_onFixedUpdate);
            if (_onLateUpdate != null) MonoAssistant.RemoveLateUpdate(_onLateUpdate);

            _onUpdate = null;
            _onFixedUpdate = null;
            _onLateUpdate = null;
        }

        ~MonoHelper()
        {
            Dispose(); // 防止忘记手动释放
        }
    }
}