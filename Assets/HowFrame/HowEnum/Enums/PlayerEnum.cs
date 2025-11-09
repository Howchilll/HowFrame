namespace HowEnum
{
    public static class PlayerEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> HP = new("HP");
        public static readonly EnumKey<Tag> SP = new("SP");
        public static readonly EnumKey<Tag> EXE = new("EXE");
        public static class Body
        {
            public static readonly EnumKey<Tag> Hight = new("Body.Hight");
            public static readonly EnumKey<Tag> Weight = new("Body.Weight");
            public static readonly EnumKey<Tag> YYP = new("Body.YYP");
        }

        /// <summary>
        /// 将字符串转换为EnumKey
        /// </summary>
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "HP": return HP;
                case "SP": return SP;
                case "EXE": return EXE;
                    case "Body.Hight": return Body.Hight;
                    case "Body.Weight": return Body.Weight;
                    case "Body.YYP": return Body.YYP;
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
                HP,
                SP,
                EXE,
                Body.Hight,
                Body.Weight,
                Body.YYP,
            };
        }
    }
}
