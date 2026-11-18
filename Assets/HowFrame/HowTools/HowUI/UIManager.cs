using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using HowEnum;
using static HowFrame.AssetAssistant;

namespace HowFrame
{
    public static class UIManager
    {
        private static Transform _uiRoot;
        private static GameObject _canvasPrefab;
        private static readonly Dictionary<short, Canvas> Canvases = new Dictionary<short, Canvas>();
        private static readonly Dictionary<string, PanelBase> UIObjects = new Dictionary<string, PanelBase>();
        private static bool _initialized;

        #region Show UI

        /// <summary>层级越大越晚渲染。同名面板复用并移动到指定层。</summary>
        public static void Show(string UIName, short uiLevel = 0)
        {
            ShowInternal(false, uiLevel, new[] { UIName });
        }

        public static void Show(params string[] UINames)
        {
            ShowInternal(false, 0, UINames);
        }

        public static void Show(short uiLevel, params string[] UINames)
        {
            ShowInternal(false, uiLevel, UINames);
        }

        public static void Show(bool father = false, params string[] UINames)
        {
            ShowInternal(father, 0, UINames);
        }

        /// <summary>father 为 true 时，第一个名称表示已加载的父面板，其余面板挂到它下面。</summary>
        public static void Show(bool father, short uiLevel, params string[] UINames)
        {
            ShowInternal(father, uiLevel, UINames);
        }

        public static void Show(string UIName, object parameter, short uiLevel = 0)
        {
            Show(UIName, uiLevel);
            if (UIObjects.TryGetValue(UIName, out var panel) && panel != null)
                panel.WhenShowWithParameter(parameter);
        }

        public static void Show(EnumKeyBase UIName, short uiLevel = 0)
        {
            Show(UIName.name, uiLevel);
        }

        public static void Show(EnumKeyBase UIName, object parameter, short uiLevel = 0)
        {
            Show(UIName.name, parameter, uiLevel);
        }

        public static void Show(bool father = false, params EnumKeyBase[] UINames)
        {
            ShowInternal(father, 0, UINames.Select(k => k.name).ToArray());
        }

        public static void Show(bool father, short uiLevel, params EnumKeyBase[] UINames)
        {
            ShowInternal(father, uiLevel, UINames.Select(k => k.name).ToArray());
        }

        public static void Show(params EnumKeyBase[] UINames)
        {
            ShowInternal(false, 0, UINames.Select(k => k.name).ToArray());
        }

        public static void Show(short uiLevel, params EnumKeyBase[] UINames)
        {
            ShowInternal(false, uiLevel, UINames.Select(k => k.name).ToArray());
        }

        private static void ShowInternal(bool father, short uiLevel, string[] names)
        {
            if (names == null || names.Length == 0) return;
            Wake();
            if (!_initialized) return;

            var canvas = GetOrCreateCanvas(uiLevel);
            Transform parent = canvas.transform;
            int firstPanel = 0;
            if (father && UIObjects.TryGetValue(names[0], out var fatherPanel) && fatherPanel != null)
            {
                // 移动整个父面板树，让嵌套面板保持在同一个层级。
                Transform rootPanel = fatherPanel.transform;
                while (rootPanel.parent != null && rootPanel.parent.GetComponent<Canvas>() == null)
                    rootPanel = rootPanel.parent;
                if (rootPanel.parent != canvas.transform)
                    rootPanel.SetParent(canvas.transform, false);
                parent = fatherPanel.transform;
                firstPanel = 1;
            }

            for (int i = firstPanel; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name)) continue;

                if (!UIObjects.TryGetValue(name, out var panel) || panel == null)
                {
                    GameObject prefab = AddressableGet<GameObject>(name);
                    if (prefab == null)
                    {
                        Debug.LogError($"UI 预设体 {name} 加载失败");
                        continue;
                    }

                    var instance = Object.Instantiate(prefab, parent);
                    panel = instance.GetComponent<PanelBase>();
                    if (panel == null)
                    {
                        var panelType = TypeAssistant.GetType(name);
                        if (panelType != null && typeof(PanelBase).IsAssignableFrom(panelType))
                            panel = instance.AddComponent(panelType) as PanelBase;
                    }
                    if (panel == null)
                    {
                        Debug.LogError($"UI 预设体 {name} 上没有 PanelBase 组件，且无法通过 TypeAssistant 创建");
                        Object.Destroy(instance);
                        continue;
                    }

                    UIObjects[name] = panel;
                    panel.Init();
                    NotifyLanguageChanged(panel);
                }
                else if (panel.transform.parent != parent)
                {
                    if (parent.IsChildOf(panel.transform))
                    {
                        Debug.LogError($"UIManager: 无法将 {name} 挂到它自己的子物体下");
                        continue;
                    }
                    panel.transform.SetParent(parent, false);
                }

