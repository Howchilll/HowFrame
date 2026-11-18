using System.Collections.Generic;
using System.IO;
using HowEnum;
using HowFrame;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 运行时助手中不依赖场景 / Addressables 的部分
/// </summary>
public class HowFrameRuntimeTests
{
    private const string TestDir = "HowFrameTests";

    public class SaveSample
    {
        public int Level;
        public string Name;
        public List<int> Items;
    }

    [TearDown]
    public void Cleanup()
    {
        string dir = Path.Combine(Application.persistentDataPath, TestDir);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DataAssistant_RoundTripsPlainClass(bool isJson)
    {
        var data = new SaveSample { Level = 7, Name = "how", Items = new List<int> { 1, 2, 3 } };
        DataAssistant.WriteData(data, "sample", isJson, TestDir);

        Assert.IsTrue(DataAssistant.HasData("sample", isJson, TestDir));
        var read = DataAssistant.ReadData<SaveSample>("sample", isJson, TestDir);
        Assert.AreEqual(7, read.Level);
        Assert.AreEqual("how", read.Name);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, read.Items);
    }

    [Test]
    public void DataAssistant_OverwriteReplacesAndLeavesNoTempFile()
    {
        DataAssistant.WriteData(new SaveSample { Level = 1 }, "slot", false, TestDir);
        DataAssistant.WriteData(new SaveSample { Level = 2 }, "slot", false, TestDir);

        Assert.AreEqual(2, DataAssistant.ReadData<SaveSample>("slot", false, TestDir).Level);
        string dir = Path.Combine(Application.persistentDataPath, TestDir);
        Assert.IsEmpty(Directory.GetFiles(dir, "*.tmp"));
    }

    [Test]
    public void DataAssistant_DeleteData()
    {
        DataAssistant.WriteData(new SaveSample(), "gone", true, TestDir);
        Assert.IsTrue(DataAssistant.DeleteData("gone", true, TestDir));
        Assert.IsFalse(DataAssistant.HasData("gone", true, TestDir));
        Assert.IsFalse(DataAssistant.DeleteData("gone", true, TestDir));
    }

    [Test]
    public void DataAssistant_MissingFileReturnsDefault()
    {
        Assert.IsNull(DataAssistant.ReadData<SaveSample>("nope", false, TestDir));
    }

    [Test]
    public void LangManager_MissingKeyFallsBackToKey()
    {
        LangManager.Wake();
        Assert.AreEqual("__missing_key__", LangManager.GetLangContent(LangModuleEnum.UI, "__missing_key__"));
        Assert.AreEqual("__missing_default__", LangManager.GetLangContent("__missing_default__"));
        Assert.IsFalse(LangManager.TryGetLangContent(LangModuleEnum.UI, "__missing_key__", out _));
    }

    [Test]
    public void EnumKey_ConvertRoundTripsAndCompareByValue()
    {
        Assert.AreSame(LangTypeEnum.English, LangTypeEnum.Convert("English"));
        Assert.AreEqual(LangTypeEnum.Chinese, LangTypeEnum.Convert(LangTypeEnum.Convert(LangTypeEnum.Chinese)));
        Assert.AreNotEqual(LangTypeEnum.Chinese, LangTypeEnum.English);
    }

    [Test]
    public void UIManager_BackOnEmptyStackReturnsFalse()
    {
        UIManager.ClearStack();
        Assert.IsFalse(UIManager.Back());
        Assert.AreEqual(0, UIManager.StackCount);
        Assert.IsNull(UIManager.StackTop);
    }
}
