using HowFrame;
using System;
using UnityEngine;

public class PropertyTest : MonoBehaviour
{
    private void Awake()
    {
        

        // 注册事件
        PropertyAssistant.SetEvent<int>("hp", (num) =>
        {
            num.Log();
        });
        
    }
}


