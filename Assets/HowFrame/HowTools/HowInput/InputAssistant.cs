using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem.LowLevel;
using HowEnum;
namespace HowFrame
{
    public static class InputAssistant
    {
        public readonly static Ref<EnumKey<InputEnum.Tag>> InputType = new Ref<EnumKey<InputEnum.Tag>>();
        private static InputActionAsset _asset;
        private static readonly Dictionary<string, InputActionMap> _maps = new();
        private static readonly Dictionary<InputAction, List<Action<InputAction.CallbackContext>>> _callbacks = new();
        private static readonly Dictionary<InputAction, List<Action<InputAction.CallbackContext>>> _canceledCallbacks = new();
        private static bool _initialized = false;
        private static PlayerInput _playerInput;
        private static string _currentScheme = "Keyboard&Mouse";

        /// <summary>
        /// 初始化加载 Asset（延迟初始化，在资源加载完成后调用）
        /// </summary>
        public static void Wake()
        {
            if (_initialized) return; // 防止重复初始化

            InputActionAsset asset = AssetAssistant.AddressableGet<InputActionAsset>("HowInputActions");
            _asset = asset;

            if (asset == null)
            {
                Debug.LogError("[InputAssistant] Asset 加载失败");
                return;
            }

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

            Debug.Log($"[InputAssistant] 已加载 {_maps.Count} 个 ActionMap。");

            _initialized = true;
            InputType.Value = InputEnum.MouseKeyboard;
            // 设备变化监听
            InputSystem.onEvent += OnInputEvent;

            PropertyAssistant.SetObj(GlobalEventEnum.InputTypeChange, InputType);
            //  PropertyAssistant.SetEvent<EnumKey<InputEnum.Tag>>(GlobalEventEnum.InputTypeChange, (inputType) => { });
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

        public static void EnableMap(EnumKeyBase mapKey)
        {
            if (mapKey == null || string.IsNullOrEmpty(mapKey.name))
            {
                Debug.LogWarning($"[InputAssistant] EnableMap失败：EnumKey 为空或 name 为空");
                return;
            }
            EnableMap(mapKey.name);
        }

        public static void DisableMap(string mapName)
        {
            if (!_initialized) return;
            if (_maps.TryGetValue(mapName, out var map))
            {
                map.Disable();
            }
        }

        public static void DisableMap(EnumKeyBase mapKey)
        {
            if (mapKey == null || string.IsNullOrEmpty(mapKey.name))
            {
                Debug.LogWarning($"[InputAssistant] DisableMap失败：EnumKey 为空或 name 为空");
                return;
            }
            DisableMap(mapKey.name);
        }

        public static bool IsMapEnabled(string mapName)
        {
            if (!_maps.TryGetValue(mapName, out var map)) return false;
            return map.enabled;
        }

        public static bool IsMapEnabled(EnumKeyBase mapKey)
        {
            if (mapKey == null || string.IsNullOrEmpty(mapKey.name))
            {
                Debug.LogWarning($"[InputAssistant] IsMapEnabled失败：EnumKey 为空或 name 为空");
                return false;
            }
            return IsMapEnabled(mapKey.name);
        }
        #endregion

        #region Action 绑定
        /// <summary>
        /// 绑定 Action 事件
        /// </summary>
        /// <param name="mapName">ActionMap 名称</param>
        /// <param name="actionName">Action 名称</param>
        /// <param name="callback">performed 事件回调（按下/执行时触发）</param>
        /// <param name="onCanceled">canceled 事件回调（仅 Button 类型 Action 可用，释放/取消时触发）</param>
        public static void BindAction(string mapName, string actionName, Action<InputAction.CallbackContext> callback, Action<InputAction.CallbackContext> onCanceled = null)
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

            // 绑定 performed 事件（避免重复注册）
            if (!_callbacks.ContainsKey(action)) _callbacks[action] = new List<Action<InputAction.CallbackContext>>();
            if (!_callbacks[action].Contains(callback))
            {
                action.performed += callback;
                _callbacks[action].Add(callback);
            }
            else
            {
                Debug.LogWarning($"[InputAssistant] BindAction：回调已存在，跳过重复注册 {mapName}/{actionName}");
            }

            // 如果是 Button 类型且提供了 onCanceled 回调，则绑定 canceled 事件（避免重复注册）
            if (onCanceled != null && action.type == InputActionType.Button)
            {
                if (!_canceledCallbacks.ContainsKey(action)) _canceledCallbacks[action] = new List<Action<InputAction.CallbackContext>>();
                if (!_canceledCallbacks[action].Contains(onCanceled))
                {
                    action.canceled += onCanceled;
                    _canceledCallbacks[action].Add(onCanceled);
                }
                else
                {
                    Debug.LogWarning($"[InputAssistant] BindAction：canceled 回调已存在，跳过重复注册 {mapName}/{actionName}");
                }
            }
            else if (onCanceled != null && action.type != InputActionType.Button)
            {
                Debug.LogWarning($"[InputAssistant] BindAction：Action {actionName} 不是 Button 类型，onCanceled 回调将被忽略");
            }
        }

