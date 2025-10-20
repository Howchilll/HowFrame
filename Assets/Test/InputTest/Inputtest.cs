
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using HowFrame;

    public class Inputtest:MonoBehaviour
    {
        private void Start()
        {
            InputAssistant.Wake();
            CoroutineAssistant.DelayInvoke("11",1, () =>
            {
                InputAssistant.EnableMap("Move");
                InputAssistant.EnableMap("Attack");
                InputAssistant.BindAction("Move","MoveAround",OnMove);
                InputAssistant.BindAction("Move","Jump",OnJump);
                InputAssistant.BindAction("Attack","Attack",OnAttack);
                InputAssistant.BindAction("Attack","Skill",OnSkill);  
            });

            
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
        
        private void OnSkill(InputAction.CallbackContext context)
        {
            Debug.Log("Skill");
        }

    }
