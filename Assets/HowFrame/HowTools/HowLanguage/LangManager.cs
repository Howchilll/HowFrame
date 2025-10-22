using System.Collections.Generic;
using System.IO;
using static HowFrame.DataAssitant;
using UnityEngine;
using HowEnum;
using Unity.Plastic.Newtonsoft.Json;

namespace HowFrame
{

public static class LangManager
{
    private static Ref<EnumKey<LangTypeEnum.Tag>> NowLang = new();
    public static List<EnumKey<LangModuleEnum.Tag>> ModuleList;
    private static Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>> landic=new Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>>();

    private static string _langName;
    static LangManager()
    {
        ModuleList = LangModuleEnum.GetAll();
    }

    
    public static string GetLangContent(EnumKey<LangModuleEnum.Tag> LangModule,string key)
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
    public static string GetLangContent(string key)
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

    public static async void SetLanguage(EnumKey<LangTypeEnum.Tag> aimType = null)
    {
        aimType ??= LangTypeEnum.English;
        string langName = LangTypeEnum.Convert(aimType);

        foreach (var item in ModuleList)
        {
            string moduleName = LangModuleEnum.Convert(item);
            string fileName = $"{langName}_{moduleName}_Lang.json";
            string pathName =GlobalPath.LangPath+ "/" + fileName;

            if (!landic.ContainsKey(item))
                landic[item] = new Dictionary<string, string>();

            TextAsset langText = await AssetAssistant.ImportAsset<TextAsset>(pathName);

            // ✅ 反序列化成字典
            if (langText != null && !string.IsNullOrEmpty(langText.text))
            {
                try
                {
                    var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(langText.text);
                    if (dic != null)
                    {
                        landic[item] = dic;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"语言文件解析错误: {fileName}\n{ex}");
                }
            }
            else
            {
                Debug.LogWarning($"语言文件未找到: {pathName}");
            }
        }
    }
    
    public static void wake(){}
}
}
