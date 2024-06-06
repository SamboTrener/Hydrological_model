using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class PoolGenerator : MonoBehaviour
{

    public static PoolGenerator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] bool randomizeSeed;
    [SerializeField] int seed;
    [Range(2, 8)]
    [SerializeField] int erosionRadius = 3;
    [Range(0, 1)]
    [SerializeField] float inertia = .05f; // ��� ������� �������� ���� ��������� ������� ����������� � ������� ���� �� ������.
                                           // ��� �������� 1 ���� ������� �� ������� �����������.
    [Range(0, 1)]
    [SerializeField] float depositSpeed = .3f;
    [Range(0, 1)]
    [SerializeField] float evaporateSpeed = .01f;
    [SerializeField] float gravity = 4;
    [SerializeField] int maxDropletLifetime = 300;

    [SerializeField] float initialWaterVolume = 0.001f;
    [SerializeField] float initialSpeed = 1;
    [SerializeField] float epsilon = 0.01f;

    // Indices and weights of erosion brush precomputed for every node
    System.Random prng;

    List<Pool> pools = new List<Pool>();

    // Initialization creates a System.Random object and precomputes indices and weights of erosion brush
    void Initialize()
    {
        seed = (randomizeSeed) ? UnityEngine.Random.Range(-10000, 10000) : seed;
        prng = new System.Random(seed);
    }

    public void GeneratePools(float[,] map, float[,] poolMap, int mapSize, int numIterations = 30000)
    {
        Initialize();

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            // �������� ����� � ��������� ����� �����
            var droplet = new WaterDroplet(prng.Next(0, mapSize - 1), prng.Next(0, mapSize - 1), 0, 0, initialSpeed, initialWaterVolume);

            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {

                int nodeX = (int)droplet.posX;
                int nodeY = (int)droplet.posY;


                if (lifetime == maxDropletLifetime - 1)
                {
                    droplet.dirX = 0;
                    droplet.dirY = 0;
                }

                if (poolMap[nodeY, nodeX] > map[nodeY, nodeX])
                {
                    AddDropletToPool(poolMap, map, droplet, mapSize);
                    break;
                }

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient;
                var isCalculated = TryCalculateHeightAndGradient(map, mapSize, droplet.posX, droplet.posY, out heightAndGradient);
                if (!isCalculated)
                {
                    break;
                }

                // ������ ����������� � ��������� ����� (������ ��������� �� 1 ���������� �� ��������).
                droplet.dirX = (droplet.dirX * inertia - heightAndGradient.gradientX * (1 - inertia));
                droplet.dirY = (droplet.dirY * inertia - heightAndGradient.gradientY * (1 - inertia));
                // Normalize direction
                float len = Mathf.Sqrt(droplet.dirX * droplet.dirX + droplet.dirY * droplet.dirY);
                if (len != 0)
                {
                    droplet.dirX /= len;
                    droplet.dirY /= len;
                }
                droplet.posX += droplet.dirX;
                droplet.posY += droplet.dirY;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if (droplet.posX < 0 || droplet.posX >= mapSize - 1 || droplet.posY < 0 || droplet.posY >= mapSize - 1)
                {
                    break;
                }
                if (droplet.dirX == 0 && droplet.dirY == 0)
                {
                    break;
                }
                if (droplet.volume <= 0.1 * initialWaterVolume)
                {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                HeightAndGradient newHeightAndGradient;
                isCalculated = TryCalculateHeightAndGradient(map, mapSize, droplet.posX, droplet.posY, out newHeightAndGradient);
                if (!isCalculated)
                {
                    break;
                }
                float newHeight = newHeightAndGradient.height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // if flowing uphill:
                if (deltaHeight > 0)
                {
                    if (lifetime == maxDropletLifetime - 1)
                    {
                        CreatePool(poolMap, map, droplet, mapSize);
                        break;
                    }
                }
                else
                {
                    // Update droplet's speed and water content
                    droplet.speed = Mathf.Sqrt(droplet.speed * droplet.speed + deltaHeight * gravity);
                    droplet.volume *= (1 - evaporateSpeed);
                    if (lifetime == maxDropletLifetime - 1)
                    {
                        lifetime--;
                    }
                }
            }
        }
    }

    void AddVolumeEqually(float[,] poolMap, Pool pool, float volume)
    {
        foreach (var point in pool.Points)
        {
            poolMap[point.Y, point.X] += volume / pool.Points.Count;
        }
    }

    Pool FindPool(int coordX, int coordY)
    {
        foreach (var pool in pools)
        {
            foreach(var point in pool.Points)
            {
                if(coordX == point.X && coordY == point.Y)
                {
                    return pool;
                }
            }
        }
        throw new Exception();
    }

    void AddDropletToPool(float[,] poolMap, float[,] map, WaterDroplet droplet, int mapSize)
    {
        Debug.Log("Adding droplet to pool");
        int coordY = (int)droplet.posY;
        int coordX = (int)droplet.posX;
        var pool = FindPool(coordX, coordY);

        //Debug.Log($"pool id = {pool.Id}");
        //DebugVolIfNegativeTemp(map, poolMap, pool);
        //Debug.Log($"points count is {pool.Points.Count}");

        pool.Volume += droplet.volume;
        //Debug.Log($"Droplet volume before adding = {droplet.volume}");
        AddVolumeEqually(poolMap, pool, droplet.volume);

        CorrectPool(poolMap, map, mapSize, pool);
    }

    int id = 0;
    void CreatePool(float[,] poolMap, float[,] map, WaterDroplet droplet, int mapSize)
    {
        Debug.Log("Pool creation started");
        int coordY = (int)droplet.posY;
        int coordX = (int)droplet.posX;

        var initialPoolVolume = droplet.volume;
        var FirstLowestPointOfLeak = new Point(coordX, coordY, map[coordY, coordX]);
        var points = new List<Point>
        {
            FirstLowestPointOfLeak
        };
        var pool = new Pool(points, initialPoolVolume, id);  //������ ������� � ����� ����� 
        id++;
        poolMap[coordY, coordX] = map[coordY, coordX] + initialPoolVolume;

        CorrectPool(poolMap, map, mapSize, pool);

        pools.Add(pool);
    }

    private void CorrectPool(float[,] poolMap, float[,] map, int mapSize, Pool pool)
    {
        FindLowestPointOfLeak(map, poolMap, pool, mapSize); //����� ������ ����� ��� ����� ������ ����

        var testingPoint = pool.Points.First();
        while (pool.LowestPointOfLeak.Height + epsilon < poolMap[testingPoint.Y, testingPoint.X])
        {
            if (pool.Points.Contains(pool.LowestPointOfLeak))
            {
              //  Debug.Log("pool contains lowestPointOfLeak");
                break;
            }
            if (poolMap[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X] > map[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X]) //� ����� ���������� ������ �������
            {
                var anotherPool = FindPool(pool.LowestPointOfLeak.X, pool.LowestPointOfLeak.Y);
                MergeTwoPools(map, poolMap, pool, anotherPool); //��� ����� ��� ������������� temp
                FindLowestPointOfLeak(map, poolMap, pool, mapSize);
                testingPoint = pool.Points.First(); //��������� ����� ������������, ������ ��� ������ ����� ����� �� ��������
                Debug.Log($"Lowest point of leak height = {pool.LowestPointOfLeak.Height}");
                Debug.Log($"Water level = {poolMap[testingPoint.Y, testingPoint.X]}");
                if (pool.LowestPointOfLeak.Height + epsilon >= poolMap[testingPoint.Y, testingPoint.X])
                    break;
            }

            AddPointInPool(map, poolMap, pool);
            
            //Debug.Log("vol before correction");
            //DebugVolTemp(map, poolMap, pool);
            while (!IsPoolCorrect(map, poolMap, pool))
            {
                //Debug.Log("vol in process of correction");
                //DebugVolTemp(map, poolMap, pool);
                CorrectPoolOnce(map, poolMap, pool);
            }
            //Debug.Log("vol after correction");
            //DebugVolTemp(map, poolMap, pool);

            testingPoint = pool.Points.First(); //��������� ����� ������������, ������ ��� ������ ����� ����� �� ��������

            FindLowestPointOfLeak(map, poolMap, pool, mapSize);
            // Debug.Log($"pool.LowestPointOfLeak.Height = {pool.LowestPointOfLeak.Height}");
            // Debug.Log($"poolMap[testingPoint.Y, testingPoint.X] = {poolMap[testingPoint.Y, testingPoint.X]}");
        }
    }

    void FindLowestPointOfLeak(float[,] map, float[,] poolMap, Pool pool, int mapSize)
    {
        foreach (var point in pool.Points)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (point.Y + y < 0 || point.Y + y >= mapSize)
                {
                    continue;
                }
                for (int x = -1; x <= 1; x++)
                {
                    if (point.X + x < 0 || point.X + x >= mapSize)
                    {
                        continue;
                    }
                    if (pool.Points.FirstOrDefault(poolPoint => poolPoint.Y == point.Y + y && poolPoint.X == point.X + x) == null) //���� �������� ����� �� � ��������
                    {
                        if (poolMap[point.Y, point.X] > map[point.Y + y, point.X + x]) //������� ����� ������ ����� 
                        {
                            pool.LowestPointOfLeak = new Point(point.X + x, point.Y + y, map[point.Y + y, point.X + x]);
                        }
                    }
                }
            }
        }
    }

    void AddPointInPool(float[,] map, float[,] poolMap, Pool pool)
    {
        var lowestPoint = pool.LowestPointOfLeak; //��� ��������
        var testingPoint = pool.Points.First(); //��� ��������

        var dif = poolMap[testingPoint.Y, testingPoint.X] - map[lowestPoint.Y, lowestPoint.X];

        pool.Points.Add(lowestPoint); //����� ����� ������ ��������
        poolMap[lowestPoint.Y, lowestPoint.X] = map[lowestPoint.Y, lowestPoint.X] + dif; //��������� � ����� ������

        foreach (var point in pool.Points) //����������� ������ ��� ������ �����
        {
            poolMap[point.Y, point.X] -= dif / pool.Points.Count;
            // Debug.Log($"poolmap � ����� �� ������� �������� {poolMap[point.Y, point.X]}");
        }
    }

    bool IsPoolCorrect(float[,] map, float[,] poolMap, Pool pool)
    {
        foreach (var point in pool.Points)
        {
            if (poolMap[point.Y, point.X] < map[point.Y, point.X])
            {
                return false;
            }
        }
        return true;
    }

    void DebugVolIfNegativeTemp(float[,] map, float[,] poolMap, Pool pool)
    {
        var volume = 0f;
        foreach(var point in pool.Points)
        {
            volume += poolMap[point.Y, point.X] - map[point.Y, point.X];
        }
        if(volume < 0)
        {
            Debug.Log(volume);
        }
    }
    void DebugVolTemp(float[,] map, float[,] poolMap, Pool pool)
    {
        var volume = 0f;
        foreach (var point in pool.Points)
        {
            volume += poolMap[point.Y, point.X] - map[point.Y, point.X];
        }
        Debug.Log(volume);
    }

    void CorrectPoolOnce(float[,] map, float[,] poolMap, Pool pool)
    {
        var biggestDif = 0f;
        Point baddestPoint = null;
        foreach (var point in pool.Points)
        {
            if (poolMap[point.Y, point.X] < map[point.Y, point.X])
            {
                var dif = map[point.Y, point.X] - poolMap[point.Y, point.X];
                if (dif > biggestDif)
                {
                    biggestDif = dif;
                    baddestPoint = point;
                }
            }
        }

        if (baddestPoint != null)
        {
            poolMap[baddestPoint.Y, baddestPoint.X] = 0;
            pool.Points.Remove(baddestPoint); //���� ����� ��������, ��� ������� ����� ����������� �� ���������, �� �� ������ ���� ��� �� ��� ���� �������
            //Debug.Log("Point was removed");
            foreach (var point in pool.Points)
            {
                poolMap[point.Y, point.X] -= biggestDif / pool.Points.Count;
            }
        }
    }

    void MergeTwoPools(float[,] map, float[,] poolMap, Pool pool, Pool secondPool)
    {
        Debug.Log($"Merging pools with ids {pool.Id} and {secondPool.Id}");
        var secondPoolTestingPoint = secondPool.Points.First();
        var secondPoolWaterLevel = poolMap[secondPoolTestingPoint.Y, secondPoolTestingPoint.X];

        foreach(var point in secondPool.Points)
        {
            poolMap[point.Y, point.X] = 0;
        }
        AddVolumeEqually(poolMap, pool, secondPool.Volume);

        foreach(var pointToAdd in secondPool.Points)
        {
            AddPointInPool(map, poolMap, poo);
        }


      /*  foreach (var point in secondPool.Points)
        {
           pool.Points.Add(point);
        }
        pool.Volume += secondPool.Volume;
        if (secondPool.LowestPointOfLeak.Height < pool.LowestPointOfLeak.Height && !pool.Points.Contains(secondPool.LowestPointOfLeak))
        {
            pool.LowestPointOfLeak = secondPool.LowestPointOfLeak;
        }

        pools.Remove(secondPool);

        foreach (var point in pool.Points)
        {
            poolMap[point.Y, point.X] = map[point.Y, point.X] + pool.Volume / pool.Points.Count; //��� ��� �� �������� ���� ��
        }

        //Debug.Log("vol before correction");
        //DebugVolTemp(map, poolMap, pool);
        while (!IsPoolCorrect(map, poolMap, pool))
        {
            CorrectPoolOnce(map, poolMap, pool);
        }
        DebugVolTemp(map,poolMap, pool);
        //Debug.Log("vol after correction");
        //DebugVolTemp(map, poolMap, pool); */
    }

    bool TryCalculateHeightAndGradient(float[,] nodes, int mapSize, float posX, float posY, out HeightAndGradient heightAndGradient)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        if (coordX >= mapSize - 1 || coordY >= mapSize - 1 || coordX < 0 || coordY < 0)
        {
            heightAndGradient = new HeightAndGradient();
            return false;
        }

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        //int nodeIndexNW = coordY * mapSize + coordX;

        float heightNW = nodes[coordY, coordX];
        float heightNE = nodes[coordY, coordX + 1];
        float heightSW = nodes[coordY + 1, coordX];
        float heightSE = nodes[coordY + 1, coordX + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        heightAndGradient = new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
        return true;
    }

    struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}

