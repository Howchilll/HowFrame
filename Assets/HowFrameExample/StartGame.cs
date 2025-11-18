using System;
using UnityEngine;
using HowFrame;
public class StartGame: MonoBehaviour
{
   private async void Start()
   {
       try
       {
           await HowInit.Init();
           "游戏初始化开始".Log();
           await AssetAssistant.LoadLabelsAsync(() =>
           {
               
               UIManager.Show("ExamplePanel");
               AudioManager.AddSound("按按钮");
               "游戏初始化完成".Log();
           },"UI","Audio");
       }
       catch (Exception e)
       {
           e.Log(); 
       }
   }
   
}
