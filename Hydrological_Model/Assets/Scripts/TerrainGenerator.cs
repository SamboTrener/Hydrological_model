using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainGenerator Instance { get; private set; }

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
