using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class TerrainCreator : MonoBehaviour
{
    [SerializeField] Terrain terrain;
    [SerializeField] int side;
    [SerializeField] int height;
    [SerializeField] int scale;
    [SerializeField] float offsetX;
    [SerializeField] float offsetY;
    Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(CreateTerrain);
    }

    private void CreateTerrain()
    {
        scale = Random.Range(30, 50);
        var offsetX = Random.Range(0, 10);
        var offsetY = Random.Range(0, 10);
        terrain.terrainData.size = new Vector3(side, height, side);

        //var heights = PerlinNoiseGenerator.GeneratePerlinNoiseHeightMap(side * 2, height, scale, offsetX, offsetY);

        //terrain.terrainData.SetHeights(0, 0, heights);

        var size = terrain.terrainData.heightmapResolution;

        var heights = terrain.terrainData.GetHeights(0, 0, size, size);

        heights.GeneratePerlinNoiseHeightMap(side, height, scale, offsetX, offsetY);

        terrain.terrainData.SetHeights(0, 0, heights);
    }
}
