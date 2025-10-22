namespace HowEnum
{
    public static class LangTypeEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> Chinese = new();
        public static readonly EnumKey<Tag> English = new();
        public static readonly EnumKey<Tag> Malayu = new();

        /// <summary>
        /// 将字符串转换为EnumKey
        /// </summary>
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "Chinese": return Chinese;
                case "English": return English;
                case "Malayu": return Malayu;
                default: throw new System.ArgumentException($"Unknown value: {value}");
            }
        }

        /// <summary>
        /// 将EnumKey转换为字符串
        /// </summary>
        public static string Convert(EnumKey<Tag> enumKey)
        {
            if (enumKey == null) return null;

            if (enumKey == Chinese) return "Chinese";
            if (enumKey == English) return "English";
            if (enumKey == Malayu) return "Malayu";
            throw new System.ArgumentException($"Unknown enumKey: {enumKey}");
        }

        /// <summary>
        /// 获取所有枚举项
        /// </summary>
        public static System.Collections.Generic.List<EnumKey<Tag>> GetAll()
        {
            return new System.Collections.Generic.List<EnumKey<Tag>>
            {
                Chinese,
                English,
                Malayu,
            };
        }
    }
}
