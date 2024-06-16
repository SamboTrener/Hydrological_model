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
    [SerializeField] Button buildADamButton;
    [SerializeField] Button createPoolsFromPointButton;
    [SerializeField] Button startGameButton;

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
    float[,] poolMap;
    int mapSizeWithBorder;

    private void Awake()
    {
        var createTerrainButton = createTerrain.GetComponent<Button>();
        createTerrainButton.onClick.AddListener(CreateTerrain);

        var erodeButton = erode.GetComponent<Button>();
        
        erodeButton.onClick.AddListener(() => StartCoroutine(Erode()));

        erodeOnceButton.onClick.AddListener(() => StartCoroutine(ShowOneDropPath()));

        createPoolsButton.onClick.AddListener(() => StartCoroutine(CreatePools()));

        buildADamButton.onClick.AddListener(BuildDam);

        createPoolsFromPointButton.onClick.AddListener(() => StartCoroutine(CreatePoolsFromPoint()));

        startGameButton.onClick.AddListener(() => StartCoroutine(StartGame()));
    }

    IEnumerator StartGame()
    {
        CreateTerrain();
        yield return Erode();
        GameManager.Instance.SpawnTargets(map, 35, 15, elevationScale, xStart, yStart);
        BuildDam();
        yield return CreatePoolsFromPoint();
        GameManager.Instance.GetResults(poolMap);
    }

    [SerializeField] int xStart;
    [SerializeField] int yStart;
    IEnumerator CreatePoolsFromPoint()
    {
        for (int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            PoolGeneratorNonDynamic.Instance.GeneratePoolsFromPoint(map, poolMap, mapSizeWithBorder, numIterationsPools / numStatesBeforeTheEnd, xStart, yStart);
            poolsConstructor.ConstructMesh(mapSize, poolMap, erosionBrushRadius, elevationScale);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }

    void BuildDam()
    {
        DamManager.Instance.BuildDam(map, poolMap, mapSizeWithBorder);
        terrainConstructor.ConstructMesh(mapSize, map, erosionBrushRadius, elevationScale);
        poolsConstructor.ConstructMesh(mapSize, poolMap, erosionBrushRadius, elevationScale);
    }

    IEnumerator CreatePools()
    {
        for(int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            PoolGeneratorNonDynamic.Instance.GeneratePools(map, poolMap, mapSizeWithBorder, numIterationsPools / numStatesBeforeTheEnd);
            poolsConstructor.ConstructMesh(mapSize, poolMap, erosionBrushRadius, elevationScale);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }

    void CreateTerrain()
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = HeightmapGenerator.Instance.GenerateHeightMap(mapSizeWithBorder);
        terrainConstructor.ConstructMesh(mapSize, map, erosionBrushRadius, elevationScale);

        poolMap = new float[mapSizeWithBorder, mapSizeWithBorder];
        //TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator ShowOneDropPath()
    {
        yield return ErosionGenerator.Instance.ErodeOnce(map, mapSizeWithBorder, elevationScale, numIterationsErosion / numStatesBeforeTheEnd);
        terrainConstructor.ConstructMesh(mapSize, map, erosionBrushRadius, elevationScale);
        //TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator Erode()
    {
        for(int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            ErosionGenerator.Instance.Erode(map, mapSizeWithBorder, numIterationsErosion / numStatesBeforeTheEnd);
            terrainConstructor.ConstructMesh(mapSize, map, erosionBrushRadius, elevationScale);
            //TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }
}
