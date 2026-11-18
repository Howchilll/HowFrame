using System.Collections.Generic;
using System.Linq;
using HowEnum;
using HowFrame;
using NUnit.Framework;
using UnityEngine;

public class ShaderManagerTests
{
    private const string Unlit = "Universal Render Pipeline/Unlit";
    private const string Lit = "Universal Render Pipeline/Lit";

    [TearDown]
    public void Cleanup() => ShaderManager.ReleaseAllMaterials();

    [Test]
    public void GetShader_FindsAndCachesByName()
    {
        Assert.IsTrue(ShaderManager.TryGetShader(Unlit, out var shader));
        Assert.AreEqual(Unlit, shader.name);
        Assert.IsTrue(ShaderManager.Shaders.ContainsKey(Unlit));
        Assert.IsFalse(ShaderManager.TryGetShader("No/Such/Shader", out _));
    }

    [Test]
    public void SetByMaterialKey_OnlyChangesThatMaterial()
    {
        var a = ShaderManager.CreateMaterial("A", Unlit);
        var b = ShaderManager.CreateMaterial("B", Unlit);

        Assert.AreEqual(1, ShaderManager.SetColor("A", "_BaseColor", Color.red));
        Assert.AreEqual(Color.red, a.GetColor("_BaseColor"));
        Assert.AreNotEqual(Color.red, b.GetColor("_BaseColor"));
    }

    [Test]
    public void SetByShaderName_ChangesEveryRegisteredMaterialUsingIt()
    {
        var a = ShaderManager.CreateMaterial("A", Unlit);
        var b = ShaderManager.CreateMaterial("B", Unlit);
        var other = ShaderManager.CreateMaterial("C", Lit);

        Assert.AreEqual(2, ShaderManager.SetFloat(Unlit, "_Cutoff", 0.25f));
        Assert.AreEqual(0.25f, a.GetFloat("_Cutoff"));
        Assert.AreEqual(0.25f, b.GetFloat("_Cutoff"));
        Assert.AreNotEqual(0.25f, other.GetFloat("_Cutoff"));
    }

    [Test]
    public void MissingPropertyIsSkipped()
    {
        ShaderManager.CreateMaterial("A", Unlit);
        Assert.AreEqual(0, ShaderManager.SetFloat("A", "_NoSuchProperty", 1f));
        Assert.AreEqual(0, ShaderManager.SetFloat("NoSuchKey", "_Cutoff", 1f));
    }

    [Test]
    public void TrySetFromString_ParsesByDeclaredType()
    {
        var m = ShaderManager.CreateMaterial("A", Unlit);

        Assert.IsTrue(ShaderManager.TrySetFromString("A", "_Cutoff", "0.75", out _));
        Assert.AreEqual(0.75f, m.GetFloat("_Cutoff"), 1e-5f);

        Assert.IsTrue(ShaderManager.TrySetFromString("A", "_BaseColor", "#FF0000", out _));
        Assert.AreEqual(Color.red, m.GetColor("_BaseColor"));

        Assert.IsTrue(ShaderManager.TrySetFromString("A", "_BaseColor", "0,1,0", out _));
        Assert.AreEqual(Color.green, m.GetColor("_BaseColor"));

        Assert.IsFalse(ShaderManager.TrySetFromString("A", "_Cutoff", "abc", out var error));
        StringAssert.Contains("_Cutoff", error);
        Assert.IsFalse(ShaderManager.TrySetFromString("A", "_Nope", "1", out _));
    }

    [Test]
    public void Clone_CopiesValuesAndIsIndependent()
    {
        var source = ShaderManager.CreateMaterial("Src", Unlit);
        source.SetColor("_BaseColor", Color.blue);

        var clone = ShaderManager.CloneMaterial("Copy", "Src");
        Assert.AreEqual(Color.blue, clone.GetColor("_BaseColor"));

        ShaderManager.SetColor("Copy", "_BaseColor", Color.yellow);
        Assert.AreEqual(Color.blue, source.GetColor("_BaseColor"));

        var fromShader = ShaderManager.CloneMaterial("FromShader", Unlit);
        Assert.AreEqual(Unlit, fromShader.shader.name);
    }

    [Test]
    public void Release_DestroysOwnedButNotRegistered()
    {
        var owned = ShaderManager.CreateMaterial("Owned", Unlit);
        var external = new Material(Shader.Find(Unlit));
        ShaderManager.RegisterMaterial("External", external);

        ShaderManager.ReleaseAllMaterials();

        Assert.IsTrue(owned == null);
        Assert.IsFalse(external == null);
        Assert.IsFalse(ShaderManager.HasMaterial("Owned"));
        Object.DestroyImmediate(external);
    }

    [Test]
    public void Keywords()
    {
        var m = ShaderManager.CreateMaterial("A", Lit);
        ShaderManager.SetKeyword("A", "_NORMALMAP", true);
        Assert.IsTrue(m.IsKeywordEnabled("_NORMALMAP"));
        ShaderManager.SetKeyword("A", "_NORMALMAP", false);
        Assert.IsFalse(m.IsKeywordEnabled("_NORMALMAP"));
    }

    [Test]
    public void ShaderProperty_ConvertsFromString()
    {
        ShaderProperty p = "_BaseColor";
        Assert.AreEqual(Shader.PropertyToID("_BaseColor"), p.Id);
        Assert.AreEqual(new ShaderProperty("_BaseColor"), p);
    }

    [TestCase("_BaseColor", false, "BaseColor")]
    [TestCase("base map", false, "BaseMap")]
    [TestCase("2D", false, "_2D")]
    [TestCase("float", true, "@float")]
    [TestCase("_NORMALMAP", true, "_NORMALMAP")]
    public void Generator_ToIdentifier(string raw, bool keepCase, string expected)
    {
        Assert.AreEqual(expected, ShaderKeysGenerator.ToIdentifier(raw, keepCase));
    }

    [Test]
    public void Generator_ClassNamesDisambiguateClashes()
    {
        var names = ShaderKeysGenerator.AssignClassNames(new List<string>
        {
            "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit", "Custom/Water",
        });
        CollectionAssert.AreEqual(new[] { "Unlit", "ParticlesUnlit", "Water" }, names);
    }

    [Test]
    public void Generator_EmitsPropertiesAndKeywords()
    {
        var infos = new List<ShaderKeysGenerator.ShaderInfo>
        {
            new ShaderKeysGenerator.ShaderInfo { Shader = Shader.Find(Lit) },
        };
        string code = ShaderKeysGenerator.GenerateCode(infos, "HowEnum");
        StringAssert.Contains("public static class Lit", code);
        StringAssert.Contains($"public const string ShaderName = \"{Lit}\";", code);
        StringAssert.Contains("public static readonly ShaderProperty BaseColor = new(\"_BaseColor\");", code);
        StringAssert.Contains("public const string NORMALMAP = \"_NORMALMAP\";", code);
        Assert.IsFalse(code.Contains("class GlobalKeywords"), "内置 Shader 默认不生成全局关键字");
    }
}
