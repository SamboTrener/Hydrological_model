using System.Collections;
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
    [SerializeField] Button prepareForGameButton;
    [SerializeField] Button startGameButton;

    [SerializeField] GameObject damCreationWindow;

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
        createTerrain.onClick.AddListener(CreateTerrain);

        var erodeButton = erode.GetComponent<Button>();
        
        erodeButton.onClick.AddListener(() => StartCoroutine(Erode()));

        erodeOnceButton.onClick.AddListener(() => StartCoroutine(ShowOneDropPath()));

        createPoolsButton.onClick.AddListener(() => StartCoroutine(CreatePools()));

        buildADamButton.onClick.AddListener(BuildDam);

        createPoolsFromPointButton.onClick.AddListener(() => StartCoroutine(CreatePoolsFromPoint()));

        prepareForGameButton.onClick.AddListener(() => StartCoroutine(PrepareForGame()));

        startGameButton.onClick.AddListener(() => StartCoroutine(StartGame()));
    }

    IEnumerator StartGame()
    {
        yield return CreatePoolsFromPoint();
        GameManager.Instance.GetResults(poolMap);
        damCreationWindow.SetActive(false);
    }

    IEnumerator PrepareForGame()
    {
        ResetPools();
        CreateTerrain();
        yield return Erode();
        GameManager.Instance.SpawnTargets(map, 35, 15, elevationScale, xStart, yStart);
        damCreationWindow.SetActive(true);
    }

    void ResetPools()
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2; 
        poolMap = new float[mapSizeWithBorder, mapSizeWithBorder];
        poolsConstructor.ConstructMesh(mapSize, poolMap, erosionBrushRadius, elevationScale);
    }

    void BuildDam()
    {
        DamManager.Instance.BuildDam(map, poolMap, mapSizeWithBorder);
        terrainConstructor.ConstructMesh(mapSize, map, erosionBrushRadius, elevationScale);
        poolsConstructor.ConstructMesh(mapSize, poolMap, erosionBrushRadius, elevationScale);
    }

    [SerializeField] int xStart = 35;
    [SerializeField] int yStart = 35;
    IEnumerator CreatePoolsFromPoint()
    {
        for (int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            PoolGeneratorNonDynamic.Instance.GeneratePools(map, poolMap, mapSizeWithBorder, numIterationsPools / numStatesBeforeTheEnd, xStart, yStart);
            poolsConstructor.ConstructMesh(mapSize, poolMap, erosionBrushRadius, elevationScale);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
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
        terrainConstructor.ConstructMesh(mapSizeWithBorder, map, erosionBrushRadius, elevationScale);

        poolMap = new float[mapSizeWithBorder, mapSizeWithBorder];
        //TerrainGenerator.Instance.CreateTerrain(elevationScale, mapSizeWithBorder, map);
    }

    IEnumerator ShowOneDropPath()
    {
        yield return DropletPathDrawer.Instance.ShowOneDropPath(map, mapSizeWithBorder, elevationScale);
    }

    IEnumerator Erode()
    {
        for(int i = 0; i < numStatesBeforeTheEnd; i++)
        {
            ErosionGenerator.Instance.Erode(map, mapSizeWithBorder, numIterationsErosion / numStatesBeforeTheEnd);
            terrainConstructor.ConstructMesh(mapSize, map, erosionBrushRadius, elevationScale);
            Debug.Log("New mesh constructed");
            yield return new WaitForSeconds(frameSpeed);
        }
    }
}
