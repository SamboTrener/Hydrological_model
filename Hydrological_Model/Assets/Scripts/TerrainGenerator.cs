using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainGenerator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] Material material;

    public float scale = 20;
    public float elevationScale = 10;
    Mesh mesh;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    public void ContructMesh(int mapSize, int mapSizeWithBorder, int erosionBrushRadius, float[,] map)
    {
        Vector3[] verts = new Vector3[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
        int t = 0;

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int borderedMapIndex = (y + erosionBrushRadius) * mapSizeWithBorder + x + erosionBrushRadius;
            int meshMapIndex = y * mapSize + x;

            Vector2 percent = new Vector2(x / (mapSize - 1f), y / (mapSize - 1f));
            Vector3 pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            float normalizedHeight = map[y + erosionBrushRadius, x + erosionBrushRadius];
            pos += Vector3.up * normalizedHeight * elevationScale;
            verts[meshMapIndex] = pos;

            // Construct triangles
            if (x != mapSize - 1 && y != mapSize - 1)
            {
                t = (y * (mapSize - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + mapSize;
                triangles[t + 1] = meshMapIndex + mapSize + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + mapSize + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        AssignMeshComponents();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        material.SetFloat("_MaxHeight", elevationScale);
    }


    void AssignMeshComponents()
    {
        // Find/creator mesh holder object in children
        string meshHolderName = "Mesh Holder";
        Transform meshHolder = transform.Find(meshHolderName);
        if (meshHolder == null)
        {
            meshHolder = new GameObject(meshHolderName).transform;
            meshHolder.transform.parent = transform;
            meshHolder.transform.localPosition = Vector3.zero;
            meshHolder.transform.localRotation = Quaternion.identity;
        }

        // Ensure mesh renderer and filter components are assigned
        if (!meshHolder.gameObject.GetComponent<MeshFilter>())
        {
            meshHolder.gameObject.AddComponent<MeshFilter>();
        }
        if (!meshHolder.GetComponent<MeshRenderer>())
        {
            meshHolder.gameObject.AddComponent<MeshRenderer>();
        }

        meshRenderer = meshHolder.GetComponent<MeshRenderer>();
        meshFilter = meshHolder.GetComponent<MeshFilter>();
    }
}