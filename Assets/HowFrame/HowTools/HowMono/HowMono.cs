using System;
using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 替代 MonoBehaviour 的基类，使用 UpdateHelper 控制更新频率
    /// 子类可以重写 fps 属性来设置更新频率（默认 60fps）
    /// </summary>
    public abstract class HowMono : MonoBehaviour
    {
        private UpdateHelper helper;
        
        /// <summary>
        /// 更新频率，子类可以重写此属性来设置自定义帧率（默认 60fps）
        /// </summary>
        protected virtual int fps { get; } = 60;

        private void Awake()
        {
            helper = new UpdateHelper(fps);
            OnAwake();
        }

        private void Start()
        {
            OnStart();
            helper.OnUpdate += OnUpdate;
        }

        private void OnDestroy()
        {
            helper?.Dispose();
        }

        /// <summary>
        /// Awake 时调用，等同于 MonoBehaviour.Awake
        /// </summary>
        protected virtual void OnAwake()
        {
        }

        /// <summary>
        /// Start 时调用，等同于 MonoBehaviour.Start
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// 按指定 fps 频率更新，等同于 MonoBehaviour.Update（但频率可控）
        /// </summary>
        protected virtual void OnUpdate()
        {
        }
    }
}
