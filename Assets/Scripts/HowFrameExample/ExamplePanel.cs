using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ExamplePanel : PanelBase
{
    [SerializeField] private TMP_Text HP;
    [SerializeField] private Button CButton;
    [SerializeField] private Button EButton;
    
    
    
    
    protected override void Init()
    {
        EventAssistant.Subscribe("HPChange",TextLangCheck);
        
        
        CButton.onClick.AddListener(()=>
        {
            LangManager.LoadLangData("Chinese");
            TextLangCheck();
        });
        
        EButton.onClick.AddListener(()=>
        {
            LangManager.LoadLangData("English");
            TextLangCheck();
        });

    }

    private void TextLangCheck()
    {
        HP.text = LangManager.LanDic["HP"] + ":" + ArchiveData.Hp;
    }
    
    
    protected internal override void WhenShow()
    {
        TextLangCheck();
    }

    protected internal override void WhenHide()
    {
       "PanelHide".Log();
    }
}
