using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class MeshCreator : MonoBehaviour
{
    [ContextMenu("Generate Wave Surface Mesh")]
    public void GenerateWaveSurfaceMesh()
    {
        // 创建一个新的 Mesh
        Mesh mesh = new Mesh();
        mesh.name = "WaveSurfaceMesh";

        // 定义网格的分辨率
        int widthSegments = 50;  // 横向分段数
        int heightSegments = 50; // 纵向分段数
        float width = 1.0f;      // 网格宽度
        float height = 1.0f;     // 网格高度
        float waveHeight = 0.1f; // 波浪高度

        // 计算顶点数
        Vector3[] vertices = new Vector3[(widthSegments + 1) * (heightSegments + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[widthSegments * heightSegments * 6];

        // 生成顶点和 UV
        for (int y = 0; y <= heightSegments; y++)
        {
            for (int x = 0; x <= widthSegments; x++)
            {
                int index = y * (widthSegments + 1) + x;

                // 计算顶点位置
                float xPos = (x / (float)widthSegments) * width - width / 2;
                float yPos = (y / (float)heightSegments) * height - height / 2;
                float zPos = Mathf.Sin(xPos * Mathf.PI * 2) * Mathf.Sin(yPos * Mathf.PI * 2) * waveHeight;

                vertices[index] = new Vector3(xPos, zPos, yPos);

                // 计算 UV 坐标
                uv[index] = new Vector2(x / (float)widthSegments, y / (float)heightSegments);
            }
        }

        // 生成三角形索引
        int triangleIndex = 0;
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int bottomLeft = y * (widthSegments + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (widthSegments + 1);
                int topRight = topLeft + 1;

                // 第一个三角形
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;

                // 第二个三角形
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomRight;
            }
        }

        // 设置 Mesh 数据
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals(); // 自动计算法线

        // 保存 Mesh 到 Assets 文件夹
        SaveMesh(mesh, "Assets/WaveSurfaceMesh.asset");
    }

    private void SaveMesh(Mesh mesh, string path)
    {
        // 检查是否在编辑器模式下运行
        if (!Application.isEditor)
        {
            Debug.LogError("Mesh saving is only supported in the Unity Editor.");
            return;
        }

        // 保存 Mesh 到指定路径
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Mesh saved to {path}");
    }
}
