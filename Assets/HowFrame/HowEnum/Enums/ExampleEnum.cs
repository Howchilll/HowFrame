namespace HowEnum
{
    public static class ExampleEnum
    {
        public class Tag { }
        public static readonly EnumKey<Tag> example1 = new();
        public static readonly EnumKey<Tag> example2 = new();
        
        public static class SubExample
        {
            public static readonly EnumKey<Tag> example3 = new();
            public static readonly EnumKey<Tag> example4 = new();
        }
        
        public class exampleEnum{}
    }
}