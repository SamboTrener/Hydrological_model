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
    [SerializeField] int numIterationsPools = 10000;
    [SerializeField] int numIterationsErosion = 600000;
    [SerializeField] int numStatesBeforeTheEnd = 20;
    [SerializeField] float frameSpeed = 5f;
    [SerializeField] float elevationScale = 100;

    [SerializeField] MeshConstructor terrainConstructor;
    [SerializeField] MeshConstructor poolsConstructor;

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

        createPoolsButton.onClick.AddListener(() => StartCoroutine(CreatePools()));
    }

    IEnumerator CreatePools()
    {
        floodMap = new float[mapSizeWithBorder, mapSizeWithBorder];
        PoolGenerator.Instance.GeneratePools(map, floodMap, mapSizeWithBorder, numIterationsPools);
        poolsConstructor.ConstructMesh(mapSize, mapSizeWithBorder, floodMap, erosionBrushRadius, elevationScale);
        yield return new WaitForSeconds(frameSpeed);
    }

    void CreateTerrain()
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = HeightmapGenerator.Instance.GenerateHeightMap(mapSizeWithBorder);
        terrainConstructor.ConstructMesh(mapSize, mapSizeWithBorder, map, erosionBrushRadius, elevationScale);
        //TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator ShowOneDropPath()
    {
        yield return ErosionGenerator.Instance.ErodeOnce(map, mapSizeWithBorder, elevationScale, numIterationsErosion / numStatesBeforeTheEnd);
        TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator Erode()
    {
        for(int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            ErosionGenerator.Instance.Erode(map, mapSizeWithBorder, numIterationsErosion / numStatesBeforeTheEnd);
            terrainConstructor.ConstructMesh(mapSize, mapSizeWithBorder, map, erosionBrushRadius, elevationScale);
            //TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }
}
