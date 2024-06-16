using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class MeshConstructor : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Material tempMaterial;

    Mesh mesh;

    public void ConstructMesh(int mapSize, float[,] map, int erosionBrushRadius, float elevationScale)
    {
        if (mesh == null)
        {
            mesh = new Mesh();
        }
        mesh.Clear(false);

        Vector3[] verts = new Vector3[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
        int t;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int meshMapIndex = x * mapSize + y;
                var vec = new Vector2(y, x);
                var pos = new Vector3(vec.x - 1, 0, vec.y - 1);

                float normalizedHeight = map[x + erosionBrushRadius, y + erosionBrushRadius];
                pos += elevationScale * normalizedHeight * Vector3.up;
                verts[meshMapIndex] = pos;

                if (y != mapSize - 1 && x != mapSize - 1)
                {
                    t = (x * (mapSize - 1) + y) * 3 * 2;
                    triangles[t + 0] = meshMapIndex + mapSize;
                    triangles[t + 1] = meshMapIndex + mapSize + 1;
                    triangles[t + 2] = meshMapIndex;

                    triangles[t + 3] = meshMapIndex + mapSize + 1;
                    triangles[t + 4] = meshMapIndex + 1;
                    triangles[t + 5] = meshMapIndex;
                    t += 6;
                }
            }
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshRenderer.sharedMaterial = tempMaterial;
    }
}
