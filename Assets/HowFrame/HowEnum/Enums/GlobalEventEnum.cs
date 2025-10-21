namespace HowEnum
{
    public static class GlobalEventEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> InputTypeChange = new();
        public static readonly EnumKey<Tag> LanguageChange = new();
        public static readonly EnumKey<Tag> StartGame = new();
    }
}
