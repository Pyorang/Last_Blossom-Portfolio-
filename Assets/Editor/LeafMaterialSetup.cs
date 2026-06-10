using UnityEngine;
using UnityEditor;

public static class LeafMaterialSetup
{
    [MenuItem("Tools/Setup Leaf Material (Alpha Clipping)")]
    public static void SetupLeafMaterial()
    {
        string texturePath = "Assets/03. Arts/Models/tree-pink-fbx/textures/";
        string materialPath = texturePath + "Mat_Leaf.mat";

        Texture2D combinedMap = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Leaf_Combined.png");
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Leaf_Normal.png");
        Texture2D aoMap = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Leaf_AO.png");

        if (combinedMap == null)
        {
            Debug.LogError("Leaf_Combined.png를 찾을 수 없습니다.\n" +
                           "먼저 Tools > Combine Leaf Texture + Opacity 를 실행하세요.");
            return;
        }

        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            Debug.LogError("URP Lit 쉐이더를 찾을 수 없습니다. URP가 설치되어 있는지 확인하세요.");
            return;
        }

        Material leafMat = new Material(urpLitShader);
        leafMat.name = "Mat_Leaf";

        leafMat.SetFloat("_Surface", 0);
        leafMat.SetFloat("_AlphaClip", 1);
        leafMat.SetFloat("_Cutoff", 0.5f);
        leafMat.SetFloat("_Cull", 0);

        leafMat.SetTexture("_BaseMap", combinedMap);
        
        if (normalMap != null)
            leafMat.SetTexture("_BumpMap", normalMap);
        
        if (aoMap != null)
            leafMat.SetTexture("_OcclusionMap", aoMap);

        leafMat.EnableKeyword("_ALPHATEST_ON");
        leafMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
        {
            AssetDatabase.DeleteAsset(materialPath);
        }

        AssetDatabase.CreateAsset(leafMat, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Material 생성 완료: {materialPath}\n" +
                  "FBX의 Leaf 메시에 이 Material을 적용하세요.");

        Selection.activeObject = leafMat;
        EditorGUIUtility.PingObject(leafMat);
    }
}
