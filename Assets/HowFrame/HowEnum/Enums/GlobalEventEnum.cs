namespace HowEnum
{
    public static class GlobalEventEnum
    {
        public class Tag { }

        //old
        public static readonly EnumKey<Tag> InputTypeChange = new("InputTypeChange");
        public static readonly EnumKey<Tag> LanguageChange = new("LanguageChange");
        public static readonly EnumKey<Tag> StartGame = new("StartGame");

        

    }
}
