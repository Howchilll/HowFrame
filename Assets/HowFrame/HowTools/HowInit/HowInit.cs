using System;
using System.Threading.Tasks;
using HowFrame;
using UnityEngine;


public static class HowInit
{
      public async static Task Init()
      {
            Debug.Log("框架初始化开始");
            AssetAssistant.Wake();
            await AssetAssistant.LoadLabelsAsync(() =>
            {
                  AudioManager.Wake();
                  CoroutineAssistant.Wake();
                  DataAssistant.Wake();
                  DebugAssistant.Wake();
                  InputAssistant.Wake();
                //  KeyAssistant.Wake(); 已经弃用
                  LangManager.Wake();
                  MonoAssistant.Wake();
                  PropertyAssistant.Wake();
                  SceneLoadAssistant.Wake();
                  UIManager.Wake();
                  TypeAssistant.Wake();
                  UpdateAssistant.Wake();
                  Debug.Log("框架初始化完成");
            },"Instance");
      }
      
      
      
}
