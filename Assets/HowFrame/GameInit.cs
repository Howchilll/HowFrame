using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HowFrame;
using HowEnum;

[Serializable]
public struct BoolDicPair
{
    public string key;
    public bool value;
}

[Serializable]
public struct FloatDicPair
{
    public string key;
    public float value;
}

/// <summary>
/// 场景入口：初始化框架 → 加载默认语言 → 预加载业务资源。
/// 其他脚本可通过 GameInit.IsReady / GameInit.Ready / GameInit.WhenReady 等待游戏初始化完成。
/// </summary>
public class GameInit : MonoBehaviour
{
    [Header("加载期间显示 / 加载完成后显示（可不填）")]
    public GameObject doneHide;
    public GameObject doneShow;

    [Header("默认语言（LangTypeEnum 中的名字），留空则用 English")]
    public string langName;

    [Header("初始化时预加载的 Addressables 标签")]
    public List<string> resourcesTags = new() { "UI", "Audio", "Prefab" };

    [SerializeField] private List<BoolDicPair> _boolDic = new();
    [SerializeField] private List<FloatDicPair> _floatDic = new();

    public static Dictionary<string, bool> BoolDic = new();
    public static Dictionary<string, float> FloatDic = new();

    /// <summary>当前加载进度 0~1（业务资源预加载阶段）</summary>
    public static float Progress { get; private set; }

    /// <summary>游戏初始化（框架 + 语言 + 业务资源）是否完成</summary>
    public static bool IsReady { get; private set; }

    /// <summary>初始化完成时触发（只触发一次）。已就绪后再订阅请用 WhenReady。</summary>
    public static event Action OnReady;

    private static Task _initTask;

    /// <summary>可 await 的游戏初始化任务（场景中没有 GameInit 时为 null）</summary>
    public static Task Ready => _initTask;

    /// <summary>游戏初始化完成后执行；已完成则立即执行</summary>
    public static void WhenReady(Action action)
    {
        if (action == null) return;
        if (IsReady) action();
        else OnReady += action;
    }

    // 注意：不提供"阻塞 Awake 直到初始化完成"的选项——Addressables / UnityWebRequest 的回调
    // 都要在主线程推进，阻塞主线程等待会直接死锁。需要等待请用 Ready / WhenReady。
    private void Awake()
    {
        _ = AwakeAsync();
    }

    private async Task AwakeAsync()
    {
        BoolDic.Clear();
        FloatDic.Clear();
        foreach (var pair in _boolDic)
            BoolDic[pair.key] = pair.value;
        foreach (var pair in _floatDic)
            FloatDic[pair.key] = pair.value;

        SetActiveSafe(doneHide, true);
        SetActiveSafe(doneShow, false);

        // 场景重载时复用同一个初始化任务，不重复初始化
        _initTask ??= Starter();

        try
        {
            await _initTask;
        }
        catch (Exception e)
        {
            Debug.LogError($"GameInit 初始化异常: {e}");
        }

        if (this == null) return; // 等待期间对象已被销毁
        SetActiveSafe(doneHide, false);
        SetActiveSafe(doneShow, true);
    }

    private async Task Starter()
    {
        "游戏初始化开始".Log(color: DebugColor.Cyan);
        await HowInit.Init();

        "默认语言配置加载".Log(color: DebugColor.Cyan);
        await LangManager.SetLanguage(ParseLanguage(langName));

        "游戏资源加载".Log(color: DebugColor.Cyan);
        Progress = 0f;
        await AssetAssistant.LoadLabelsAsync(null, p => Progress = p, resourcesTags.ToArray());

        "游戏初始化完成".Log(color: DebugColor.Cyan);
        IsReady = true;

        var callbacks = OnReady;
        OnReady = null;
        if (callbacks == null) return;
        foreach (Action callback in callbacks.GetInvocationList())
        {
            try { callback(); }
            catch (Exception e) { Debug.LogError($"[GameInit] OnReady 回调异常: {e}"); }
        }
    }

    private static EnumKey<LangTypeEnum.Tag> ParseLanguage(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null; // LangManager 默认 English
        try
        {
            return LangTypeEnum.Convert(name.Trim());
        }
        catch (ArgumentException)
        {
            Debug.LogWarning($"GameInit: 未知的语言 \"{name}\"，改用默认语言");
            return null;
        }
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
