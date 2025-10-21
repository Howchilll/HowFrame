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
        public readonly static  Ref<EnumKey<InputEnum.Tag>> InputType=new Ref<EnumKey<InputEnum.Tag>>();
        private static InputActionAsset _asset;
        private static readonly Dictionary<string, InputActionMap> _maps = new();
        private static readonly Dictionary<InputAction, List<Action<InputAction.CallbackContext>>> _callbacks = new();
        private static bool _initialized = false;
        private static PlayerInput _playerInput;
        private static string _currentScheme = "Keyboard&Mouse";
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
                InputType.Value=InputEnum.MouseKeyboard;
                // 设备变化监听
                InputSystem.onEvent += OnInputEvent;

                PropertyAssistant.SetObj(GlobalEventEnum.InputTypeChange, InputType);
              //  PropertyAssistant.SetEvent<EnumKey<InputEnum.Tag>>(GlobalEventEnum.InputTypeChange, (inputType) => { });
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


        
        #region Rebinding（按键重绑定）


    private static InputActionRebindingExtensions.RebindingOperation _rebindingOperation;


    public static void StartRebind(string mapName, string actionName, Action onComplete = null, Action onCancel = null)
    {
        if (!_maps.TryGetValue(mapName, out var map))
        {
            Debug.LogWarning($"[InputAssistant] StartRebind失败：Map不存在 {mapName}");
            return;
        }

        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"[InputAssistant] StartRebind失败：Action不存在 {actionName}");
            return;
        }

        if (_rebindingOperation != null)
        {
            Debug.LogWarning("[InputAssistant] 已有Rebind操作正在进行。");
            return;
        }

        Debug.Log($"[InputAssistant] 开始重绑定 {mapName}/{actionName}，等待玩家输入...");

        action.Disable(); // 暂时禁用，防止旧绑定触发

        _rebindingOperation = action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse/position")
            .WithControlsExcluding("Mouse/delta")
            .OnMatchWaitForAnother(0.1f) // 等待更稳定的输入
            .OnComplete(op =>
            {
                action.Enable();
                op.Dispose();
                _rebindingOperation = null;
                onComplete?.Invoke();
                Debug.Log($"[InputAssistant] 已重新绑定 {actionName} 为 {action.bindings[action.bindings.Count - 1].effectivePath}");
            })
            .OnCancel(op =>
            {
                action.Enable();
                op.Dispose();
                _rebindingOperation = null;
                onCancel?.Invoke();
                Debug.Log("[InputAssistant] 重绑定取消。");
            });

        _rebindingOperation.Start();
    }


    public static void RebindToKey(string mapName, string actionName, string newBindingPath)
    {
        if (!_maps.TryGetValue(mapName, out var map))
        {
            Debug.LogWarning($"[InputAssistant] RebindToKey失败：Map不存在 {mapName}");
            return;
        }

        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"[InputAssistant] RebindToKey失败：Action不存在 {actionName}");
            return;
        }

        // 清除旧绑定并应用新键
        action.ApplyBindingOverride(newBindingPath);
        Debug.Log($"[InputAssistant] {actionName} 已绑定到 {newBindingPath}");
    }


    public static void CancelRebind()
    {
         if (_rebindingOperation != null)
        {
            _rebindingOperation.Cancel();
            _rebindingOperation.Dispose();
            _rebindingOperation = null;
            Debug.Log("[InputAssistant] 手动取消Rebind。");
        }
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
        public static void Wake(){}
    }
    
}
