using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HowFrame.AssetAssistant;
using Object = UnityEngine.Object;

namespace HowFrame
{
    public abstract class PanelBase : MonoBehaviour
    {
        protected internal abstract void Init();

        protected internal virtual void WhenShow()
        {
        }

        protected internal virtual void WhenHide()
        {
        }

        protected internal virtual void WhenShowWithParameter(object parameter)
        {
        }


    }
}