using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 从启动场景运行：HowFrame 依赖启动场景里的 GameInit 完成初始化，直接在其它场景点 Play 会缺资源 / UI 起不来。
/// - 菜单 Tools/HowFrame/从启动场景运行（开关，按用户保存在 EditorPrefs）：开启后点 Play 总是先进入启动场景；
/// - 菜单 Tools/HowFrame/运行启动场景（Ctrl+Alt+P）：只这一次从启动场景运行，不改变开关。
/// 退出 Play 后 Unity 会回到原来打开的场景。启动场景在 HowFrame 设置中配置，留空用 Build Settings 第一个启用的场景。
/// </summary>
[InitializeOnLoad]
public static class HowFramePlayFromBoot
{
    private const string PrefKey = "HowFrame.PlayFromBoot";
    private const string OnceKey = "HowFrame.PlayFromBoot.Once"; // SessionState：跨域重载保留
    private const string ToggleMenu = "Tools/HowFrame/从启动场景运行";

    static HowFramePlayFromBoot()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += Apply;
        EditorBuildSettings.sceneListChanged += Apply;
    }

    /// <summary>启动场景设置变化后重新应用（HowFrame 设置页保存时调用）</summary>
    public static void Refresh() => Apply();

    /// <summary>是否总是从启动场景运行（每个用户各自的偏好）</summary>
    public static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, false);
        set
        {
            EditorPrefs.SetBool(PrefKey, value);
            Apply();
        }
    }

    /// <summary>当前生效的启动场景路径；没有可用场景时为 null</summary>
    public static string BootScenePath
    {
        get
        {
            string configured = HowFrameSettings.instance.bootScene;
            if (!string.IsNullOrEmpty(configured) && AssetDatabase.LoadAssetAtPath<SceneAsset>(configured) != null)
                return configured;
            return EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled && !string.IsNullOrEmpty(s.path))?.path;
        }
    }

    [MenuItem(ToggleMenu, false, 20)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        UnityEngine.Debug.Log(Enabled
            ? $"[HowFrame] 已开启从启动场景运行：{BootScenePath ?? "（没有可用的启动场景）"}"
            : "[HowFrame] 已关闭从启动场景运行");
    }

    [MenuItem(ToggleMenu, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(ToggleMenu, Enabled);
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Tools/HowFrame/运行启动场景 %&p", false, 21)]
    private static void PlayOnce()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        if (BootScenePath == null)
        {
            EditorUtility.DisplayDialog("运行启动场景", "没有可用的启动场景：请在 HowFrame 设置中指定，或在 Build Settings 中添加场景。", "好");
            return;
        }
        SessionState.SetBool(OnceKey, true);
        Apply();
        EditorApplication.isPlaying = true;
    }

    private static void Apply()
    {
        bool use = Enabled || SessionState.GetBool(OnceKey, false);
        string path = use ? BootScenePath : null;
        EditorSceneManager.playModeStartScene = path == null ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                // 进入启动场景前先处理当前场景的未保存修改，避免用户以为改动丢了
                if (EditorSceneManager.playModeStartScene != null && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorApplication.isPlaying = false;
                    SessionState.EraseBool(OnceKey);
                    Apply();
                }
                break;
            case PlayModeStateChange.EnteredEditMode:
                if (SessionState.GetBool(OnceKey, false))
                {
                    SessionState.EraseBool(OnceKey);
                    Apply();
                }
                break;
        }
    }
}
