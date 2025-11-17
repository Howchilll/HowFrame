using UnityEngine;
using HowFrame;

/// <summary>
/// HowMono 使用示例
/// 演示如何继承 HowMono 来替代 MonoBehaviour，并获得可控制的更新频率
/// </summary>
public class HowMonoExample : HowMono
{
    private int _updateCount = 0;
    private int _frameCount = 0;
    
    // 示例1: 使用默认 60fps
    // 不需要重写 fps 属性，直接使用默认值

    protected override void OnAwake()
    {
        Debug.Log("[HowMonoExample] OnAwake 被调用");
        // 可以在这里初始化组件、获取引用等
    }

    protected override void OnStart()
    {
        Debug.Log("[HowMonoExample] OnStart 被调用");
        // 可以在这里进行依赖其他对象的初始化
    }

    protected override void OnUpdate()
    {
        _updateCount++;
        _frameCount++;
        
        // 每 60 次更新打印一次（因为 60fps，所以大约每秒打印一次）
        if (_updateCount % 60 == 0)
        {
            Debug.Log($"[HowMonoExample] 已更新 {_updateCount} 次，总帧数: {_frameCount}");
        }
        
        // 可以在这里执行更新逻辑
        // transform.Rotate(Vector3.up * Time.deltaTime);
    }
}

/// <summary>
/// 示例2: 自定义更新频率（30fps）
/// </summary>
public class LowFPSExample : HowMono
{
    private int _updateCount = 0;
    
    // 重写 fps 属性，设置为 30fps
    protected override int fps => 30;

    protected override void OnAwake()
    {
        Debug.Log("[LowFPSExample] 初始化，使用 30fps");
    }

    protected override void OnUpdate()
    {
        _updateCount++;
        
        // 每 30 次更新打印一次（因为 30fps，所以大约每秒打印一次）
        if (_updateCount % 30 == 0)
        {
            Debug.Log($"[LowFPSExample] 30fps 更新，已执行 {_updateCount} 次");
        }
        
        // 适合不需要高频率更新的逻辑，比如 UI 刷新、AI 决策等
    }
}

/// <summary>
/// 示例3: 15fps 更新（适合低频逻辑）
/// </summary>
public class VeryLowFPSExample : HowMono
{
    private int _updateCount = 0;
    
    protected override int fps => 15; // 15fps，每 4 帧更新一次

    protected override void OnUpdate()
    {
        _updateCount++;
        
        // 适合低频更新，比如网络同步、数据统计等
        if (_updateCount % 15 == 0)
        {
            Debug.Log($"[VeryLowFPSExample] 15fps 更新，已执行 {_updateCount} 次");
        }
    }
}

/// <summary>
/// 示例4: 实际游戏组件示例
/// </summary>
public class PlayerController : HowMono
{
    private float _speed = 5f;
    private Vector3 _moveDirection;
    
    // 使用默认 60fps，适合需要流畅控制的角色移动

    protected override void OnAwake()
    {
        // 初始化组件
        // _rigidbody = GetComponent<Rigidbody>();
    }

    protected override void OnStart()
    {
        // 依赖其他系统的初始化
        // InputManager.Instance.RegisterPlayer(this);
    }

    protected override void OnUpdate()
    {
        // 处理输入
        // _moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        // 移动角色
        // transform.position += _moveDirection * _speed * Time.deltaTime;
    }
}

/// <summary>
/// 示例5: UI 更新组件（使用低帧率）
/// </summary>
public class UIHealthBar : HowMono
{
    private int _currentHealth = 100;
    private int _maxHealth = 100;
    
    protected override int fps => 30; // UI 不需要 60fps，30fps 足够

    protected override void OnUpdate()
    {
        // 更新血条显示
        // healthBar.fillAmount = (float)_currentHealth / _maxHealth;
        
        // 30fps 对 UI 来说已经足够流畅，还能节省性能
    }
}

