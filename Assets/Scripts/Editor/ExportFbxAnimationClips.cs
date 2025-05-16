using UnityEditor;
using UnityEngine;

public class ExportFbxAnimationClips
{
    [MenuItem("Tools/Export FBX Animation Clips From Path")]
    private static void ExportClipsFromPath()
    {
        // 指定 FBX 文件的路径
        string fbxFolderPath = "Assets/SciFiWarriorPBRHPPolyart/Animations";

        // 获取指定路径下的所有 FBX 文件
        string[] fbxFiles = AssetDatabase.FindAssets("t:Model", new[] { fbxFolderPath });

        foreach (var guid in fbxFiles)
        {
            // 获取 FBX 文件的路径
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // 获取 FBX 文件名（不含扩展名）
            string fbxFileName = System.IO.Path.GetFileNameWithoutExtension(path);

            // 加载 FBX 文件中的所有资源
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var asset in assets)
            {
                // 检查是否是 AnimationClip 且不是子资源
                if (asset is AnimationClip clip && !AssetDatabase.IsSubAsset(clip))
                {
                    // 根据 FBX 文件名和动画剪辑名生成新路径
                    var newPath = $"Assets/Animators/PolyartAnimation/{fbxFileName}_{clip.name}.anim";

                    // 创建新的动画剪辑
                    AnimationClip newClip = Object.Instantiate(clip);

                    // 设置动画循环
                    SetAnimationClipLoop(newClip);

                    // 保存新的动画剪辑
                    AssetDatabase.CreateAsset(newClip, newPath);
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Export Finished!");
    }

    private static void SetAnimationClipLoop(AnimationClip clip)
    {
        // 设置 WrapMode 为 Loop
        clip.wrapMode = WrapMode.Loop;

        // 设置 AnimationClipSettings 的 loopTime 为 true
        var serializedClip = new SerializedObject(clip);
        var settings = serializedClip.FindProperty("m_AnimationClipSettings");
        if (settings != null)
        {
            settings.FindPropertyRelative("m_LoopTime").boolValue = true;
            serializedClip.ApplyModifiedProperties();
        }
    }
}