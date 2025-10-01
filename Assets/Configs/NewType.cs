using System;
using System.Collections.Generic;
using MessagePack;
[Serializable]
[MessagePackObject(AllowPrivate = true)]
public struct NewType
{
    [Key(0)] public int Field1;
    [Key(1)] public int Field2;
    [Key(2)] public List<int> Field3;

    public NewType(int field1, int field2, List<int> field3)
    {
        Field1 = field1;
        Field2 = field2;
        Field3 = field3;
    }
}
