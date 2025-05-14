using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DataManager
{
    public static void GlobalSave()
    {
        GlobalData.Write();
    }

    public static void GlobalLoad()
    {
        GlobalData.Wake();
    }

    public static void ArchiveSave()
    {
        ArchiveData.SaveArchive();
    }

    public static void ArchiveLoad()
    {
        ArchiveData.InitData();
    }



    public static void Wake()
    {

    }
}

// 初始化全局，初始化按键，初始化语言