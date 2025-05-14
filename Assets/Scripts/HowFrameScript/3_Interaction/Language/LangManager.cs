using System.Collections.Generic;
using System.IO;
using static DataAssitant;
using UnityEngine;

public static class LangManager
{
    public static Dictionary<string, string> LanDic = new Dictionary<string, string>();
    private static string _langName;
    private static Language _language;

    static LangManager()
    {
        LoadLangData(GlobalData.Language);
    }
    
    
    
    public static void LoadLangData(string langName)
    {
        if (langName == _langName) return;

        _language = LoadConfig<Language>("Languages/" + langName);
        _langName = _language.LanguageName;
        LanDic = _language.LanguageDictionary ?? new Dictionary<string, string>();
        GlobalData.Language=langName;
    }
    public static void wake(){}
}