        /// <summary>
        /// 解绑 Action 事件
        /// </summary>
        /// <param name="mapName">ActionMap 名称</param>
        /// <param name="actionName">Action 名称</param>
        /// <param name="callback">performed 事件回调</param>
        /// <param name="onCanceled">canceled 事件回调（如果之前绑定了的话）</param>
        public static void UnbindAction(string mapName, string actionName, Action<InputAction.CallbackContext> callback, Action<InputAction.CallbackContext> onCanceled = null)
        {
            if (!_maps.TryGetValue(mapName, out var map)) return;
            var action = map.FindAction(actionName);
            if (action == null) return;

            // 解绑 performed 事件
            action.performed -= callback;
            if (_callbacks.TryGetValue(action, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0) _callbacks.Remove(action);
            }

            // 解绑 canceled 事件（如果提供了）
            if (onCanceled != null)
            {
                action.canceled -= onCanceled;
                if (_canceledCallbacks.TryGetValue(action, out var canceledList))
                {
                    canceledList.Remove(onCanceled);
                    if (canceledList.Count == 0) _canceledCallbacks.Remove(action);
                }
            }
        }

        /// <summary>
        /// 使用 EnumKey 绑定 Action 事件
        /// </summary>
        /// <param name="mapKey">ActionMap 的 EnumKey</param>
        /// <param name="actionKey">Action 的 EnumKey</param>
        /// <param name="callback">performed 事件回调（按下/执行时触发）</param>
        /// <param name="onCanceled">canceled 事件回调（仅 Button 类型 Action 可用，释放/取消时触发）</param>
        public static void BindAction(EnumKeyBase mapKey, EnumKeyBase actionKey, Action<InputAction.CallbackContext> callback, Action<InputAction.CallbackContext> onCanceled = null)
        {
            if (mapKey == null || string.IsNullOrEmpty(mapKey.name))
            {
                Debug.LogWarning($"[InputAssistant] BindAction失败：Map EnumKey 为空或 name 为空");
                return;
            }
            if (actionKey == null || string.IsNullOrEmpty(actionKey.name))
            {
                Debug.LogWarning($"[InputAssistant] BindAction失败：Action EnumKey 为空或 name 为空");
                return;
            }
            BindAction(mapKey.name, actionKey.name, callback, onCanceled);
        }

        /// <summary>
        /// 使用 EnumKey 解绑 Action 事件
        /// </summary>
        /// <param name="mapKey">ActionMap 的 EnumKey</param>
        /// <param name="actionKey">Action 的 EnumKey</param>
        /// <param name="callback">performed 事件回调</param>
        /// <param name="onCanceled">canceled 事件回调（如果之前绑定了的话）</param>
        public static void UnbindAction(EnumKeyBase mapKey, EnumKeyBase actionKey, Action<InputAction.CallbackContext> callback, Action<InputAction.CallbackContext> onCanceled = null)
        {
            if (mapKey == null || string.IsNullOrEmpty(mapKey.name))
            {
                Debug.LogWarning($"[InputAssistant] UnbindAction失败：Map EnumKey 为空或 name 为空");
                return;
            }
            if (actionKey == null || string.IsNullOrEmpty(actionKey.name))
            {
                Debug.LogWarning($"[InputAssistant] UnbindAction失败：Action EnumKey 为空或 name 为空");
                return;
            }
            UnbindAction(mapKey.name, actionKey.name, callback, onCanceled);
        }

        public static T ReadValue<T>(string mapName, string actionName) where T : struct
        {
            if (!_maps.TryGetValue(mapName, out var map)) return default;
            var action = map.FindAction(actionName);
            if (action == null) return default;
            return action.ReadValue<T>();
        }

        /// <summary>
        /// 使用 EnumKey 读取 Action 值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="mapKey">ActionMap 的 EnumKey</param>
        /// <param name="actionKey">Action 的 EnumKey</param>
        /// <returns>Action 的值</returns>
        public static T ReadValue<T>(EnumKeyBase mapKey, EnumKeyBase actionKey) where T : struct
        {
            if (mapKey == null || string.IsNullOrEmpty(mapKey.name))
            {
                Debug.LogWarning($"[InputAssistant] ReadValue失败：Map EnumKey 为空或 name 为空");
                return default;
            }
            if (actionKey == null || string.IsNullOrEmpty(actionKey.name))
            {
                Debug.LogWarning($"[InputAssistant] ReadValue失败：Action EnumKey 为空或 name 为空");
                return default;
            }
            return ReadValue<T>(mapKey.name, actionKey.name);
        }
        #endregion

        #region ControlScheme / 设备切换
        private static void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!_initialized || device == null || !eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;
            // 忽略无效输入帧
            if (!eventPtr.valid) return;

            // 检测设备类型
            if (device is Gamepad)
            {
                if (_currentScheme != "Gamepad")
                {
                    _currentScheme = "Gamepad";
                  ApplyControlScheme("Gamepad");  
                    Debug.Log("[InputAssistant] 检测到手柄输入，切换到 Gamepad Scheme");
                    InputType.Value = InputEnum.GamePad;
                }
            }
            else if (device is Keyboard || device is Mouse)
            {
                if (_currentScheme != "Keyboard&Mouse")
                {
                    ApplyControlScheme("PC");  

                    _currentScheme = "Keyboard&Mouse";
                    Debug.Log("[InputAssistant] 检测到键鼠输入，切换到 Keyboard&Mouse Scheme");
                    InputType.Value = InputEnum.MouseKeyboard;
                }
            }
            else if (device is Touchscreen)
            {
                if (_currentScheme != "Touch")
                {
                    _currentScheme = "Touch";
                    Debug.Log("[InputAssistant] 检测到触摸输入，切换到 Touch Scheme");
                }
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

        

        
        
        
        
        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorPlayModeMonitor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    InputSystem.onEvent -= OnInputEvent;
                    _playerInput = null;
                    _initialized = false;
                    Debug.Log("[InputAssistant] 清理完毕，防止退出PlayMode时异常。");
                }
            };
        }
        #endif
    }
}
