using System.Collections.Generic;
using System.IO;
using static HowFrame.DataAssitant;
using UnityEngine;
using HowEnum;
namespace HowFrame
{

public static class LangManager
{
    private static Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>> landic=new Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>>();
        
    public static Dictionary<string, string> LanDic = new Dictionary<string, string>();
    private static string _langName;
    private static Language _language;

    static LangManager()
    {
       // LoadLangData(GlobalData.Language);
    }

    
    public static string GetContent(EnumKey<LangModuleEnum.Tag> LangModule,string key)
    {
        var ModuleDict = landic[LangModule];
        if (ModuleDict == null)
        {
            Debug.LogError("Null ModuleDict");
        }

        var Content = ModuleDict[key];
        if (Content == null)
        {
            Debug.LogError("Null Cantent");
        }
        return Content;
    }
    public static string GetContent(string key)
    {
        var ModuleDict = landic[LangModuleEnum.Default];
        if (ModuleDict == null)
        {
            Debug.LogError("Null ModuleDict");
        }

        var Content = ModuleDict[key];
        if (Content == null)
        {
            Debug.LogError("Null Cantent");
        }
        return Content;
    }
    
    // public static void LoadLangData(string langName)
    // {
    //     if (langName == _langName) return;
    //
    //     _language = LoadConfig<Language>("Languages/" + langName);
    //     _langName = _language.LanguageName;
    //     LanDic = _language.LanguageDictionary ?? new Dictionary<string, string>();
    //     GlobalData.Language=langName;
    // }
    public static void wake(){}
}
}
