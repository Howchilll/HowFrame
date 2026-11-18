using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HowEnum;
using LitJson;

namespace HowFrame
{
    /// <summary>
    /// 多语言管理：按 语种(LangTypeEnum) × 模块(LangModuleEnum) 加载
    /// StreamingAssets/{GlobalPath.LangPath}/{语种}_{模块}_Lang.json。
    /// 切换语言后通过 OnLanguageChanged / CurrentLanguage 通知，UIManager 会据此刷新所有面板（PanelBase.WhenLanguageChanged）。
    /// </summary>
    public static class LangManager
    {
        /// <summary>当前语言（可订阅 OnChanged）</summary>
        public static readonly Ref<EnumKey<LangTypeEnum.Tag>> CurrentLanguage = new();

        /// <summary>语言切换并加载完成后触发</summary>
        public static event Action OnLanguageChanged;

        public static List<EnumKey<LangModuleEnum.Tag>> ModuleList;

        private static Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>> landic =
            new Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>>();

        // 同一个缺失的 key 只警告一次，避免每帧刷屏
        private static readonly HashSet<string> _reportedMissing = new HashSet<string>();
        private static bool _initialized;

        /// <summary>取指定模块下的文本；缺失时返回 key 本身并警告一次</summary>
        public static string GetLangContent(EnumKey<LangModuleEnum.Tag> langModule, string key)
        {
            if (TryGetLangContent(langModule, key, out var content)) return content;
            ReportMissing(langModule?.name, key);
            return key;
        }

        /// <summary>取 Default 模块下的文本；缺失时返回 key 本身并警告一次</summary>
        public static string GetLangContent(string key)
        {
            return GetLangContent(LangModuleEnum.Default, key);
        }

        public static bool TryGetLangContent(EnumKey<LangModuleEnum.Tag> langModule, string key, out string content)
        {
            content = null;
            if (langModule == null || key == null) return false;
            return landic.TryGetValue(langModule, out var moduleDict) && moduleDict != null &&
                   moduleDict.TryGetValue(key, out content) && content != null;
        }

        /// <summary>切换语言并加载所有模块的语言文件；为空时使用 English</summary>
        public static async Task SetLanguage(EnumKey<LangTypeEnum.Tag> aimType = null)
        {
            Wake();
            aimType ??= LangTypeEnum.English;
            string langName = aimType.name;

            // 先加载到新字典，全部完成后再整体替换，避免加载途中读到一半新一半旧
            var newDic = new Dictionary<EnumKey<LangModuleEnum.Tag>, Dictionary<string, string>>();
            foreach (var module in ModuleList)
            {
                string fileName = $"{langName}_{module.name}_Lang.json";
                string pathName = GlobalPath.LangPath + "/" + fileName;
                newDic[module] = new Dictionary<string, string>();

                TextAsset langText = await AssetAssistant.ImportAsset<TextAsset>(pathName);
                if (langText == null || string.IsNullOrEmpty(langText.text))
                {
                    Debug.LogWarning($"语言文件未找到: {pathName}");
                    continue;
                }

                try
                {
                    var dic = JsonMapper.ToObject<Dictionary<string, string>>(langText.text);
                    if (dic != null) newDic[module] = dic;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"语言文件解析错误: {fileName}\n{ex}");
                }
            }

            landic = newDic;
            _reportedMissing.Clear();
            CurrentLanguage.Value = aimType;

            var callbacks = OnLanguageChanged;
            if (callbacks == null) return;
            foreach (Action callback in callbacks.GetInvocationList())
            {
                try { callback(); }
                catch (Exception e) { Debug.LogError($"[LangManager] OnLanguageChanged 回调异常: {e}"); }
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

        // 运行期间遇到的缺失 key（按模块），切换语言不清空，方便调试窗口导出到 Excel
        private static readonly Dictionary<string, HashSet<string>> _missingKeys = new Dictionary<string, HashSet<string>>();

        /// <summary>运行期间遇到的缺失 key：模块名 → key 集合</summary>
        public static IReadOnlyDictionary<string, HashSet<string>> MissingKeys => _missingKeys;

        /// <summary>缺失 key 总数</summary>
        public static int MissingCount
        {
            get
            {
                int count = 0;
                foreach (var set in _missingKeys.Values) count += set.Count;
                return count;
            }
        }

        public static void ClearMissingKeys()
        {
            _missingKeys.Clear();
            _reportedMissing.Clear();
        }

        private static void ReportMissing(string module, string key)
        {
            if (key == null) return;
            module ??= "";
            if (!_missingKeys.TryGetValue(module, out var set))
                _missingKeys[module] = set = new HashSet<string>();
            set.Add(key);

            string id = module + "/" + key;
            if (!_reportedMissing.Add(id)) return;
            Debug.LogWarning($"[LangManager] 缺少文本: 语言={CurrentLanguage.Value?.name ?? "未加载"} 模块={module} key={key}");
        }
    }
}
