using System.Linq;
using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 控制台界面（IMGUI，不依赖任何预设体）：` 或 F1 开关；Enter 执行，↑↓ 历史，Tab 补全，Esc 关闭。
    /// </summary>
    public class HowConsoleView : MonoBehaviour
    {
        private static HowConsoleView _instance;

        public static bool IsOpen => _instance != null && _instance._open;

        private bool _open;
        private string _input = "";
        private int _historyIndex = -1;
        private Vector2 _scroll;
        private bool _focusInput;
        private bool _scrollToBottom;
        private float _prevTimeScale = 1f;

        /// <summary>打开控制台时是否暂停游戏（timeScale = 0）</summary>
        public static bool PauseWhenOpen = false;

        private GUIStyle _lineStyle;
        private GUIStyle _inputStyle;
        private Texture2D _background;

        internal static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("HowConsole");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HowConsoleView>();
        }

        public static void Toggle()
        {
            if (_instance == null) Create();
            _instance.SetOpen(!_instance._open);
        }

        private void OnEnable() => HowConsole.OnLine += OnLine;
        private void OnDisable() => HowConsole.OnLine -= OnLine;
        private void OnLine(HowConsole.Line line) => _scrollToBottom = true;

        private void SetOpen(bool open)
        {
            if (_open == open) return;
            _open = open;
            _focusInput = open;
            _scrollToBottom = open;
            if (!PauseWhenOpen) return;
            if (open)
            {
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else Time.timeScale = _prevTimeScale;
        }

        private void Update()
        {
            if (TogglePressed()) SetOpen(!_open);
        }

        private static bool TogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
                return keyboard.backquoteKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F1);
#else
            return false;
#endif
        }

        private void EnsureStyles()
        {
            if (_lineStyle != null) return;
            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.1f, 0.92f));
            _background.Apply();
            _lineStyle = new GUIStyle(GUI.skin.label) { richText = false, wordWrap = true, fontSize = 13, padding = new RectOffset(4, 4, 1, 1) };
            _inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 14 };
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            float scale = Mathf.Clamp(Screen.height / 720f, 1f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale, height = Screen.height / scale * 0.5f;

            HandleKeys(Event.current);
            if (!_open) return;

            GUI.DrawTexture(new Rect(0, 0, width, height), _background);
            GUILayout.BeginArea(new Rect(6, 6, width - 12, height - 12));

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var line in HowConsole.Lines)
            {
                GUI.contentColor = ColorOf(line.Type);
                GUILayout.Label(line.Text, _lineStyle);
            }
            GUI.contentColor = Color.white;
            GUILayout.EndScrollView();
            if (_scrollToBottom && Event.current.type == EventType.Repaint)
            {
                _scroll.y = float.MaxValue;
                _scrollToBottom = false;
            }

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("HowConsoleInput");
            _input = GUILayout.TextField(_input, _inputStyle, GUILayout.Height(24));
            if (GUILayout.Button("执行", GUILayout.Width(50), GUILayout.Height(24))) Submit();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            if (_focusInput)
            {
                GUI.FocusControl("HowConsoleInput");
                _focusInput = false;
            }
        }

        private void HandleKeys(Event e)
        {
            if (e.type != EventType.KeyDown) return;
            switch (e.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Submit();
                    e.Use();
                    break;
                case KeyCode.Escape:
                case KeyCode.BackQuote:
                case KeyCode.F1:
                    // Update 里也会检测到开关键，这里只吃掉事件，避免 ` 被输入到文本框
                    if (e.keyCode == KeyCode.Escape) SetOpen(false);
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    BrowseHistory(-1);
                    e.Use();
                    break;
                case KeyCode.DownArrow:
                    BrowseHistory(1);
                    e.Use();
                    break;
                case KeyCode.Tab:
                    AutoComplete();
                    e.Use();
                    break;
            }
            if (e.character == '`' || e.character == '\t') e.Use();
        }

        private void Submit()
        {
            string text = _input.Trim();
            _input = "";
            _historyIndex = -1;
            _focusInput = true;
            if (text.Length > 0) HowConsole.Execute(text);
        }

        private void BrowseHistory(int delta)
        {
            var history = HowConsole.History;
            if (history.Count == 0) return;
            if (_historyIndex < 0) _historyIndex = history.Count;
            _historyIndex = Mathf.Clamp(_historyIndex + delta, 0, history.Count);
            _input = _historyIndex < history.Count ? history[_historyIndex] : "";
            MoveCursorToEnd();
        }

        private void AutoComplete()
        {
            string head = _input.TrimStart();
            if (head.Contains(" ")) return;
            var matches = HowConsole.Complete(head).ToList();
            if (matches.Count == 0) return;
            if (matches.Count == 1)
            {
                _input = matches[0] + " ";
            }
            else
            {
                // 补到公共前缀，并列出候选
                string common = matches.Aggregate((a, b) =>
                {
                    int n = 0;
                    while (n < a.Length && n < b.Length && char.ToLowerInvariant(a[n]) == char.ToLowerInvariant(b[n])) n++;
                    return a.Substring(0, n);
                });
                if (common.Length > head.Length) _input = common;
                HowConsole.Print(string.Join("  ", matches));
            }
            MoveCursorToEnd();
        }

        private void MoveCursorToEnd()
        {
            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor == null) return;
            editor.text = _input;
            editor.MoveTextEnd();
        }

        private static Color ColorOf(HowConsole.LineType type) => type switch
        {
            HowConsole.LineType.Input => new Color(0.55f, 0.8f, 1f),
            HowConsole.LineType.Warning => new Color(1f, 0.85f, 0.4f),
            HowConsole.LineType.Error => new Color(1f, 0.45f, 0.45f),
            HowConsole.LineType.Log => new Color(0.75f, 0.75f, 0.75f),
            _ => Color.white,
        };

        private void OnDestroy()
        {
            if (_background != null) Destroy(_background);
            if (_instance == this) _instance = null;
        }
    }
}
