namespace HowEnum
{
    public record EnumKeyBase
    {
        public string name;
    }

    public record EnumKey<TTag> : EnumKeyBase
    {
        internal EnumKey(string name)
        {
            this.name = name;
        }
        
        internal EnumKey()
        {
            
        }
        
        
    }
    
}