                panel.gameObject.SetActive(true);
                panel.WhenShow();
            }
        }

        #endregion

        #region Hide UI

        public static void Hide(bool destroy = false, params string[] UINames)
        {
            HidePanels(UINames.Where(name => UIObjects.ContainsKey(name))
                .Select(name => UIObjects[name]).ToArray(), destroy);
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

        /// <summary>关闭指定 Canvas 下的所有面板，包含嵌套面板；保留 Canvas 供后续复用。</summary>
        public static void HideLevel(short uiLevel, bool destroy = false)
        {
            if (!Canvases.TryGetValue(uiLevel, out var canvas) || canvas == null) return;
            HidePanels(UIObjects.Values.Where(panel => panel != null &&
                panel.transform.IsChildOf(canvas.transform)).ToArray(), destroy);
        }

        /// <summary>关闭所有面板，同时清空导航栈。</summary>
        public static void HideAll(bool destroy = false)
        {
            NavStack.Clear();
            HidePanels(UIObjects.Values.ToArray(), destroy);
        }

        public static void Hide(PanelBase panel, bool destroy = false)
        {
            if (panel == null) return;
            if (!UIObjects.ContainsValue(panel))
            {
                Debug.LogWarning($"UIManager: 未在管理列表中找到该面板引用 {panel.GetType().Name}");
                return;
            }
            HidePanels(new[] { panel }, destroy);
        }

        private static void HidePanels(PanelBase[] panels, bool destroy)
        {
            var targets = new HashSet<PanelBase>(panels.Where(panel => panel != null));
            if (destroy)
            {
                // 父面板销毁时，同时清理跟随销毁的子面板缓存。
                foreach (var panel in UIObjects.Values.ToArray())
                {
                    if (panel != null && panels.Any(parent => parent != null &&
                        panel.transform.IsChildOf(parent.transform)))
                        targets.Add(panel);
                }
            }

            // 回调可能修改管理列表，因此始终遍历快照。
            foreach (var panel in targets)
            {
                if (panel != null && panel.gameObject.activeSelf)
                    panel.WhenHide();
            }

            foreach (var panel in targets)
            {
                if (panel == null) continue;
                if (destroy)
                {
                    foreach (var key in UIObjects.Where(pair => pair.Value == panel).Select(pair => pair.Key).ToArray())
                    {
                        UIObjects.Remove(key);
                        NavStack.RemoveAll(entry => entry.Name == key); // 已销毁的面板不能再被 Back 回来
                    }
                    Object.Destroy(panel.gameObject);
                }
                else
                {
                    panel.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Navigation Stack

        private readonly struct NavEntry
        {
            public readonly string Name;
            public readonly short Level;

            public NavEntry(string name, short level)
            {
                Name = name;
                Level = level;
            }
        }

        // 导航栈：Push 打开的面板按顺序入栈，Back 关闭栈顶并重新显示上一个。
        // 与 Show/Hide 互不干扰：直接 Show 的面板不入栈。
        private static readonly List<NavEntry> NavStack = new List<NavEntry>();

        /// <summary>导航栈深度</summary>
        public static int StackCount => NavStack.Count;

        /// <summary>导航栈顶的面板名，栈空时为 null</summary>
        public static string StackTop => NavStack.Count > 0 ? NavStack[NavStack.Count - 1].Name : null;

        /// <summary>
        /// 打开面板并压入导航栈。hidePrevious 为 true 时隐藏当前栈顶（Back 时会重新显示）。
        /// 面板已在栈中时会被移到栈顶。
        /// </summary>
        public static void Push(string UIName, object parameter = null, short uiLevel = 0, bool hidePrevious = true)
        {
            if (string.IsNullOrEmpty(UIName)) return;

            if (hidePrevious && NavStack.Count > 0 && StackTop != UIName)
                Hide(false, StackTop);

            NavStack.RemoveAll(entry => entry.Name == UIName);
            NavStack.Add(new NavEntry(UIName, uiLevel));

            if (parameter != null) Show(UIName, parameter, uiLevel);
            else Show(UIName, uiLevel);
        }

        public static void Push(EnumKeyBase UIName, object parameter = null, short uiLevel = 0, bool hidePrevious = true)
        {
            Push(UIName.name, parameter, uiLevel, hidePrevious);
        }

        /// <summary>
        /// 关闭栈顶面板并重新显示上一个。栈为空时返回 false（可据此决定是否弹出"退出游戏"等）。
        /// </summary>
        public static bool Back(bool destroy = false)
        {
            if (NavStack.Count == 0) return false;

            var top = NavStack[NavStack.Count - 1];
            NavStack.RemoveAt(NavStack.Count - 1);
            Hide(destroy, top.Name);

            if (NavStack.Count > 0)
            {
                var previous = NavStack[NavStack.Count - 1];
                if (!Check(previous.Name))
                    ShowInternal(false, previous.Level, new[] { previous.Name });
            }
            return true;
        }

        /// <summary>连续 Back 直到指定面板位于栈顶；栈中没有该面板时不做任何事并返回 false。</summary>
        public static bool BackTo(string UIName, bool destroy = false)
        {
            if (NavStack.All(entry => entry.Name != UIName)) return false;
            while (StackTop != UIName && Back(destroy)) { }
            return true;
        }

        public static bool BackTo(EnumKeyBase UIName, bool destroy = false)
        {
            return BackTo(UIName.name, destroy);
        }

        /// <summary>清空导航栈（不关闭面板）</summary>
        public static void ClearStack()
        {
            NavStack.Clear();
        }

        #endregion

        #region Language

        private static void RefreshLanguage()
        {
            foreach (var panel in UIObjects.Values.ToArray())
                NotifyLanguageChanged(panel);
        }

        private static void NotifyLanguageChanged(PanelBase panel)
        {
            if (panel == null || LangManager.CurrentLanguage.Value == null) return;
            try
            {
                panel.WhenLanguageChanged();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UIManager: {panel.GetType().Name}.WhenLanguageChanged 异常: {e}");
            }
        }

        #endregion

        #region Check UI

        public static bool Check(string UIName)
        {
            return UIObjects.TryGetValue(UIName, out var panel) && panel != null &&
                panel.gameObject.activeInHierarchy;
        }

        public static bool Check(EnumKeyBase UIName)
        {
            return Check(UIName.name);
        }

        #endregion

        /// <summary>创建持久化 UIRoot、全局唯一的 EventSystem 和默认 0 层 Canvas；其余层在 Show 时创建。</summary>
        public static void Wake()
        {
            if (_initialized && _uiRoot != null) return;
            _initialized = false;

            _canvasPrefab = AddressableGet<GameObject>("Canvas");
            if (_canvasPrefab == null || _canvasPrefab.GetComponent<Canvas>() == null)
            {
                Debug.LogError("UIManager: Canvas 预制体加载失败或缺少 Canvas 组件!");
                return;
            }

            Canvases.Clear();
            UIObjects.Clear();
            NavStack.Clear();
            LangManager.OnLanguageChanged -= RefreshLanguage; // 重复 Wake 时避免重复订阅
            LangManager.OnLanguageChanged += RefreshLanguage;
            _uiRoot = new GameObject("UIRoot").transform;
            Object.DontDestroyOnLoad(_uiRoot.gameObject);
            CreateEventSystem();
            GetOrCreateCanvas(0);
            _initialized = true;
        }

        private static Canvas GetOrCreateCanvas(short uiLevel)
        {
            if (Canvases.TryGetValue(uiLevel, out var canvas) && canvas != null)
                return canvas;

            var instance = Object.Instantiate(_canvasPrefab, _uiRoot, false);
            instance.name = $"Canvas_{uiLevel}";
            canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.sortingLayerID = 0;
            canvas.sortingOrder = uiLevel;
            instance.transform.localScale = Vector3.one;

            // 兼容自带 EventSystem 的旧 Canvas 预制体：全局那一个已由 CreateEventSystem 建在 UIRoot 下，
            // 每层 Canvas 里跟着实例化出来的都去掉，避免出现多个。
            foreach (var eventSystem in instance.GetComponentsInChildren<EventSystem>(true))
            {
                if (eventSystem.gameObject == instance) continue;
                eventSystem.gameObject.SetActive(false);
                Object.Destroy(eventSystem.gameObject);
            }

            // Overlay Canvas 不需要预制体自带的 UI 相机。
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
                Object.Destroy(camera.gameObject);
            }

            Canvases[uiLevel] = canvas;
            return canvas;
        }

        /// <summary>
        /// EventSystem 全局只建一个，放在 UIRoot 下（每层 Canvas 都实例化一次预制体，跟着 Canvas 走会出现多个）。
        /// 场景里已经有 EventSystem 时不再创建；否则依次使用：
        /// Canvas 预制体里自带的 EventSystem（兼容旧预制体，保留它的输入模块设置）、
        /// Addressables 里单独的 EventSystem 预制体（Instance 标签）、默认设置。
        /// </summary>
        private static void CreateEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            GameObject prefab = null;
            var embedded = _canvasPrefab.GetComponentInChildren<EventSystem>(true);
            if (embedded != null && embedded.gameObject != _canvasPrefab)
                prefab = embedded.gameObject;
            if (prefab == null)
                prefab = AddressableGet<GameObject>("EventSystem");

            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, _uiRoot, false);
            }
            else
            {
                Debug.LogWarning("UIManager: 没有找到 EventSystem 预制体，改用默认设置创建");
                instance = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                instance.transform.SetParent(_uiRoot, false);
            }
            instance.name = "EventSystem";
        }
    }
}
