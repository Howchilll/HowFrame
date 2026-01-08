using System.Threading.Tasks;
using UnityEngine;
using HowFrame;
using HowEnum;

public class StartGame : MonoBehaviour
{
    private static bool _initialized;
    private static Task _initTask;

    [Header("首次加载动画")]
    public GameObject startGameObj;

    private void Awake()
    {
        // 物理唯一
        if (_initTask != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // 启动初始化（但不 await）
        _initTask = Starter();
    }

    private async void Start()
    {
        // 👉 等初始化完成，再碰 UI
        await _initTask;

        UIManager.Show("MenuPanel");
    }

    private async Task Starter()
    {
        if (_initialized)
            return;

        _initialized = true;

        startGameObj?.SetActive(true);

        "游戏初始化开始".Log(color: DebugColor.Cyan);

        await HowInit.Init();

        "默认语言配置加载".Log(color: DebugColor.Cyan);
        await LangManager.SetLanguage(LangTypeEnum.English);

        "游戏资源加载".Log(color: DebugColor.Cyan);
        await AssetAssistant.LoadLabelsAsync(() => { }, "UI", "Audio");

        "全局数据加载或初始化".Log(color: DebugColor.Cyan);
       // GlobalSetting.Wake();

        startGameObj?.SetActive(false);

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