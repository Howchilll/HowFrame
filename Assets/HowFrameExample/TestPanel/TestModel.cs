using HowFrame;
using UnityEngine;
using UnityEngine.UI;
using HowEnum;

public partial class TestPanel : PanelBase
{
   private void OnInit()
   {
      
   }
   
   private void OnShow()
   {
      
   }

   private void OnHide()
   {
      
   }
    protected override void WhenShowWithParameter(object  parameter)
    {
        
    }
    

    

    private void OnButtonBtnClick()
    {
        111.Log();
    }

    private void ChangeLanguage()
    {
        var lang= LangManager.GetLangContent(LangModuleEnum.UI,"TestPanel");
        var langs = lang.Split(',');
        textTxt.text = langs[0];
    }
}

