namespace HowEnum
{
    public static class EnumTestEnum
    {
        public class Tag { }
        
        public static readonly EnumKey<Tag> Test1 = new();
        public static readonly EnumKey<Tag> Test2 = new();
        public static readonly EnumKey<Tag> Test3 = new();
    }
}
