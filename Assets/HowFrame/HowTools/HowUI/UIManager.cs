using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HowEnum;
using static HowFrame.AssetAssistant;

namespace HowFrame
{
    public static class UIManager
    {
        private static Canvas Canvas;
        private static readonly Dictionary<string, PanelBase> UIObjects = new Dictionary<string, PanelBase>();
        private static bool _initialized = false;

        #region Show UI

        public static void Show(bool father = false, params string[] UINames)
        {
            Transform fatherTransform = Canvas.transform;

            if (father && UIObjects.ContainsKey(UINames[0]))
                fatherTransform = UIObjects[UINames[0]].transform;

            foreach (var name in UINames)
            {
                if (!UIObjects.ContainsKey(name))
                {
                    if (father && fatherTransform == UIObjects[UINames[0]].transform)
                    {
                        // 跳过父级的 Show
                        father = false;
                        continue;
                    }

                    GameObject uiPrefab = AddressableGet<GameObject>(name);
                    if (uiPrefab == null)
                    {
                        Debug.LogError($"UI 预设体 {name} 加载失败");
                        continue;
                    }

                    var uiObj = Object.Instantiate(uiPrefab, fatherTransform);
                    var panel = uiObj.GetComponent<PanelBase>();
                    if (panel == null)
                    {
                        var panelType = TypeAssistant.GetType(name);
                        if (panelType != null)
                        {
                            panel = uiObj.AddComponent(panelType) as PanelBase;
                        }
                    }
                    if (panel == null)
                    {
                        Debug.LogError($"UI 预设体 {name} 上没有 PanelBase 组件，且无法通过TypeAssistant创建");
                        Object.Destroy(uiObj);
                        continue;
                    }
                    panel.Init();

                    UIObjects[name] = panel;
                }
                else
                {
                    UIObjects[name].gameObject.SetActive(true);
                }

                UIObjects[name].WhenShow();
            }
        }

        public static void Show(params string[] UINames)
        {
            Show(false, UINames);
        }

        public static void Show(string UIName, object parameter)
        {
            Show(UIName);
            UIObjects[UIName].WhenShowWithParameter(parameter);
        }
        public static void Show(EnumKeyBase UIName, object parameter)
        {
            Show(UIName);
            UIObjects[UIName.name].WhenShowWithParameter(parameter);
        }
        public static void Show(bool father = false, params EnumKeyBase[] UINames)
        {
            Show(father, UINames.Select(k => k.name).ToArray());
        }

        public static void Show(params EnumKeyBase[] UINames)
        {
            Show(false, UINames.Select(k => k.name).ToArray());
        }

        #endregion

        #region Hide UI

        public static void Hide(bool destroy = false, params string[] UINames)
        {
            foreach (var name in UINames)
            {
                if (UIObjects.ContainsKey(name))
                {
                    UIObjects[name].WhenHide();
                    if (destroy)
                    {
                        Object.Destroy(UIObjects[name].gameObject);
                        UIObjects.Remove(name);
                    }
                    else
                    {
                        UIObjects[name].gameObject.SetActive(false);
                    }
                }
            }
        }

        public static void Hide(params string[] UINames)
        {
            Hide(false, UINames);
        }

        public static void Hide(bool destroy = false, params EnumKeyBase[] UINames)
        {
            Hide(destroy, UINames.Select(k => k.name).ToArray());
        }

        public static void Hide(params EnumKeyBase[] UINames)
        {
            Hide(false, UINames.Select(k => k.name).ToArray());
        }

        #endregion

        #region Check UI

        /// <summary>
        /// 检查指定UI面板是否处于显示状态
        /// </summary>
        public static bool Check(string UIName)
        {
            if (UIObjects.TryGetValue(UIName, out var panel))
            {
                return panel.gameObject.activeSelf;
            }
            return false;
        }

        /// <summary>
        /// 检查指定UI面板是否处于显示状态
        /// </summary>
        public static bool Check(EnumKeyBase UIName)
        {
            return Check(UIName.name);
        }

        #endregion

        #region HideAll

        /// <summary>
        /// 隐藏所有已加载的UI面板
        /// </summary>
        public static void HideAll(bool destroy = false)
        {
            foreach (var pair in UIObjects)
            {
                if (!pair.Value.gameObject.activeSelf)
                {
                    continue;
                }

                pair.Value.WhenHide();
                if (destroy)
                {
                    Object.Destroy(pair.Value.gameObject);
                }
                else
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }

            if (destroy)
            {
                UIObjects.Clear();
            }
        }

        #endregion

        #region Hide PanelBase

        public static void Hide(PanelBase panel, bool destroy = false)
        {
            if (panel == null) return;

            string targetKey = null;

            // 直接通过引用查找对应 key
            foreach (var pair in UIObjects)
            {
                if (pair.Value == panel)
                {
                    targetKey = pair.Key;
                    break;
                }
            }

            if (targetKey == null)
            {
                Debug.LogWarning($"UIManager: 未在管理列表中找到该面板引用 {panel.GetType().Name}");
                return;
            }

            panel.WhenHide();

            if (destroy)
            {
                Object.Destroy(panel.gameObject);
                UIObjects.Remove(targetKey);
            }
            else
            {
                panel.gameObject.SetActive(false);
            }
        }


        #endregion

   
        /// <summary>
        /// 初始化 UIManager（延迟初始化，在资源加载完成后调用）
        /// </summary>
        public static void Wake()
        {
            if (_initialized) return; // 防止重复初始化

            GameObject canvasObj = AddressableGet<GameObject>("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("UIManager: Canvas 加载失败!");
                return;
            }

            canvasObj = Object.Instantiate(canvasObj);
            Canvas = canvasObj.GetComponent<Canvas>();
            if (Canvas == null)
            {
                Debug.LogError("UIManager: Canvas 组件不存在!");
                return;
            }

            Camera oldCamera = Canvas.worldCamera;
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.worldCamera = null;
            if (oldCamera != null) Object.Destroy(oldCamera.gameObject);

            Object.DontDestroyOnLoad(Canvas.gameObject);
            _initialized = true;
        }
    }
}
