namespace HowEnum
{
    public static class UINameEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> TestPanel = new("TestPanel");
        public static readonly EnumKey<Tag> ExamplePanel = new("ExamplePanel");

        /// <summary>
        /// 将字符串转换为EnumKey
        /// </summary>
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "TestPanel": return TestPanel;
                case "ExamplePanel": return ExamplePanel;
                default: throw new System.ArgumentException($"Unknown value: {value}");
            }
        }

        /// <summary>
        /// 将EnumKey转换为字符串
        /// </summary>
        public static string Convert(EnumKey<Tag> enumKey)
        {
            if (enumKey == null) return null;

            if (enumKey == TestPanel) return "TestPanel";
            if (enumKey == ExamplePanel) return "ExamplePanel";
            throw new System.ArgumentException($"Unknown enumKey: {enumKey}");
        }

        /// <summary>
        /// 获取所有枚举项
        /// </summary>
        public static System.Collections.Generic.List<EnumKey<Tag>> GetAll()
        {
            return new System.Collections.Generic.List<EnumKey<Tag>>
            {
                TestPanel,
                ExamplePanel,
            };
        }
    }
}
