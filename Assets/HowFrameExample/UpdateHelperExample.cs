using UnityEngine;
using HowFrame;

/// <summary>
/// UpdateHelper 使用示例
/// 演示如何创建一个普通类并使用 UpdateHelper 来注册更新事件
/// </summary>
public class UpdateHelperExample : MonoBehaviour
{
    private UpdateHelper _updateHelper;
    private UpdateHelper _systemUpdateHelper;
    
    private int _frameCount = 0;
    private int _systemFrameCount = 0;

    private void Start()
    {
        // 示例1: 创建 Unity 主线程更新（默认 60fps）
        _updateHelper = new UpdateHelper();
        _updateHelper.OnUpdate += OnUnityUpdate;
        
        // 示例2: 创建 30fps 的更新
        // _updateHelper = new UpdateHelper(30);
        // _updateHelper.OnUpdate += OnUnityUpdate;
        
        // 示例3: 创建系统线程更新（异步执行）
        _systemUpdateHelper = new UpdateHelper(60, isSystemUpdate: true);
        _systemUpdateHelper.OnUpdate += OnSystemUpdate;
    }

    /// <summary>
    /// Unity 主线程更新回调（在主线程执行）
    /// </summary>
    private void OnUnityUpdate()
    {
        _frameCount++;
        
        // 每 60 帧打印一次
        if (_frameCount % 60 == 0)
        {
            Debug.Log($"[UnityUpdate] 已执行 {_frameCount} 帧");
        }
        
        // 可以在这里执行 Unity API 操作，比如 Transform、GameObject 等
        // transform.Rotate(Vector3.up * Time.deltaTime);
    }

    /// <summary>
    /// 系统线程更新回调（在后台线程执行）
    /// </summary>
    private void OnSystemUpdate()
    {
        _systemFrameCount++;
        
        // 每 60 帧打印一次
        if (_systemFrameCount % 60 == 0)
        {
            Debug.Log($"[SystemUpdate] 已执行 {_systemFrameCount} 帧");
        }
        
        // 注意：这里不能使用 Unity API，因为不在主线程
        // 可以执行纯 C# 计算、网络请求等操作
    }

    private void OnDestroy()
    {
        // 重要：记得释放资源，取消注册
        _updateHelper?.Dispose();
        _systemUpdateHelper?.Dispose();
    }

    // ========== 其他使用方式示例 ==========
    
    /// <summary>
    /// 示例：在普通 C# 类中使用（不继承 MonoBehaviour）
    /// </summary>
    public class MyGameLogic
    {
        private UpdateHelper _helper;
        private int _counter = 0;

        public MyGameLogic()
        {
            // 创建 15fps 的更新器
            _helper = new UpdateHelper(15);
            _helper.OnUpdate += UpdateLogic;
        }

        private void UpdateLogic()
        {
            _counter++;
            // 执行游戏逻辑更新
        }

        public void Cleanup()
        {
            // 清理时释放
            _helper?.Dispose();
        }
    }
}

