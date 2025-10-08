using HowFrame;
using System;
using UnityEngine;

public class PropertyTest : MonoBehaviour
{

    private void Awake()
    {
        int a = 10;
        ref int b = ref a;   // C# 7.0+
        b = 20;

        // 注册事件
        PropertyAssistant.SetEvent<int>("hp", (num) =>
        {
            num.Log();
        });
        
    }
}


