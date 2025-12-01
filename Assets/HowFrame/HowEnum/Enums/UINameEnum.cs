namespace HowEnum
{
    public static class UINameEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> StartPanel = new("StartPanel");
        public static readonly EnumKey<Tag> SettingPanel = new("SettingPanel");
        public static readonly EnumKey<Tag> IngamePanel = new("IngamePanel");
        public static readonly EnumKey<Tag> HintPanel = new("HintPanel");
        public static readonly EnumKey<Tag> OptionPanel = new("OptionPanel");

        /// <summary>
        /// 将字符串转换为EnumKey
        /// </summary>
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "StartPanel": return StartPanel;
                case "SettingPanel": return SettingPanel;
                case "IngamePanel": return IngamePanel;
                case "HintPanel": return HintPanel;
                case "OptionPanel": return OptionPanel;
                default: throw new System.ArgumentException($"Unknown value: {value}");
            }
        }

        /// <summary>
        /// 获取所有枚举项
        /// </summary>
        public static System.Collections.Generic.List<EnumKey<Tag>> GetAll()
        {
            return new System.Collections.Generic.List<EnumKey<Tag>>
            {
                StartPanel,
                SettingPanel,
                IngamePanel,
                HintPanel,
                OptionPanel,
            };
        }
    }
}
