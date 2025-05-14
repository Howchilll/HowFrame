using System.Collections.Generic;
using UnityEngine;
using MessagePack;


[MessagePackObject(AllowPrivate = true)]
internal struct GlobalRecord
{
    [Key(0)] public float MusicVol;
    [Key(1)] public float SoundVol;
    [Key(2)] public string Language;
    [Key(3)] public float MouseSensX;
    [Key(4)] public float MouseSensY;

    [Key(5)] public Dictionary<int, string> Archives;
    [Key(6)] public Dictionary<string, bool> Achivements;
    [Key(7)] public short LastArchive;
    [Key(8)] public Dictionary<string, bool> GlobalFlags;
    
    public GlobalRecord(Dictionary<string, bool>achivements)
    {
        MusicVol = 0.8f;
        SoundVol = 0.8f;
        Language = "English";
        MouseSensX = 1.0f;
        MouseSensY = 1.0f;
        Archives = new Dictionary<int, string>()
        {
            [1]="",
            [2]="",
            [3]="",
        };
        Achivements = achivements;
        LastArchive = 0;
        GlobalFlags = new Dictionary<string, bool>()
        {
            ["set1"] = true,
            ["set2"] = true,
            ["set3"] = true,
        };
    }
}

internal struct GlobalConfig
{
    public Dictionary<string, bool> Achivements;
}