using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonsManager : MonoBehaviour
{
    [SerializeField] Button createTerrain;
    [SerializeField] Button erode;

    [SerializeField] int mapSize = 255;
    [SerializeField] int erosionBrushRadius = 3;
    [SerializeField] int numIterations = 30000;

    float[,] map;
    int mapSizeWithBorder;

    private void Awake()
    {
        var createTerrainButton = createTerrain.GetComponent<Button>();
        createTerrainButton.onClick.AddListener(CreateTerrain);

        var erodeButton = erode.GetComponent<Button>();
        erodeButton.onClick.AddListener(Erode);
    }

    void CreateTerrain()
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = HeightmapGenerator.Instance.GenerateHeightMap(mapSizeWithBorder);
        TerrainGenerator.Instance.ContructMesh(mapSize, mapSizeWithBorder, erosionBrushRadius, map);
    }

    void Erode()
    {
        ErosionGenerator.Instance.Erode(map, mapSizeWithBorder, numIterations);
        TerrainGenerator.Instance.ContructMesh(mapSize, mapSizeWithBorder, erosionBrushRadius, map);
    }
}
