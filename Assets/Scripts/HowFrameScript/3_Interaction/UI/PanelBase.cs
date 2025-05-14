using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AssetAssistant;

public abstract class PanelBase : MonoBehaviour
{ 
    protected abstract void Init();
   protected  internal virtual void WhenShow(){}
   protected  internal virtual void WhenHide(){}
    
    private void Start()
    {
        Init();
    }
}
