using UnityEngine;

public class TerrainGeneratorLegacy : MonoBehaviour
{
    public static TerrainGeneratorLegacy Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] Terrain terrain;

    public void CreateTerrain(float elevationScale, int mapSizeWithBorder, float[,] map)
    {
        terrain.terrainData.size = new Vector3(mapSizeWithBorder, elevationScale, mapSizeWithBorder);
        terrain.terrainData.SetHeights(0, 0, map);
    }
}
