using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 全局统一帧节奏控制器（对象版 + 静态版）
    /// 自动在单例创建时启动循环
    /// </summary>
    public class Updater : MonoBehaviour
    {
        private static Updater _instance;
        public static Updater Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Updater_Core");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Updater>();
                    _instance.Init(); // 自动启动循环
                }
                return _instance;
            }
        }

        public const int BaseFPS = 60;
        private static int _frameCount;
        private static readonly float _frameDelta = 1f / BaseFPS;

        public static bool Should30 => _frameCount % 2 == 0;
        public static bool Should15 => _frameCount % 4 == 0;
        public static bool Should1 => _frameCount % 60 == 0;

        private static volatile bool _systemBusy;
        private Coroutine _loop;

        // ==== 对象版注册列表 ====
        private readonly List<UpdateHelper> _orderHelpers = new();
        private readonly List<UpdateHelper> _systemHelpers = new();

        // ==== 静态回调 ====
        private Action _staticUnityUpdate;
        private Action _staticSystemUpdate;

        // ---- 初始化 ----
        private void Init()
        {
            _loop = StartCoroutine(RunLoop());
        }

        // -------- 注册对象 --------
        internal void Register(UpdateHelper helper)
        {
            if (helper.IsSystemUpdate)
            {
                if (!_systemHelpers.Contains(helper))
                    _systemHelpers.Add(helper);
            }
            else
            {
                if (!_orderHelpers.Contains(helper))
                    _orderHelpers.Add(helper);
            }
        }

        internal void Unregister(UpdateHelper helper)
        {
            _systemHelpers.Remove(helper);
            _orderHelpers.Remove(helper);
        }

        // -------- 注册静态回调 --------
        internal void RegisterStatic(Action unityUpdate, Action systemUpdate)
        {
            _staticUnityUpdate += unityUpdate;
            _staticSystemUpdate += systemUpdate;
        }

        internal void UnregisterStatic(Action unityUpdate, Action systemUpdate)
        {
            _staticUnityUpdate -= unityUpdate;
            _staticSystemUpdate -= systemUpdate;
        }
        
        // -------- 循环执行 --------
        private IEnumerator RunLoop()
        {
            var wait = new WaitForSeconds(_frameDelta);

            while (true)
            {
                _frameCount++;

                // System 异步执行
                if (_systemBusy)
                {
                   //Debug.LogWarning("[Updater] SystemUpdate 上一帧尚未完成");
                }
                else
                {
                    _systemBusy = true;
                    var helpers = new List<UpdateHelper>(_systemHelpers);
                    var systemStatic = _staticSystemUpdate;

                    _ = Task.Run(() =>
                    {
                        try
                        {
                            foreach (var h in helpers)
                                if (h != null && h.IsActiveForCurrentFrame(_frameCount))
                                    h.InvokeInternal();

                            systemStatic?.Invoke();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                        finally
                        {
                            _systemBusy = false;
                        }
                    });
                }

                // Unity 主线程执行
                foreach (var h in _orderHelpers)
                    if (h != null && h.IsActiveForCurrentFrame(_frameCount))
                        h.InvokeInternal();

                _staticUnityUpdate?.Invoke();

                yield return wait;
            }
        }
    }
}
