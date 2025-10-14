global using static HowMacro;

public static class HowMacro
{
    public static T @SET_GET<T>() { return default(T);}
    public static int @CLASS_COUNT;
}