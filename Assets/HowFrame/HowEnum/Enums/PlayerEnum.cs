namespace HowEnum
{
    public static class PlayerEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> HP = new();
        public static readonly EnumKey<Tag> SP = new();
        public static readonly EnumKey<Tag> EXE = new();
        public static class Body
        {
            public static readonly EnumKey<Tag> Hight = new();
            public static readonly EnumKey<Tag> Weight = new();
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
                default: throw new System.ArgumentException($"Unknown value: {value}");
            }
        }

        /// <summary>
        /// 将EnumKey转换为字符串
        /// </summary>
        public static string Convert(EnumKey<Tag> enumKey)
        {
            if (enumKey == null) return null;

            if (enumKey == HP) return "HP";
            if (enumKey == SP) return "SP";
            if (enumKey == EXE) return "EXE";
                if (enumKey == Body.Hight) return "Body.Hight";
                if (enumKey == Body.Weight) return "Body.Weight";
            throw new System.ArgumentException($"Unknown enumKey: {enumKey}");
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
            };
        }
    }
}
