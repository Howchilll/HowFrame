using HowEnum;
using HowFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MenuPanel : PanelBase
{
//Define
    [SerializeField] private Button Test_btn;
    [SerializeField] private Image Test_img;
    [SerializeField] private TextMeshProUGUI text_txt;
    [SerializeField] private TextMeshProUGUI AA_ttxt;
    [SerializeField] private Image image_img;
    [SerializeField] private Toggle Toggle1_tog;
    [SerializeField] private Toggle Toggle2_tog;
    [SerializeField] private Toggle Toggle3_tog;
    [SerializeField] private ScrollRect ScrollView_scr;
    private string[] TransContent;

    //end Define

    protected override void Init()
    {
        //Init
        Test_btn = transform.Find("Test_btn_img").GetComponent<Button>();
        Test_img = transform.Find("Test_btn_img").GetComponent<Image>();
        text_txt = transform.Find("text_txt").GetComponent<TextMeshProUGUI>();
        AA_ttxt = transform.Find("AA_ttxt").GetComponent<TextMeshProUGUI>();
        image_img = transform.Find("image_img").GetComponent<Image>();
        Toggle1_tog = transform.Find("ToggleGroup/Toggle1_tog").GetComponent<Toggle>();
        Toggle2_tog = transform.Find("ToggleGroup/Toggle2_tog").GetComponent<Toggle>();
        Toggle3_tog = transform.Find("ToggleGroup/Toggle3_tog").GetComponent<Toggle>();
        ScrollView_scr = transform.Find("ScrollView_scr").GetComponent<ScrollRect>();

        Test_btn.onClick.AddListener(OnTestBtnClick);
        Toggle1_tog.onValueChanged.AddListener((value) => OnToggle1ToggleChange(value));
        Toggle2_tog.onValueChanged.AddListener((value) => OnToggle2ToggleChange(value));
        Toggle3_tog.onValueChanged.AddListener((value) => OnToggle3ToggleChange(value));

        //end Init
        OnInit();
    }

    protected override void WhenShow()
    {
        //Show

        var rawContent = LangManager.GetLangContent(LangModuleEnum.UI, "XXXContent");
        TransContent = rawContent.Split(",");
        AA_ttxt.text = TransContent[0];

        //告诉开发者赋值的顺序: AA_ttxt

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

