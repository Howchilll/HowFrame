#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BikePreviewCaptureTool
{
    private const int ImageSize = 512;
    private const string ModelPathFormat = "Assets/GameRes/Prefab/Model/Model{0}.prefab";
    private const string SpritePathFormat = "Assets/GameRes/Sprite/BikePreview{0}.png";
    private const string ConfigPathFormat = "Assets/GameRes/ScriptableObject/BikeConfig{0}.asset";

    [MenuItem("Tools/DMT/Capture Bike UI Previews")]
    public static void CaptureAll()
    {
        var captureScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        try
        {
            for (var id = 1; id <= 10; id++)
                CaptureOne(id);
        }
        finally
        {
            if (captureScene.IsValid())
                EditorSceneManager.CloseScene(captureScene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CaptureOne(int id)
    {
        var modelPath = string.Format(ModelPathFormat, id);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Bike preview capture skipped: missing {modelPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        HideRiderRenderers(instance);

        try
        {
            var bounds = CalculateBounds(instance);
            NormalizeModel(instance, bounds);
            bounds = CalculateBounds(instance);

            var texture = RenderPreview(instance, bounds);
            var spritePath = string.Format(SpritePathFormat, id);
            File.WriteAllBytes(spritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            ConfigureSprite(spritePath);
            AssignConfigSprite(id, spritePath);
            Debug.Log($"Captured bike preview {id}: {spritePath}");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void HideRiderRenderers(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var current = renderer.transform;
            while (current != null && current != root.transform)
            {
                var objectName = current.name;
                if (objectName.Contains("Dummy_Mannequin") || objectName.Contains("Biker Rig"))
                {
                    renderer.enabled = false;
                    break;
                }

                current = current.parent;
            }
        }
    }

    private static void NormalizeModel(GameObject instance, Bounds bounds)
    {
        var maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize > 0f)
            instance.transform.localScale *= 2.6f / maxSize;

        bounds = CalculateBounds(instance);
        instance.transform.position -= bounds.center;
    }

    private static Texture2D RenderPreview(GameObject instance, Bounds bounds)
    {
        var preview = new PreviewRenderUtility(true);

        try
        {
            var camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.35f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = bounds.center + new Vector3(3.2f, 1.4f, -4.2f);
            camera.transform.LookAt(bounds.center + Vector3.up * bounds.extents.y * 0.1f);

            preview.lights[0].intensity = 1.35f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            preview.lights[1].intensity = 0.65f;
            preview.lights[1].transform.rotation = Quaternion.Euler(15f, 145f, 0f);
            preview.ambientColor = new Color(0.32f, 0.36f, 0.42f, 1f);
            preview.AddSingleGO(instance);

            preview.BeginPreview(new Rect(0, 0, ImageSize, ImageSize), GUIStyle.none);
            camera.Render();
            var rendered = preview.EndPreview();

            var temp = RenderTexture.GetTemporary(ImageSize, ImageSize, 0, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active;
            Graphics.Blit(rendered, temp);
            RenderTexture.active = temp;
            var texture = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0);
            texture.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temp);
            return texture;
        }
        finally
        {
            preview.Cleanup();
        }
    }

    private static void ConfigureSprite(string spritePath)
    {
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static void AssignConfigSprite(int id, string spritePath)
    {
        var configPath = string.Format(ConfigPathFormat, id);
        var config = AssetDatabase.LoadAssetAtPath<BikeSkinConfig>(configPath);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (config == null || sprite == null)
            return;

        config.UiPreviewSprite = sprite;
        EditorUtility.SetDirty(config);
    }
}
#endif
