using System;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
[Serializable]
public struct PlayerData
{
    [Key(0)] public int Hp;
    [Key(1)] public int Atk;
    [Key(2)] public string Name;
    [Key(3)] public bool Gender;

    public PlayerData(int hp, int atk, string name, bool gender)
    {
        Hp = hp;
        Atk = atk;
        Name = name;
        Gender = gender;
    }
}
