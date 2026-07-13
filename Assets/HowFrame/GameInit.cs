using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HowFrame;
using HowEnum;
using UnityEngine.Serialization;

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

public class GameInit : MonoBehaviour
{
    [Header("调试用：是否阻塞初始化")]
    public bool blockOnAwake = false;

    private static bool _initialized;
    private static Task _initTask;

    public GameObject doneHide;
    public GameObject doneShow;
    public string langName;
    public List<string> resourcesTags = new() { "UI", "Audio","Prefab"};

    [SerializeField] private List<BoolDicPair> _boolDic = new();
    [SerializeField] private List<FloatDicPair> _floatDic = new();

    public static Dictionary<string, bool> BoolDic = new();
    public static Dictionary<string, float> FloatDic = new();

    private void Awake()
    {
        if (blockOnAwake)
        {
            // 阻塞主线程，初始化完成再继续
            AwakeAsync().GetAwaiter().GetResult();
        }
        else
        {
            // 非阻塞异步
            _ = AwakeAsync();
        }
    }

    private async Task AwakeAsync()
    {
        BoolDic.Clear();
        FloatDic.Clear();
        doneHide.SetActive(true);
        doneShow.SetActive(false);

        foreach (var pair in _boolDic)
            BoolDic[pair.key] = pair.value;

        foreach (var pair in _floatDic)
            FloatDic[pair.key] = pair.value;

        _initTask = Starter();

        try
        {
            await _initTask;
        }
        catch (Exception e)
        {
            Debug.LogError($"GameInit 初始化异常: {e}");
        }

        doneHide.SetActive(false);
        doneShow.SetActive(true);
    }

    private async Task Starter()
    {
        if (_initialized)
            return;

        _initialized = true;

        "游戏初始化开始".Log(color: DebugColor.Cyan);
        await HowInit.Init();

        "默认语言配置加载".Log(color: DebugColor.Cyan);
        await LangManager.SetLanguage(LangTypeEnum.Convert(langName));

        "游戏资源加载".Log(color: DebugColor.Cyan);
        await AssetAssistant.LoadLabelsAsync(() => { }, resourcesTags.ToArray());

        "全局数据加载或初始化".Log(color: DebugColor.Cyan);
        // GlobalSetting.Wake();

        "游戏初始化完成".Log(color: DebugColor.Cyan);
    }

    private void OnApplicationQuit()
    {
       // GlobalSetting.SaveData();
    }

    private void OnDestroy()
    {
      //  GlobalSetting.SaveData();
    }
}