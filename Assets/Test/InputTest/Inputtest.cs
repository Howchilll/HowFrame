using System;
using UnityEngine;
using UnityEngine.InputSystem;
using HowFrame;
using HowEnum;

/// <summary>
/// InputAssistant 完整使用示例
/// 演示了输入系统的初始化、绑定、持续读取、设备切换监听等功能
/// </summary>
public class Inputtest : MonoBehaviour
{
    // 玩家移动速度
    [SerializeField] private float moveSpeed = 5f;
    
    // 当前移动方向（持续读取）
    private Vector2 currentMoveInput = Vector2.zero;
    
    // 跳跃力
    [SerializeField] private float jumpForce = 10f;
    
    // 是否在地面上
    private bool isGrounded = true;
    
    // Rigidbody 组件（用于移动和跳跃）
    private Rigidbody rb;
    
    private void Awake()
    {
        // 获取 Rigidbody 组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }
    
    private void Start()
    {
        // 初始化输入系统
        InputAssistant.Wake();
        
        // 延迟初始化，等待资源加载完成
        CoroutineAssistant.DelayInvoke("InputInit", 1f, () =>
        {
            InitializeInput();
        });
    }
    
    /// <summary>
    /// 初始化输入绑定
    /// </summary>
    private void InitializeInput()
    {
        // 启用需要的 ActionMap
        InputAssistant.EnableMap("Move");
        InputAssistant.EnableMap("Attack");
        
        // 绑定移动操作 - 使用 performed 事件（适合持续输入）
        InputAssistant.BindAction("Move", "MoveAround", OnMoveInput);
        
        // 绑定跳跃操作 - Button 类型，只在按下时触发
        InputAssistant.BindAction("Move", "Jump", OnJump);
        
        // 绑定攻击操作
        InputAssistant.BindAction("Attack", "Attack", OnAttack);
        
        // 绑定技能操作
        InputAssistant.BindAction("Attack", "Skill", OnSkill);
        
        // 监听输入类型变化（键鼠/手柄切换）
        PropertyAssistant.SetEvent<EnumKey<InputEnum.Tag>>(
            GlobalEventEnum.InputTypeChange,
            OnInputTypeChanged
        );
        
        Debug.Log("[InputTest] 输入系统初始化完成");
        Debug.Log($"[InputTest] 当前输入类型: {InputAssistant.InputType.Value}");
    }
    
    private void Update()
    {
        // 在 Update 中持续读取移动输入值（适用于持续输入）
        // 这样可以在每一帧都获取最新的输入状态
        currentMoveInput = InputAssistant.ReadValue<Vector2>("Move", "MoveAround");
        
        // 应用移动
        if (currentMoveInput.magnitude > 0.1f)
        {
            MovePlayer(currentMoveInput);
        }
    }
    
    /// <summary>
    /// 移动输入回调（用于调试和特殊处理）
    /// </summary>
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        Vector2 move = context.ReadValue<Vector2>();
        
        // 获取输入阶段（Started/Performed/Canceled）
        if (context.started)
        {
            Debug.Log($"[InputTest] 开始移动: {move}");
        }
        else if (context.canceled)
        {
            Debug.Log("[InputTest] 停止移动");
        }
        // performed 阶段在 Update 中通过 ReadValue 持续读取，这里不做处理
    }
    
    /// <summary>
    /// 实际移动玩家的逻辑
    /// </summary>
    private void MovePlayer(Vector2 moveDirection)
    {
        // 计算移动方向（假设 Y 轴是上下，X 轴是左右）
        Vector3 move = new Vector3(moveDirection.x, 0f, moveDirection.y);
        move = move.normalized * moveSpeed * Time.deltaTime;
        
        // 应用移动（这里使用 Transform，你也可以使用 Rigidbody）
        transform.Translate(move, Space.World);
        
        // 或者使用 Rigidbody（取消上面注释，注释掉 Transform.Translate）
        // rb.MovePosition(transform.position + move);
    }
    
    /// <summary>
    /// 跳跃输入回调
    /// </summary>
    private void OnJump(InputAction.CallbackContext context)
    {
        // Button 类型的 Action，在按下时触发（started 或 performed 阶段）
        if (context.performed && isGrounded)
        {
            Jump();
        }
    }
    
    /// <summary>
    /// 执行跳跃
    /// </summary>
    private void Jump()
    {
        if (rb != null && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            Debug.Log("[InputTest] 执行跳跃");
        }
    }
    
    /// <summary>
    /// 攻击输入回调
    /// </summary>
    private void OnAttack(InputAction.CallbackContext context)
    {
        // Button 类型，按下时触发
        if (context.performed)
        {
            PerformAttack();
        }
    }
    
    /// <summary>
    /// 执行攻击
    /// </summary>
    private void PerformAttack()
    {
        Debug.Log("[InputTest] 执行攻击");
        // TODO: 实现攻击逻辑
        // 例如：播放攻击动画、生成攻击判定、造成伤害等
    }
    
    /// <summary>
    /// 技能输入回调
    /// </summary>
    private void OnSkill(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformSkill();
        }
    }
    
    /// <summary>
    /// 执行技能
    /// </summary>
    private void PerformSkill()
    {
        Debug.Log("[InputTest] 执行技能");
        // TODO: 实现技能逻辑
    }
    
    /// <summary>
    /// 输入类型变化回调（键鼠/手柄切换）
    /// </summary>
    private void OnInputTypeChanged(EnumKey<InputEnum.Tag> inputType)
    {
        Debug.Log($"[InputTest] 输入类型已切换为: {inputType}");
        
        // 根据输入类型调整 UI 提示
        if (inputType == InputEnum.GamePad)
        {
            Debug.Log("[InputTest] 当前使用手柄，可以显示手柄图标提示");
        }
        else if (inputType == InputEnum.MouseKeyboard)
        {
            Debug.Log("[InputTest] 当前使用键鼠，可以显示键盘图标提示");
        }
    }
    
    private void OnEnable()
    {
        // 组件启用时，确保输入映射已启用
        if (InputAssistant.IsMapEnabled("Move"))
        {
            // 如果已经在运行，可以重新绑定回调
            // 注意：这里只是示例，实际使用时需要根据你的架构决定是否需要重新绑定
        }
    }
    
    private void OnDisable()
    {
        // 组件禁用时，可以选择禁用输入映射或保持启用（取决于游戏需求）
        // InputAssistant.DisableMap("Move");
        // InputAssistant.DisableMap("Attack");
    }
    
    private void OnDestroy()
    {
        // 组件销毁时，解绑所有回调，避免内存泄漏
        InputAssistant.UnbindAction("Move", "MoveAround", OnMoveInput);
        InputAssistant.UnbindAction("Move", "Jump", OnJump);
        InputAssistant.UnbindAction("Attack", "Attack", OnAttack);
        InputAssistant.UnbindAction("Attack", "Skill", OnSkill);
        
        Debug.Log("[InputTest] 已清理输入绑定");
    }
    
    // 碰撞检测（用于判断是否在地面上）
    private void OnCollisionEnter(Collision collision)
    {
        // 简单的接地检测
        if (collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        // 离开地面
        isGrounded = false;
    }
}
