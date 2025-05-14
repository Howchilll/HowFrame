using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AssetAssistant;

public static class UIManager 
{ 
    private static readonly Canvas Canvas;
    private static readonly Dictionary<string,PanelBase> UIObjects = new Dictionary<string,PanelBase>();
    
    static UIManager()
    {
        GameObject canvasObj = AssetAssistant.LoadAsset<GameObject>("Canvas", E_AssetType.Instance);
        canvasObj=Object.Instantiate(canvasObj);
        Canvas = canvasObj.GetComponent<Canvas>();
        Camera oldCamera = Canvas.worldCamera; 
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.worldCamera = null;      
        Object.Destroy(oldCamera.gameObject);
        Object.DontDestroyOnLoad(Canvas.gameObject);
    }
   
    public static void Show(bool father,params string[] UINames)
    {
        Transform fatherTransform;
        if (father) fatherTransform = UIObjects[UINames[0]].transform; 
        else fatherTransform = Canvas.transform;
        
        foreach (var name in UINames)
        {
            if (!UIObjects.ContainsKey(name))
            {
                if (father)
                {
                    father = false;
                    continue;
                }// 跳过father的Show
                
                GameObject ui = LoadAsset<GameObject>(name, E_AssetType.UI);
                if (ui == null)
                {
                    Debug.LogError($"UI 预设体 {name} 加载失败");
                    continue;
                }
                UIObjects[name] = Object.Instantiate(ui, fatherTransform).GetComponent<PanelBase>();   
            }
            else
            {
                UIObjects[name].gameObject.SetActive(true);
            }
            UIObjects[name].WhenShow();
        }
    }
    public static void Show(params string[] UINames)
    {
        Show(false, UINames);
    }
    public static void Hide(bool destroy=false,params string[] UINames)
    {
        foreach (var name in UINames)
        {
            if (UIObjects.ContainsKey(name))
            {
                UIObjects[name].WhenHide();
                if (destroy)
                {
                    Object.Destroy(UIObjects[name].gameObject);
                    UIObjects.Remove(name); 
                }
                else UIObjects[name].gameObject.SetActive(false);
            }
        }
      
    }
    public static void Hide(params string[] UINames)
    {
        Hide(false, UINames);
    }
    
    public static void wake(){}
}
