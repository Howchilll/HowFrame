using System;
using UnityEngine;
using HowFrame;
public class PropertyTest1 : MonoBehaviour
{
    private Ref<int> hp;
    private void Awake()
    {
         hp = new Ref<int>();
        // 绑定变量
        PropertyAssistant.SetObj<int>("hp", hp);

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            hp.Value = 200;
        }
    }
}