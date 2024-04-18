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
    [SerializeField] int numStatesBeforeTheEnd = 20;
    [SerializeField] float frameSpeed = 5f;

    float[,] map;
    int mapSizeWithBorder;

    private void Awake()
    {
        var createTerrainButton = createTerrain.GetComponent<Button>();
        createTerrainButton.onClick.AddListener(CreateTerrain);

        var erodeButton = erode.GetComponent<Button>();
        
        erodeButton.onClick.AddListener(() => StartCoroutine(Erode()));
    }

    void CreateTerrain()
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = HeightmapGenerator.Instance.GenerateHeightMap(mapSizeWithBorder);
        TerrainGenerator.Instance.ContructMesh(mapSize, mapSizeWithBorder, erosionBrushRadius, map);
    }

    IEnumerator Erode()
    {
        for(int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            ErosionGenerator.Instance.Erode(map, mapSizeWithBorder, numIterations / numStatesBeforeTheEnd);
            TerrainGenerator.Instance.ContructMesh(mapSize, mapSizeWithBorder, erosionBrushRadius, map);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }
}
