using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CardPrefabCreator : MonoBehaviour
{
    [Header("Settings")]
    public string inputFolderPath = "Assets/Resources/Images"; // Folder containing images
    public string outputFolderPath = "Assets/Prefabs/Cards"; // Folder to save prefabs
    public Vector2 prefabSize = new Vector2(200, 200); // Size of the prefab

    public void CreatePrefabs()
    {
        // Ensure output folder exists
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        // Load all image files from the input folder
        string[] imageFiles = Directory.GetFiles(inputFolderPath, "*.png");

        foreach (string imagePath in imageFiles)
        {
            // Load the texture
            string fileName = Path.GetFileNameWithoutExtension(imagePath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

            if (texture == null)
            {
                Debug.LogWarning($"Failed to load texture: {imagePath}");
                continue;
            }

            // Ensure the texture is set to Sprite (2D and UI) with Single mode
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            // Optimize texture settings
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single; // Set to Single mode
                textureImporter.maxTextureSize = 1024; // Limit max size to 1024x1024
                textureImporter.textureCompression = TextureImporterCompression.Compressed; // Use compressed format
                textureImporter.mipmapEnabled = true; // Enable mipmaps
                textureImporter.filterMode = FilterMode.Bilinear; // Set filter mode
                // textureImporter.spritePackingTag = "UI_Sprites"; // Assign to a packing tag
                textureImporter.SaveAndReimport();
            }

            // Create a new GameObject for the prefab
            GameObject prefab = new GameObject(fileName);

            // Set prefab size to width 200 and height 220
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 220);

            // Add an Image component and set the texture
            Image image = prefab.AddComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Add a Text element for the name
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(prefab.transform);

            Text text = textObject.AddComponent<Text>();
            text.text = fileName;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.UpperRight;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            // Adjust Text RectTransform position and size
            textRect.anchorMin = new Vector2(1, 1); // Top * Right
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(-180, -30); // Left 130 * 180
            textRect.offsetMax = new Vector2(-5, 0); // Top alignment

            // Set local width for the Text component
            textRect.sizeDelta = new Vector2(50, textRect.sizeDelta.y);

            // Set font size
            text.fontSize = 30;

            // Set best fit to true for the Text component
            text.resizeTextForBestFit = true;

            // Save the prefab
            string prefabPath = Path.Combine(outputFolderPath, fileName + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);

            // Destroy the temporary GameObject
            DestroyImmediate(prefab);
        }

        Debug.Log("Prefabs created successfully.");
    }

    [MenuItem("Tools/Create Prefabs from Images")]
    public static void StartCreatePrefabs()
    {
        CardPrefabCreator creator = new CardPrefabCreator();
        creator.CreatePrefabs();
    }
}
