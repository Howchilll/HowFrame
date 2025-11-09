namespace HowEnum
{
    public record EnumKeyBase{}

    public record EnumKey<TTag> : EnumKeyBase
    {
        public string name;
        internal EnumKey(string name)
        {
            this.name = name;
        }
        
        internal EnumKey()
        {
            
        }
        
        
    }
    
}
