using System;
using UnityEngine;
using HowEnum;
using HowFrame;
namespace Test.LanguageTest
{
    public class LanguageTest : MonoBehaviour
    {
        private void Start()
        {
            LangManager.GetLangContent(LangModuleEnum.UI, "设置音量");
            LangManager.GetLangContent(LangModuleEnum.UI, "设置音量1");
            LangManager.GetLangContent(LangModuleEnum.UI, "设置音量2");
            LangManager.GetLangContent(LangModuleEnum.UI, "设置音量3");
            LangManager.GetLangContent(LangModuleEnum.UI, "设置音量4");
            LangManager.GetLangContent(LangModuleEnum.UI, "设置音量5");
            
            LangManager.GetLangContent( "设置a");
            LangManager.GetLangContent(LangModuleEnum.ItemInfo, "设置b");
        }
    }
}