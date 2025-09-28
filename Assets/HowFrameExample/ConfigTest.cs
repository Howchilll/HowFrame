using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemConfig
{
    public string ItemName;
    public int Power;
}

[Serializable]
public class PlayerConfig
{
    public string PlayerName;
    public int Level;
    public List<ItemConfig> Inventory;   // List 嵌套测试
}

[Serializable]
public class GameConfig
{
    public string GameName;
    public List<PlayerConfig> Players;   // List 嵌套 PlayerConfig
}