using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MissingReferenceScannerTests
{
    private const string Dir = "Assets/HowFrameTests/__MissingRefTemp";

    [SetUp]
    public void Setup()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/HowFrameTests", "__MissingRefTemp");
    }

    [TearDown]
    public void Cleanup()
    {
        AssetDatabase.DeleteAsset(Dir);
    }

    [Test]
    public void DetectsReferenceToDeletedAsset_ButNotNone()
    {
        var mat = new Material(Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Standard"));
        AssetDatabase.CreateAsset(mat, Dir + "/Temp.mat");

        var go = new GameObject("Root");
        var child = new GameObject("Child");
        child.transform.SetParent(go.transform);
        child.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshRenderer>(); // 没赋值（None）不算丢失
        PrefabUtility.SaveAsPrefabAsset(go, Dir + "/Temp.prefab");
        Object.DestroyImmediate(go);

        Assert.IsEmpty(MissingReferenceScanner.ScanAsset(Dir + "/Temp.prefab"));

        AssetDatabase.DeleteAsset(Dir + "/Temp.mat");
        var findings = MissingReferenceScanner.ScanAsset(Dir + "/Temp.prefab");

        Assert.AreEqual(1, findings.Count, string.Join("\n", findings));
        Assert.AreEqual(MissingReferenceScanner.Kind.MissingReference, findings[0].Kind);
        Assert.AreEqual("Temp/Child", findings[0].ObjectPath); // Prefab 根节点名 = 文件名
        Assert.AreEqual("MeshRenderer", findings[0].Component);
    }

    [Test]
    public void ScanAssets_RespectsFolder()
    {
        var go = new GameObject("Clean");
        PrefabUtility.SaveAsPrefabAsset(go, Dir + "/Clean.prefab");
        Object.DestroyImmediate(go);
        Assert.IsFalse(MissingReferenceScanner.ScanAssets(Dir).Any());
    }
}
