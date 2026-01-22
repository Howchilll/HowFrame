using System;
using UnityEngine;
using UnityEngine.InputSystem;
using HowFrame;

public class Inputtest:MonoBehaviour
{
    private void Start()
    {
  
            InputAssistant.EnableMap("Move");
            InputAssistant.EnableMap("Attack");
            InputAssistant.BindAction("Move","MoveAround",OnMove);
            InputAssistant.BindAction("Move","Jump",OnJump);
            InputAssistant.BindAction("Attack","Attack",OnAttack,OnAttackDone);
            InputAssistant.BindAction("Attack","Skill",OnSkill);  
            
            
    }
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 move = context.ReadValue<Vector2>();
        Debug.Log($"玩家移动方向: {move}");
        // TODO: 玩家移动逻辑
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        Debug.Log("Jump");
    }
        
    private void OnAttack(InputAction.CallbackContext context)
    {
        Debug.Log("Attack");
    }
    private void OnAttackDone(InputAction.CallbackContext context)
    {
        Debug.Log("OnAttackDone");
    }
    private void OnSkill(InputAction.CallbackContext context)
    {
        Debug.Log("Skill");
    }

}