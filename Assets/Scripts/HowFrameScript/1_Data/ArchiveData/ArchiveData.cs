using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static DataAssitant;
using static GlobalData;
public static class ArchiveData 
{
   public static float Hp { get => _record.Hp; set => _record.Hp = value; }   
   public static float MaxHp { get => _record.MaxHp; set => _record.MaxHp = value; }

   
   private static ArchiveRecord _record;
   private static ArchiveConfig _config;
   
   public static void InitData()
   {
      if (File.Exists( Application.persistentDataPath+ "ArchiveConfig.dat"))
      {
         _config = LoadConfig<ArchiveConfig>("Configs/ArchiveConfig");

         _record = new ArchiveRecord(
            _config.MaxHP
         );
         Write(); 
      }else Load();    
   }
   
   internal static void SaveArchive()
   {
      Write();
   }
   
   internal static void Write() { WriteData(_record, "ArchiveData"); }
   private static void Load() { _record = ReadData<ArchiveRecord>("ArchiveData"); }
   public static void JsonSave() { WriteData(_record, "ArchiveData",true); }

}
