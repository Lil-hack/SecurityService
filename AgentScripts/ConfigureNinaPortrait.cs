using System;
using UnityEditor;
using UnityEngine;

public static class ConfigureNinaPortrait
{
    public static string Apply()
    {
        const string path = "Assets/Resources/Characters/NinaPortraitSelected.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Portrait texture importer missing");
        var before = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        string old = before.width + "x" + before.height;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return "Portrait import: " + old + " -> " + texture.width + "x" + texture.height + "; aspect preserved";
    }
}
