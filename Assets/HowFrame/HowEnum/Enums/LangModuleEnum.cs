namespace HowEnum
{
    public static class LangModuleEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> UI = new("UI");
        public static readonly EnumKey<Tag> ItemInfo = new("ItemInfo");
        public static readonly EnumKey<Tag> Default = new("Default");

        /// <summary>
        /// 将字符串转换为EnumKey
        /// </summary>
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "UI": return UI;
                case "ItemInfo": return ItemInfo;
                case "Default": return Default;
                default: throw new System.ArgumentException($"Unknown value: {value}");
            }
        }

        /// <summary>
        /// 将EnumKey转换为字符串
        /// </summary>
        public static string Convert(EnumKey<Tag> enumKey)
        {
            if (enumKey == null) return null;

            if (enumKey == UI) return "UI";
            if (enumKey == ItemInfo) return "ItemInfo";
            if (enumKey == Default) return "Default";
            throw new System.ArgumentException($"Unknown enumKey: {enumKey}");
        }

        /// <summary>
        /// 获取所有枚举项
        /// </summary>
        public static System.Collections.Generic.List<EnumKey<Tag>> GetAll()
        {
            return new System.Collections.Generic.List<EnumKey<Tag>>
            {
                UI,
                ItemInfo,
                Default,
            };
        }
    }
}
