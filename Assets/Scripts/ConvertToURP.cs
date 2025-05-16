using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ConvertToURP : EditorWindow
{
    [MenuItem("Tools/URP 转换/批量替换 Shader")]
    static void ReplaceShaders()
    {
        // 获取所有材质
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            // 将 Standard Shader 替换为 URP Lit
            if (material.shader.name == "Standard")
            {
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                EditorUtility.SetDirty(material);
            }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/内置 转换/批量替换 Shader")]
    static void ReplacePipelineShaders()
    {
        // 获取所有材质
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            // 将 Standard Shader 替换为 URP Lit
            if (material.shader.name == "Universal Render Pipeline/Lit")
            {
                material.shader = Shader.Find("Standard");
                EditorUtility.SetDirty(material);
            }
        }
        AssetDatabase.SaveAssets();
    }
}