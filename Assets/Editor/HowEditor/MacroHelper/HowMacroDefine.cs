using System;
using System.Collections.Generic;

#if UNITY_EDITOR
public static class HowMacroDefine
{
    private static Dictionary<string, Func<string, string>> MacroDic=new();
    public static string SET_GET(string line) // public int hp = @SET_GET<int>(GameData.hp) 
    {
        
        return line;
    }

   static HowMacroDefine()
   {
       MacroDic["@SET_GET"] = SET_GET;
   }
    
    
    
    
    
    
    
    
}

#endif  