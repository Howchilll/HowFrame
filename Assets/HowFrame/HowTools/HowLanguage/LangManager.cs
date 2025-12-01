using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static HowFrame.DataAssistant;
using UnityEngine;
using HowEnum;
using LitJson;


namespace HowFrame
{

public static class LangManager
{
    private static Ref<EnumKey<LangTypeEnum.Tag>> NowLang = new();
    public static List<EnumKey<LangModuleEnum.Tag>> ModuleList;
    private static Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>> landic = new Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>>();
    private static bool _initialized = false;

    private static string _langName;

    
    public static string GetLangContent(EnumKey<LangModuleEnum.Tag> LangModule,string key)
    {
        Dictionary<string, string> moduleDict;
        moduleDict = landic[LangModule];
        if (moduleDict == null)
        {
            Debug.LogError(string.Join(",","Null ModuleDict",LangModule.name,_langName));
        }

        if (!moduleDict.ContainsKey(key))
        {
            Debug.LogError(string.Join(",", "Null Content in", LangModule.name, _langName,key));
        }
        var Content = moduleDict[key];
        if (Content == null)
        {
            Debug.LogError(string.Join(",", "Null Content in", LangModule.name, _langName,key));
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

    public static async Task SetLanguage(EnumKey<LangTypeEnum.Tag> aimType = null)
    {
        aimType ??= LangTypeEnum.English;
        string langName = aimType.name;
        _langName=langName;
        foreach (var item in ModuleList)
        {
            string moduleName =item.name;
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
                    var dic = JsonMapper.ToObject<Dictionary<string, string>>(langText.text);

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

    /// <summary>
    /// 初始化 LangManager（延迟初始化，在资源加载完成后调用）
    /// </summary>
    public static void Wake()
    {
        if (_initialized) return; // 防止重复初始化
        ModuleList = LangModuleEnum.GetAll();
        _initialized = true;
    }
}
}
