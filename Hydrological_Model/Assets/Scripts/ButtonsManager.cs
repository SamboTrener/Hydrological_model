using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ButtonsManager : MonoBehaviour
{
    [SerializeField] Button createTerrain;
    [SerializeField] Button erode;
    [SerializeField] Button erodeOnceButton;
    [SerializeField] Button createPoolsButton;

    [SerializeField] int mapSize = 507;
    [SerializeField] int erosionBrushRadius = 3;
    [SerializeField] int numIterations = 30000;
    [SerializeField] int numStatesBeforeTheEnd = 20;
    [SerializeField] float frameSpeed = 5f;
    [SerializeField] float elevationScale = 100;

    float[,] map;
    float[,] floodMap;
    int mapSizeWithBorder;

    private void Awake()
    {
        var createTerrainButton = createTerrain.GetComponent<Button>();
        createTerrainButton.onClick.AddListener(CreateTerrain);

        var erodeButton = erode.GetComponent<Button>();
        
        erodeButton.onClick.AddListener(() => StartCoroutine(Erode()));

        erodeOnceButton.onClick.AddListener(() => StartCoroutine(ShowOneDropPath()));

        createPoolsButton.onClick.AddListener(CreatePools);
    }

    void CreatePools()
    {
        /*Debug.Log($"poolMap Length is {floodMap.Length}");
        var sum = 0;
        for(int i = 0; i < floodMap.GetLength(0); i++)
        {
            for(int j = 0; j < floodMap.GetLength(1); j++)
            {
                if (floodMap[i,j] == 0)
                {
                    sum++;
                }
            }
        }
        Debug.Log(sum);*/

        PoolGenerator.Instance.GeneratePools(map, floodMap, mapSizeWithBorder, numIterations);
        MeshConstructor.Instance.ConstructMesh(mapSize, mapSizeWithBorder, floodMap, erosionBrushRadius, elevationScale);
    }

    void CreateTerrain()
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = HeightmapGenerator.Instance.GenerateHeightMap(mapSizeWithBorder);
        floodMap = new float[mapSizeWithBorder, mapSizeWithBorder];
        TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator ShowOneDropPath()
    {
        yield return ErosionGenerator.Instance.ErodeOnce(map, mapSizeWithBorder, elevationScale, numIterations / numStatesBeforeTheEnd);
        TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator Erode()
    {
        for(int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            ErosionGenerator.Instance.Erode(map, mapSizeWithBorder, numIterations / numStatesBeforeTheEnd);
            TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }
}
