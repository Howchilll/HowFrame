namespace HowEnum
{
    public static class TestttEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> TTT = new("TTT");
        public static readonly EnumKey<Tag> SSSS = new("SSSS");
        public static class TS
        {
            public static readonly EnumKey<Tag> ttt = new("TS.ttt");
            public static readonly EnumKey<Tag> sss = new("TS.sss");
        }

        /// <summary>
        /// 将字符串转换为EnumKey
        /// </summary>
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "TTT": return TTT;
                case "SSSS": return SSSS;
                    case "TS.ttt": return TS.ttt;
                    case "TS.sss": return TS.sss;
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
                TTT,
                SSSS,
                TS.ttt,
                TS.sss,
            };
        }
    }
}
