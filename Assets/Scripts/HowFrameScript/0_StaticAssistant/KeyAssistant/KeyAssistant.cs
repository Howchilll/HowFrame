using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static DataAssitant;

public static class KeyAssistant
{
    public static Dictionary<string, KeyCode> Keys = new Dictionary<string, KeyCode>();

    static KeyAssistant() { LoadKeyData(); }

    private static void LoadKeyData()
    {
        Keys.Clear();
        string path = Path.Combine(Application.persistentDataPath, "KeyData.dat");
        Debug.Log(path);
        Dictionary<string, string> rawData = File.Exists(path)
            ? ReadData<Dictionary<string, string>>("KeyData")
            : LoadConfig<Dictionary<string, string>>("Configs/keyConfig");

        foreach (var pair in rawData)
        {
            Keys[pair.Key] = Enum.TryParse(pair.Value, out KeyCode parsedKey) ? parsedKey : KeyCode.None;
        }

        if (!File.Exists(path)) WriteData(rawData, "KeyData");
    }

    public static void ChangeKey(string action, KeyCode newKey)
    { 
        Keys[action] = newKey;
    }

    public static void SaveKey()
    {
        var saveData = new Dictionary<string, string>();
        foreach (var pair in Keys)
        {
            saveData[pair.Key] = pair.Value.ToString();
        }
        WriteData(saveData, "KeyData");
    }

    public static void CancelChangeKey()
    {
        LoadKeyData();
    }

    public static void DefaultSet()
    {
        var rawData = LoadConfig<Dictionary<string, string>>("Configs/keyConfig");
        Keys.Clear();
        foreach (var kv in rawData)
        {
            if (Enum.TryParse(kv.Value, out KeyCode parsedKey))
            {
                Keys[kv.Key] = parsedKey;
            }
            else
            {
                Keys[kv.Key] = KeyCode.None; 
            }
        }

        SaveKey();
    }
    
    public static void wake(){}
}