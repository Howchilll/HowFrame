using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace HowFrame
{
    public static class InputAssistant
    {
        private static InputActionAsset _asset;
        private static readonly Dictionary<string, InputActionMap> _maps = new();
        private static readonly Dictionary<InputAction, List<Action<InputAction.CallbackContext>>> _callbacks = new();
        private static bool _initialized = false;
        private static PlayerInput _playerInput;
        /// <summary>
        /// 初始化异步加载 Asset
        /// </summary>
        static InputAssistant()
        {
            UniTask.Void(async () =>
            {
                InputActionAsset asset = await AssetAssistant.AddressAsset<InputActionAsset>("HowInputActions");



                _asset = asset;
                
                _maps.Clear();
                foreach (var map in _asset.actionMaps)
                {
                    _maps[map.name] = map;
                }
                var go = new GameObject();
                go.hideFlags = HideFlags.HideAndDontSave;
                _playerInput = go.AddComponent<PlayerInput>();
                _playerInput.actions = _asset;
                _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                UnityEngine.Object.DontDestroyOnLoad(go); 
                
                if (asset == null)
                {
                    Debug.LogError("[InputAssistant] Asset 加载失败");
                    return;
                }

                Debug.Log($"[InputAssistant] 已加载 {_maps.Count} 个 ActionMap。");

                _initialized = true;

                // 设备变化监听
                InputSystem.onDeviceChange += OnDeviceChange;
            });
        }

        #region Map管理
        public static void EnableMap(string mapName)
        {
            if (!_initialized) return;
            if (_maps.TryGetValue(mapName, out var map))
            {
                map.Enable();
            }
            else
            {
                Debug.LogWarning($"[InputAssistant] 启用失败：没有找到名为 {mapName} 的 ActionMap。");
            }
        }

        public static void DisableMap(string mapName)
        {
            if (!_initialized) return;
            if (_maps.TryGetValue(mapName, out var map))
            {
                map.Disable();
            }
        }

        public static bool IsMapEnabled(string mapName)
        {
            if (!_maps.TryGetValue(mapName, out var map)) return false;
            return map.enabled;
        }
        #endregion

        #region Action 绑定
        public static void BindAction(string mapName, string actionName, Action<InputAction.CallbackContext> callback)
        {
            if (!_maps.TryGetValue(mapName, out var map))
            {
                Debug.LogWarning($"[InputAssistant] BindAction失败：Map不存在 {mapName}");
                return;
            }

            var action = map.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[InputAssistant] BindAction失败：Action不存在 {actionName}");
                return;
            }

            action.performed += callback;
            if (!_callbacks.ContainsKey(action)) _callbacks[action] = new List<Action<InputAction.CallbackContext>>();
            _callbacks[action].Add(callback);
        }

        public static void UnbindAction(string mapName, string actionName, Action<InputAction.CallbackContext> callback)
        {
            if (!_maps.TryGetValue(mapName, out var map)) return;
            var action = map.FindAction(actionName);
            if (action == null) return;

            action.performed -= callback;
            if (_callbacks.TryGetValue(action, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0) _callbacks.Remove(action);
            }
        }

        public static T ReadValue<T>(string mapName, string actionName) where T : struct
        {
            if (!_maps.TryGetValue(mapName, out var map)) return default;
            var action = map.FindAction(actionName);
            if (action == null) return default;
            return action.ReadValue<T>();
        }
        #endregion

        #region ControlScheme / 设备切换
        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!_initialized) return;

            switch (change)
            {
                case InputDeviceChange.Added:
                    if (device is Gamepad)
                    {
                        Debug.Log("[InputAssistant] 检测到手柄接入，可切换到 Gamepad Scheme");
                        // TODO: 根据你的方案切换 Control Scheme
                    }
                    break;

                case InputDeviceChange.Removed:
                    if (device is Gamepad)
                    {
                        Debug.Log("[InputAssistant] 手柄拔出，可切回 Keyboard&Mouse Scheme");
                        // TODO: 根据你的方案切换 Control Scheme
                    }
                    break;
            }
        }

        // 可选：静态辅助方法手动切 ControlScheme，需要 PlayerInput 支持



        public static void ApplyControlScheme(string schemeName)
        {
            if (_playerInput == null)
            {
                Debug.LogWarning("[InputAssistant] PlayerInput 未设置，无法切换 Scheme");
                return;
            }

            _playerInput.SwitchCurrentControlScheme(schemeName, _playerInput.devices.ToArray());
            Debug.Log($"[InputAssistant] 已切换 Control Scheme: {schemeName}");
        }
        #endregion

        public static void Wake(){}
        
    }
    
}
