using System;
using UnityEngine;

public class HowFrameWake: MonoBehaviour
{
   private void Start()
   {
      AssetAssistant.wake();
      DataAssitant.wake();
      EventAssistant.wake();
      KeyAssistant.wake();
      MonoAssistant.wake();
      SceneLoadAssistant.wake();
      TypeAssistant.wake();
      
      DataManager.Wake();
      AudioManager.wake();
      CameraEffect.Wake();
      LangManager.wake();
      UIManager.wake();



      InputCheck inputCheck = new InputCheck();
      inputCheck.init();
   }
   
}
