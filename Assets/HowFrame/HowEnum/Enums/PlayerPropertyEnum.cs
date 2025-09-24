namespace HowEnum
{
    public static class PlayerPropertyEnum
    {
        public static readonly EnumKey HP = new();
        public static readonly EnumKey MP = new();
        public static readonly EnumKey Stamina = new();
        public static class Weapon
        {
            public static readonly EnumKey Katana = new();
            public static class Guns
            {
                public static readonly EnumKey AK47 = new();
                public static readonly EnumKey ScarH = new();
                public static readonly EnumKey Magic = new();
            }
        }
    }
}
