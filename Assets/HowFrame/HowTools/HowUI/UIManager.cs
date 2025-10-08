using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static HowFrame.AssetAssistant;

namespace HowFrame
{
    public static class UIManager
    {
        private static Canvas Canvas;
        private static readonly Dictionary<string, PanelBase> UIObjects = new Dictionary<string, PanelBase>();

        static UIManager()
        {
            UniTask.Void(async () =>
            {
                GameObject canvasObj = await AddressAsset<GameObject>("Canvas");
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
            });
        }

        #region Show UI

        public static void Show(bool father = false, params string[] UINames)
        {
            UniTask.Void(async () =>
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

                        GameObject uiPrefab = await AddressAsset<GameObject>(name);
                        if (uiPrefab == null)
                        {
                            Debug.LogError($"UI 预设体 {name} 加载失败");
                            continue;
                        }

                        var uiObj = Object.Instantiate(uiPrefab, fatherTransform);
                        var panel = uiObj.GetComponent<PanelBase>();
                        if (panel == null)
                        {
                            Debug.LogError($"UI 预设体 {name} 上没有 PanelBase 组件");
                            Object.Destroy(uiObj);
                            continue;
                        }

                        UIObjects[name] = panel;
                    }
                    else
                    {
                        UIObjects[name].gameObject.SetActive(true);
                    }

                    UIObjects[name].WhenShow();
                }
            });
        }

        public static void Show(params string[] UINames)
        {
            Show(false, UINames);
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

        #endregion

        public static void wake() { }
    }
}
