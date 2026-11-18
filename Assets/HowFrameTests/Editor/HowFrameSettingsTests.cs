using System.IO;
using NUnit.Framework;

/// <summary>
/// HowFrameSettings：路径解析、框架内置文件定位、默认目录与本工程布局一致
/// </summary>
public class HowFrameSettingsTests
{
    private static HowFrameSettings S => HowFrameSettings.instance;

    [Test]
    public void ToFullPath_RelativeIsUnderProjectRoot()
    {
        string full = HowFrameSettings.ToFullPath("Assets/StreamingAssets");
        Assert.AreEqual(Path.GetFullPath(Path.Combine(HowFrameSettings.ProjectRoot, "Assets/StreamingAssets")), full);
    }

    [Test]
    public void ToFullPath_EmptyMeansProjectRoot()
    {
        Assert.AreEqual(HowFrameSettings.ProjectRoot, HowFrameSettings.ToFullPath(""));
    }

    [Test]
    public void ToProjectRelative_RoundTrips()
    {
        string full = HowFrameSettings.ToFullPath("GameTable/Config");
        Assert.AreEqual("GameTable/Config", HowFrameSettings.ToProjectRelative(full));
    }

    [Test]
    public void ToProjectRelative_KeepsPathsOutsideProject()
    {
        string outside = Path.GetFullPath(Path.Combine(HowFrameSettings.ProjectRoot, "..", "SomewhereElse"));
        StringAssert.DoesNotStartWith("SomewhereElse", HowFrameSettings.ToProjectRelative(outside));
    }

    [TestCase("ExcelJson", "excel_json.py")]
    [TestCase("LanguageConfiger", "language_configer.py")]
    [TestCase("ScanScripts", "scan_scripts.py")]
    public void BuiltinPythonScripts_AreLocated(string folder, string file)
    {
        Assert.IsTrue(File.Exists(HowFrameSettings.BuiltinPythonScript(folder, file)),
            HowFrameSettings.BuiltinPythonScript(folder, file));
    }

    [TestCase("Template_Panel.cs.txt")]
    [TestCase("Template_View.cs.txt")]
    [TestCase("Template_Model.cs.txt")]
    [TestCase("Template_Item.cs.txt")]
    public void AutoUITemplates_AreLocated(string file)
    {
        string path = HowFrameSettings.ToFullPath(Path.Combine(S.ResolvedAutoUITemplateDir, file));
        Assert.IsTrue(File.Exists(path), path);
    }

    [Test]
    public void ConfiguredDirectories_ExistInThisProject()
    {
        foreach (var dir in new[]
                 {
                     S.enumJsonDir, S.enumCsDir, S.pathJsonDir, S.pathCsDir,
                     S.configExcelDir, S.configJsonDir, S.languageExcelDir, S.languageJsonDir,
                     S.addressableRootDir, S.pyToolOutputDir,
                 })
        {
            Assert.IsTrue(Directory.Exists(HowFrameSettings.ToFullPath(dir)), $"目录不存在: {dir}");
        }
    }

    [Test]
    public void LanguageJsonDir_MatchesRuntimeLangPath()
    {
        // 编辑器输出目录必须与运行时读取位置（StreamingAssets/GlobalPath.LangPath）一致
        string runtime = HowFrameSettings.ToFullPath("Assets/StreamingAssets/" + GlobalPath.LangPath);
        Assert.AreEqual(runtime, HowFrameSettings.ToFullPath(S.languageJsonDir));
    }
}
