using NUnit.Framework;

public class PathEditorTests
{
    [Test]
    public void Resolve_IndexReturnsConstantValue()
    {
        Assert.AreEqual(GlobalPath.LangPath, PathEditor.Resolve("GlobalPath.LangPath"));
    }

    [TestCase("a.png")]
    [TestCase("Assets/Art/a.png")]
    [TestCase("Assets\\Art\\a.png")]
    [TestCase("NoSuchType.Field")]
    [TestCase("")]
    public void Resolve_NonIndexReturnsInputUnchanged(string raw)
    {
        Assert.AreEqual(raw, PathEditor.Resolve(raw));
    }
}
