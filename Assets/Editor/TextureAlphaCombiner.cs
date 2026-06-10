using UnityEngine;
using UnityEditor;
using System.IO;

public static class TextureAlphaCombiner
{
    [MenuItem("Tools/Combine Leaf Texture + Opacity (Alpha)")]
    public static void CombineLeafTextureWithOpacity()
    {
        string texturePath = "Assets/03. Arts/Models/tree-pink-fbx/textures/";
        string colorPath = texturePath + "Leaf.png";
        string opacityPath = texturePath + "Leaf_Opacity.png";
        string outputPath = texturePath + "Leaf_Combined.png";

        MakeTextureReadable(colorPath);
        MakeTextureReadable(opacityPath);

        Texture2D colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
        Texture2D opacityTex = AssetDatabase.LoadAssetAtPath<Texture2D>(opacityPath);

        if (colorTex == null || opacityTex == null)
        {
            Debug.LogError("텍스처를 찾을 수 없습니다.");
            return;
        }

        int width = colorTex.width;
        int height = colorTex.height;

        Texture2D combinedTex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color[] colorPixels = colorTex.GetPixels();
        Color[] opacityPixels = opacityTex.GetPixels(0, 0, opacityTex.width, opacityTex.height);

        bool needsResize = opacityTex.width != width || opacityTex.height != height;
        if (needsResize)
        {
            opacityPixels = ResamplePixels(opacityPixels, opacityTex.width, opacityTex.height, width, height);
        }

        Color[] resultPixels = new Color[colorPixels.Length];

        for (int i = 0; i < colorPixels.Length; i++)
        {
            resultPixels[i] = new Color(
                colorPixels[i].r,
                colorPixels[i].g,
                colorPixels[i].b,
                opacityPixels[i].r
            );
        }

        combinedTex.SetPixels(resultPixels);
        combinedTex.Apply();

        byte[] pngData = combinedTex.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), outputPath);
        File.WriteAllBytes(fullPath, pngData);

        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer != null)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log($"알파 채널 합치기 완료: {outputPath}\n" +
                  "이제 Tools > Setup Leaf Material 을 실행하세요.");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
    }

    private static void MakeTextureReadable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    private static Color[] ResamplePixels(Color[] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        Color[] result = new Color[dstWidth * dstHeight];

        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                float srcX = (float)x / dstWidth * srcWidth;
                float srcY = (float)y / dstHeight * srcHeight;

                int srcIndex = Mathf.Clamp((int)srcY, 0, srcHeight - 1) * srcWidth +
                               Mathf.Clamp((int)srcX, 0, srcWidth - 1);

                result[y * dstWidth + x] = source[srcIndex];
            }
        }

        return result;
    }
}
