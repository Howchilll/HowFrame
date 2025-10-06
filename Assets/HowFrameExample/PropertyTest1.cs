using System;
using UnityEngine;
using HowFrame;
using static HowEnum.ExampleEnum;
public class PropertyTest1 : MonoBehaviour
{

    private PropertyHelper _propertyHelper=new PropertyHelper();

    private Ref<int> hp = new(50);
    private void Awake()
    {
        _propertyHelper.SetObj("hp", hp);
        _propertyHelper.SetEvent<int>("hp", (a) =>
        {
            a.Log("这是hp：");
        });
  
    }

    private void Start()
    {
        CoroutineAssistant.StartLoop("12",1, () =>
        {
            hp.Value+=1;
        });
    }
}