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


}

