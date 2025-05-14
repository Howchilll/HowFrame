using System.Collections.Generic;
using MessagePack;
using UnityEngine;

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