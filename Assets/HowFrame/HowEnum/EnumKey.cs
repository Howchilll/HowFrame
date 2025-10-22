namespace HowEnum
{
    public record EnumKeyBase{}

    public record EnumKey<TTag> : EnumKeyBase
    {
        internal EnumKey()
        {
            
        }
    }
    
}
