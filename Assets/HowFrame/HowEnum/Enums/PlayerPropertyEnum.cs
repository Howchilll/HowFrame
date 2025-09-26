namespace HowEnum
{
    public static class PlayerPropertyEnum
    {
        public class Tag { }
        public static readonly EnumKey<Tag> HP = new();
        public static readonly EnumKey<Tag> MP = new();
        public static readonly EnumKey<Tag> Stamina = new();
        public static class Weapon
        {
            public static readonly EnumKey<Tag> Katana = new();
            public static class Guns
            {
                public static readonly EnumKey<Tag> AK47 = new();
                public static readonly EnumKey<Tag> ScarH = new();
                public static readonly EnumKey<Tag> Magic = new();
            }
        }
    }
}
