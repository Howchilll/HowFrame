using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static DataAssitant;

public static class GlobalData 
{
    public static float MusicVol{get=>_record.MusicVol;set=>_record.MusicVol=value;}
    public static float SoundVol{get=>_record.SoundVol;set=>_record.SoundVol=value;} 
    public static string Language{get=>_record.Language;set=>_record.Language=value;}
    public static float MouseSensX{get=>_record.MouseSensX;set=>_record.MouseSensX=value;}
    public static float MouseSensY{get=>_record.MouseSensY;set=>_record.MouseSensY=value;}
    public static short LastSaveIndex{get=>_record.LastArchive;set=>_record.LastArchive=value;}
    public static IDictionary<int, string>  Archives=>_record.Archives;
    public static IDictionary<string, bool> Achivements=>_record.Achivements;
    public static IDictionary<string, bool> GlobalFlags=>_record.GlobalFlags;
    
    private static GlobalRecord _record;
    private static GlobalConfig _config;
    
    static GlobalData()
    {
        InitData();
    }
    private static void InitData()
    {
        if (!File.Exists(Path.Combine(Application.persistentDataPath, "GlobalData.dat")))
        {
            _config = LoadConfig<GlobalConfig>("Configs/GlobalConfig");
            _record = new GlobalRecord(
                _config.Achivements
            );
            Write(); 
        }else Load();  
    }
    
    internal static void Write() { WriteData(_record, "GlobalData"); }
    private static void Load() { _record = ReadData<GlobalRecord>("GlobalData"); }
    public static void JsonSave() { WriteData(_record, "GlobalData",true); }
    internal static void Wake(){}//用于主动唤醒
}
