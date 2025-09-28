using System;
using UnityEngine;
using HowFrame;
using static HowEnum.ExampleEnum;
public class PropertyTest1 : MonoBehaviour
{

    private PropertyHelper _propertyHelper;
    private void Awake()
    {
       var hp = new Ref<int>(5,example1,(a)=>{},_propertyHelper);
        // 绑定变量
        PropertyAssistant.SetObj<int>("hp", hp).OnChange((num)=>Debug.Log(num));

    }

    private void OnDestroy()
    {
        
    }
}