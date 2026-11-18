using System.Linq;
using HowFrame;
using NUnit.Framework;

public class HowConsoleTests
{
    public enum Mode { Easy, Hard }

    private static int _lastAmount;
    private static string _lastText;
    private static Mode _lastMode;

    [HowCommand("test.add", "测试命令")]
    private static int TestAdd(int a, int b = 10) => a + b;

    [HowCommand("test.say")]
    private static void TestSay(int amount, string text)
    {
        _lastAmount = amount;
        _lastText = text;
    }

    [HowCommand("test.mode")]
    private static void TestMode(Mode mode, bool flag = false) => _lastMode = mode;

    [SetUp]
    public void Setup() => HowConsole.Clear();

    [Test]
    public void Tokenize_HandlesQuotes()
    {
        CollectionAssert.AreEqual(new[] { "say", "hello world", "x", "" }, HowConsole.Tokenize("say  \"hello world\" x \"\""));
    }

    [Test]
    public void AttributeCommand_ConvertsArgsAndUsesDefaults()
    {
        Assert.IsTrue(HowConsole.Execute("test.add 5"));
        Assert.AreEqual("15", HowConsole.Lines.Last().Text);
        Assert.IsTrue(HowConsole.Execute("TEST.ADD 5 1"));
        Assert.AreEqual("6", HowConsole.Lines.Last().Text);
    }

    [Test]
    public void LastStringParameterTakesRemainingArgs()
    {
        Assert.IsTrue(HowConsole.Execute("test.say 3 hello big world"));
        Assert.AreEqual(3, _lastAmount);
        Assert.AreEqual("hello big world", _lastText);
    }

    [Test]
    public void EnumAndBoolParameters()
    {
        Assert.IsTrue(HowConsole.Execute("test.mode hard on"));
        Assert.AreEqual(Mode.Hard, _lastMode);
        Assert.IsFalse(HowConsole.Execute("test.mode nightmare"));
    }

    [Test]
    public void BadInputReportsErrorWithoutThrowing()
    {
        Assert.IsFalse(HowConsole.Execute("test.add abc"));
        Assert.AreEqual(HowConsole.LineType.Error, HowConsole.Lines.Last().Type);
        Assert.IsFalse(HowConsole.Execute("test.add"));
        Assert.IsFalse(HowConsole.Execute("no.such.command"));
        StringAssert.Contains("未知命令", HowConsole.Lines.Last().Text);
    }

    [Test]
    public void ManualRegisterAndComplete()
    {
        HowConsole.Register("zz.ping", "", args => (object)("pong " + args.Length));
        try
        {
            Assert.IsTrue(HowConsole.Execute("zz.ping a b"));
            Assert.AreEqual("pong 2", HowConsole.Lines.Last().Text);
            CollectionAssert.Contains(HowConsole.Complete("zz.").ToList(), "zz.ping");
        }
        finally
        {
            HowConsole.Unregister("zz.ping");
        }
    }

    [Test]
    public void HandlerExceptionBecomesErrorLine()
    {
        HowConsole.Register("zz.boom", "", (System.Action<string[]>)(_ => throw new System.InvalidOperationException("boom")));
        try
        {
            Assert.IsFalse(HowConsole.Execute("zz.boom"));
            Assert.AreEqual("boom", HowConsole.Lines.Last().Text);
        }
        finally
        {
            HowConsole.Unregister("zz.boom");
        }
    }
}
