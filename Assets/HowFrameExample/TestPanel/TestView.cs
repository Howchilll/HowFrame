using HowFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HowEnum;
public partial class TestPanel : PanelBase
{
//Define 
    [SerializeField]
    private Button buttonBtn;
    [SerializeField]
    private GameObject imageObj;
    [SerializeField]
    private TextMeshProUGUI textTxt;

//end Define

    protected override void Init()
    {
        //Init
        buttonBtn = transform.Find("Button_Btn").GetComponent<Button>();
        buttonBtn.onClick.AddListener(OnButtonBtnClick);
        imageObj = transform.Find("Image_Obj").gameObject;
        textTxt = transform.Find("Button_Btn/Text_Txt").GetComponent<TextMeshProUGUI>();
        //end Init
        OnInit();
    }

    protected override void WhenShow()
    {
        //Show


        //end Show
        OnShow();
    }

    protected override void WhenHide()
    {
        //Hide


        //end Hide
        OnHide();
    }

    // 由 Model 中生成的 ChangeLanguage 实现；没有文本组件时编译器会移除这次调用
    partial void ChangeLanguage();

    protected override void WhenLanguageChanged()
    {
        ChangeLanguage();
    }


}

