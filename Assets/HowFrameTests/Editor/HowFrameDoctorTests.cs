using System.Collections.Generic;
using System.IO;
using System.Linq;
using HowFrame;
using NUnit.Framework;

/// <summary>健康检查、缺失 key 记录、语言表追加</summary>
public class HowFrameDoctorTests
{
    private string _dir;

    [SetUp]
    public void Setup()
    {
        _dir = Path.Combine(Path.GetTempPath(), "HowFrameDoctorTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        LangManager.ClearMissingKeys();
    }

    private static bool Has(IEnumerable<HowFrameDoctor.Issue> issues, HowFrameDoctor.Level level) => issues.Any(i => i.Level == level);

    [Test]
    public void EnumSync_DetectsOutOfDateCode()
    {
        string json = Path.Combine(_dir, "json"), cs = Path.Combine(_dir, "cs");
        Directory.CreateDirectory(json);
        Directory.CreateDirectory(cs);
        File.WriteAllText(Path.Combine(json, "FooEnum.json"), "{\"collectionName\":\"FooEnum\",\"elements\":[\"A\",\"B\"]}");
        File.WriteAllText(Path.Combine(cs, "FooEnum.cs"),
            "public static readonly EnumKey<Tag> A = new(\"A\");\npublic static readonly EnumKey<Tag> C = new(\"C\");");

        var issues = HowFrameDoctor.CheckEnumSync(json, cs).ToList();
        var warning = issues.Single(i => i.Level == HowFrameDoctor.Level.Warning);
        StringAssert.Contains("B", warning.Message);
        StringAssert.Contains("C", warning.Message);
    }

    [Test]
    public void EnumSync_InSyncIsOk()
    {
        string json = Path.Combine(_dir, "json"), cs = Path.Combine(_dir, "cs");
        Directory.CreateDirectory(json);
        Directory.CreateDirectory(cs);
        File.WriteAllText(Path.Combine(json, "FooEnum.json"), "{\"collectionName\":\"FooEnum\",\"elements\":[\"A\"]}");
        File.WriteAllText(Path.Combine(cs, "FooEnum.cs"), "public static readonly EnumKey<Tag> A = new(\"A\");");

        var issues = HowFrameDoctor.CheckEnumSync(json, cs).ToList();
        Assert.IsFalse(Has(issues, HowFrameDoctor.Level.Warning));
        Assert.IsTrue(Has(issues, HowFrameDoctor.Level.Ok));
    }

    [Test]
    public void EnumSync_SupportsGroups()
    {
        string json = Path.Combine(_dir, "json"), cs = Path.Combine(_dir, "cs");
        Directory.CreateDirectory(json);
        Directory.CreateDirectory(cs);
        File.WriteAllText(Path.Combine(json, "GEnum.json"),
            "{\"collectionName\":\"GEnum\",\"elements\":[\"HP\",{\"groupName\":\"Body\",\"children\":[\"H\"]}]}");
        File.WriteAllText(Path.Combine(cs, "GEnum.cs"),
            "public static readonly EnumKey<Tag> HP = new(\"HP\");\npublic static readonly EnumKey<Tag> H = new(\"Body.H\");");

        var issues = HowFrameDoctor.CheckEnumSync(json, cs).ToList();
        Assert.IsTrue(issues.All(i => i.Level == HowFrameDoctor.Level.Ok), string.Join("\n", issues));
    }

    [Test]
    public void EnumSync_ProjectEnumsAreInSync()
    {
        var s = HowFrameSettings.instance;
        var issues = HowFrameDoctor.CheckEnumSync(HowFrameSettings.ToFullPath(s.enumJsonDir), HowFrameSettings.ToFullPath(s.enumCsDir)).ToList();
        Assert.IsFalse(Has(issues, HowFrameDoctor.Level.Error), string.Join("\n", issues));
    }

    [Test]
    public void LanguageFiles_DetectsMissingFileAndKeyMismatch()
    {
        File.WriteAllText(Path.Combine(_dir, "Chinese_UI_Lang.json"), "{\"a\":\"甲\",\"b\":\"乙\"}");
        File.WriteAllText(Path.Combine(_dir, "English_UI_Lang.json"), "{\"a\":\"A\"}");

        var issues = HowFrameDoctor.CheckLanguageFiles(_dir, new[] { "Chinese", "English", "Malayu" }, new[] { "UI" }).ToList();
        Assert.IsTrue(issues.Any(i => i.Level == HowFrameDoctor.Level.Error && i.Message.Contains("Malayu_UI")));
        Assert.IsTrue(issues.Any(i => i.Level == HowFrameDoctor.Level.Warning && i.Message.Contains("English_UI") && i.Message.Contains("b")));
    }

    [Test]
    public void DuplicateNames_OnlyWhenLabelsOverlap()
    {
        var items = new List<HowFrameDoctor.AddressableItem>
        {
            new HowFrameDoctor.AddressableItem("Assets/A/Hit.wav", new[] { "Audio" }),
            new HowFrameDoctor.AddressableItem("Assets/B/Hit.prefab", new[] { "Prefab" }),
            new HowFrameDoctor.AddressableItem("Assets/C/Hit.ogg", new[] { "Audio", "UI" }),
        };
        var warnings = HowFrameDoctor.CheckDuplicateNames(items).Where(i => i.Level == HowFrameDoctor.Level.Warning).ToList();
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains("Assets/C/Hit.ogg", warnings[0].Message);
    }

    [Test]
    public void InstanceAssets_RequiresInstanceLabel()
    {
        var items = new List<HowFrameDoctor.AddressableItem>
        {
            new HowFrameDoctor.AddressableItem("Assets/Canvas.prefab", new[] { HowInit.InstanceLabel }),
            new HowFrameDoctor.AddressableItem("Assets/HowInputActions.inputactions", new[] { "Other" }),
        };
        var issues = HowFrameDoctor.CheckInstanceAssets(items).ToList();
        Assert.IsTrue(issues.Any(i => i.Level == HowFrameDoctor.Level.Ok && i.Message.StartsWith("Canvas")));
        Assert.IsTrue(issues.Any(i => i.Level == HowFrameDoctor.Level.Error && i.Message.StartsWith("HowInputActions")));
    }

    [Test]
    public void EnumKeysOf_ReadsAllStaticKeys()
    {
        var keys = HowFrameDoctor.EnumKeysOf<HowEnum.LangTypeEnum.Tag>(typeof(HowEnum.LangTypeEnum));
        CollectionAssert.AreEquivalent(HowEnum.LangTypeEnum.GetAll().Select(k => k.name), keys.Select(k => k.name));
    }

    [Test]
    public void LangManager_RecordsMissingKeysByModule()
    {
        LangManager.ClearMissingKeys();
        LangManager.GetLangContent("__missing_a__");
        LangManager.GetLangContent("__missing_a__");
        LangManager.GetLangContent("__missing_b__");

        Assert.AreEqual(2, LangManager.MissingCount);
        Assert.IsTrue(LangManager.MissingKeys["Default"].Contains("__missing_a__"));

        LangManager.ClearMissingKeys();
        Assert.AreEqual(0, LangManager.MissingCount);
    }

    [Test]
    public void AppendKeys_AddsOnlyNewKeysAndKeepsExisting()
    {
        string xlsx = Path.Combine(_dir, "English_UI_Lang.xlsx");
        LanguageExcelSerializer.UpdateExcel(xlsx, new Dictionary<string, string> { { "hello", "" } });

        Assert.AreEqual(2, LanguageExcelSerializer.AppendKeys(xlsx, new[] { "hello", "world", "zzz", "world" }));
        Assert.AreEqual(0, LanguageExcelSerializer.AppendKeys(xlsx, new[] { "world" }));

        var read = LanguageExcelSerializer.ReadFromExcel(xlsx);
        CollectionAssert.AreEquivalent(new[] { "hello", "world", "zzz" }, read.Keys);
    }

    [Test]
    public void AppendKeys_CreatesMissingFile()
    {
        string xlsx = Path.Combine(_dir, "sub", "Chinese_UI_Lang.xlsx");
        Assert.AreEqual(1, LanguageExcelSerializer.AppendKeys(xlsx, new[] { "k" }));
        CollectionAssert.AreEquivalent(new[] { "k" }, LanguageExcelSerializer.ReadFromExcel(xlsx).Keys);
    }
}
