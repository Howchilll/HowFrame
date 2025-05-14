using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class InputCheck 
{
    public  InputCheck()
    {
        DataManager.GlobalLoad();
        DataManager.ArchiveLoad();
   
    }
    
    
    public void init()
    {
        MonoAssistant.OnUpdate += TheInputCheck;
    }
    
    


    private void TheInputCheck()
    {
        if (Input.GetKeyDown(KeyAssistant.Keys["DebugTest"]))
        {
            "Debug Test".Log();
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["AudioTest"]))
        {
           AudioManager.AddSound("按按钮");
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["HP++"]))
        {
            ArchiveData.Hp++;
            EventAssistant.Invoke("HPChange");
            DataManager.ArchiveSave();
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["CameraEffectTest1"]))
        {
           CameraEffect.Shake();
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["CameraEffectTest2"]))
        {
           CameraEffect.SetVignette(5);
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["UIShow"]))
        {
            UIManager.Show("ExamplePanel");
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["UIHide"]))
        {
          UIManager.Hide("ExamplePanel");
        }

        if (Input.GetKeyDown(KeyAssistant.Keys["MusicTest"]))
        {
            AudioManager.AddMusic("OTS1");
        }
        
    }
    
    
    
}
