using System;
using UnityEngine;
namespace HowFrame
{

public static class MonoAssistant
{

        private static MonoHelper _instance;
        private static bool _initialized = false;
        private static MonoHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("MonoAssistant_Helper");
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<MonoHelper>();
                }
                return _instance;
            }
        }

        // 内部存储事件，避免重复添加
        private static event Action _onUpdate;
        private static event Action _onFixedUpdate;
        private static event Action _onLateUpdate;

        #region 添加/移除回调

        public static void AddUpdate(Action callback)
        {
            if (callback == null) return;
            _onUpdate -= callback; // 避免重复添加
            _onUpdate += callback;
        }

        public static void RemoveUpdate(Action callback)
        {
            if (callback == null) return;
            _onUpdate -= callback;
        }

        public static void AddFixedUpdate(Action callback)
        {
            if (callback == null) return;
            _onFixedUpdate -= callback;
            _onFixedUpdate += callback;
        }

        public static void RemoveFixedUpdate(Action callback)
        {
            if (callback == null) return;
            _onFixedUpdate -= callback;
        }

        public static void AddLateUpdate(Action callback)
        {
            if (callback == null) return;
            _onLateUpdate -= callback;
            _onLateUpdate += callback;
        }

        public static void RemoveLateUpdate(Action callback)
        {
            if (callback == null) return;
            _onLateUpdate -= callback;
        }

        #endregion

        private class MonoHelper : MonoBehaviour
        {
            public void Init() { }

            private void Update() => _onUpdate?.Invoke();
            private void FixedUpdate() => _onFixedUpdate?.Invoke();
            private void LateUpdate() => _onLateUpdate?.Invoke();
        }

        /// <summary>
        /// 初始化 MonoAssistant（延迟初始化，在资源加载完成后调用）
        /// </summary>
        internal static void Wake()
        {
            if (_initialized) return; // 防止重复初始化
            Instance.Init();
            _initialized = true;
        }
    }
}