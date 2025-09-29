using System.Collections.Generic;
using MessagePack;
using UnityEngine;
namespace HowFrame
{

[MessagePackObject(AllowPrivate = true)]
internal struct ArchiveRecord
{
    [Key(0)] public float Hp;
    [Key(1)] public float MaxHp;

    public ArchiveRecord(float hp)
    {
        Hp = hp;
        MaxHp = hp;
    
    }
}



internal struct ArchiveConfig
{
    public float MaxHP;
}
}



public struct ArchiveRecord
{
    public float Hp;
    public float Mp;

    public ArchiveRecord(float hp, float mp)
    {
        Hp = hp;
        Mp = hp;
    
    }
}
