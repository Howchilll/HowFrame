
using System;
using UnityEngine;

public class PropertyTest : MonoBehaviour
{
    private void Awake()
    {
        

        // 注册事件
        PropertyAssistant<int>.SetEvent("hp", (num) =>
        {
            num.Log();
        });
        
    }
}


