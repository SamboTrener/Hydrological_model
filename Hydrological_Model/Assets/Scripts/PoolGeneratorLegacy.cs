using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PoolGeneratorLegacy : MonoBehaviour
{

    public static PoolGeneratorLegacy Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] bool randomizeSeed;
    [SerializeField] int seed;
    [Range(0, 1)]
    [SerializeField] float inertia = .05f; 
    [Range(0, 1)]
    [SerializeField] float evaporateSpeed = .01f;
    [SerializeField] float gravity = 4;
    [SerializeField] int maxDropletLifetime = 300;

    [SerializeField] float initialWaterVolume = 0.01f;
    [SerializeField] float initialSpeed = 1;
    [SerializeField] float epsilon = 0.01f;

    System.Random prng;

    List<Pool> pools = new List<Pool>();

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

                HeightAndGradient heightAndGradient;
                var isCalculated = TryCalculateHeightAndGradient(map, mapSize, droplet.posX, droplet.posY, out heightAndGradient);
                if (!isCalculated)
                {
                    break;
                }

                droplet.dirX = (droplet.dirX * inertia - heightAndGradient.gradientX * (1 - inertia));
                droplet.dirY = (droplet.dirY * inertia - heightAndGradient.gradientY * (1 - inertia));

                float len = Mathf.Sqrt(droplet.dirX * droplet.dirX + droplet.dirY * droplet.dirY);
                if (len != 0)
                {
                    droplet.dirX /= len;
                    droplet.dirY /= len;
                }
                droplet.posX += droplet.dirX;
                droplet.posY += droplet.dirY;

                nodeX = (int)droplet.posX;
                nodeY = (int)droplet.posY;

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

                HeightAndGradient newHeightAndGradient;
                isCalculated = TryCalculateHeightAndGradient(map, mapSize, droplet.posX, droplet.posY, out newHeightAndGradient);
                if (!isCalculated)
                {
                    break;
                }
                float newHeight = newHeightAndGradient.height;
                float deltaHeight = newHeight - heightAndGradient.height;

                if (deltaHeight > 0)
                {
                    if (lifetime == maxDropletLifetime - 1)
                    {
                        if (poolMap[nodeY, nodeX] > map[nodeY, nodeX])
                        {
                            AddDropletToPool(poolMap, map, droplet, mapSize);
                        }
                        else
                        {
                            CreatePool(poolMap, map, droplet, mapSize);
                        }
                        break;
                    }
                }
                else
                {
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
            foreach (var point in pool.Points)
            {
                if (coordX == point.X && coordY == point.Y)
                {
                    return pool;
                }
            }
        }
        throw new Exception();
    }

    void DebugPointsCountThatHasWaterButNotInPool(float[,] poolMap, float[,] map, int mapSize)
    {
        var sum = 0;
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                if (poolMap[y, x] > map[y, x])
                    try
                    {
                        FindPool(x, y);
                    }
                    catch
                    {
                        sum++;
                    }
            }
        }
        Debug.Log($"points with misstake count = {sum}");
        if (sum > 0)
        {
            throw new Exception();
        }
    }

    void AddDropletToPool(float[,] poolMap, float[,] map, WaterDroplet droplet, int mapSize)
    {
        int coordY = (int)droplet.posY;
        int coordX = (int)droplet.posX;
        var pool = FindPool(coordX, coordY);

        pool.Volume += droplet.volume;
        AddVolumeEqually(poolMap, pool, droplet.volume);

        CorrectPool(poolMap, map, mapSize, pool);
    }

    int id = 0;
    void CreatePool(float[,] poolMap, float[,] map, WaterDroplet droplet, int mapSize)
    {

        int coordY = (int)droplet.posY;
        int coordX = (int)droplet.posX;

        var initialPoolVolume = droplet.volume;
        var FirstLowestPointOfLeak = new Point(coordX, coordY, map[coordY, coordX]);
        var points = new List<Point>
        {
            FirstLowestPointOfLeak
        };
        var pool = new Pool(points, initialPoolVolume, id);  
        id++;
        poolMap[coordY, coordX] = map[coordY, coordX] + initialPoolVolume;
        pools.Add(pool);

        CorrectPool(poolMap, map, mapSize, pool);
    }

    private void CorrectPool(float[,] poolMap, float[,] map, int mapSize, Pool pool)
    {
    Again:
        FindLowestPointOfLeak(map, poolMap, pool, mapSize); 

        var testingPoint = pool.Points.First();
        while (pool.LowestPointOfLeak.Height + epsilon < poolMap[testingPoint.Y, testingPoint.X])
        {
            if (pool.Points.FirstOrDefault(point => point.X == pool.LowestPointOfLeak.X && point.Y == pool.LowestPointOfLeak.Y) != null)
            {
                break; 
            }

            if (poolMap[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X] > map[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X]) 
            {
                var anotherPool = FindPool(pool.LowestPointOfLeak.X, pool.LowestPointOfLeak.Y);
                MergeTwoPools(map, poolMap, pool, anotherPool);
                goto Again;
            }

            AddPointInPool(map, poolMap, pool, pool.LowestPointOfLeak); 

            while (!IsPoolCorrect(map, poolMap, pool))
            {
                CorrectPoolOnce(map, poolMap, pool);
            }

            testingPoint = pool.Points.First();  

            FindLowestPointOfLeak(map, poolMap, pool, mapSize);
        }
    }

    void FindLowestPointOfLeak(float[,] map, float[,] poolMap, Pool pool, int mapSize)
    {
        var biggestDif = 0f;
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
                    if (pool.Points.FirstOrDefault(poolPoint => poolPoint.Y == point.Y + y && poolPoint.X == point.X + x) == null) 
                    {
                        if (poolMap[point.Y, point.X] > map[point.Y + y, point.X + x]) 
                        {
                            if(poolMap[point.Y, point.X] - map[point.Y + y, point.X + x] > biggestDif)
                            {
                                biggestDif = poolMap[point.Y, point.X] - map[point.Y + y, point.X + x];
                                pool.LowestPointOfLeak = new Point(point.X + x, point.Y + y, map[point.Y + y, point.X + x]);
                            }
                        }
                    }
                }
            }
        }
    }

    void AddPointInPool(float[,] map, float[,] poolMap, Pool pool, Point pointToAdd)
    {
        var testingPoint = pool.Points.First(); 

        var dif = poolMap[testingPoint.Y, testingPoint.X] - map[pointToAdd.Y, pointToAdd.X];

        pool.Points.Add(pointToAdd);
        poolMap[pointToAdd.Y, pointToAdd.X] = map[pointToAdd.Y, pointToAdd.X] + dif;

        foreach (var point in pool.Points) 
        {
            poolMap[point.Y, point.X] -= dif / pool.Points.Count;
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
        foreach (var point in pool.Points)
        {
            volume += poolMap[point.Y, point.X] - map[point.Y, point.X];
        }
        if (volume < 0)
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
            pool.Points.Remove(baddestPoint); 
            foreach (var point in pool.Points)
            {
                poolMap[point.Y, point.X] -= biggestDif / pool.Points.Count;
            }
        }
    }

    void MergeTwoPools(float[,] map, float[,] poolMap, Pool pool, Pool secondPool)
    {
        foreach (var point in secondPool.Points) 
        {
            poolMap[point.Y, point.X] = 0;
        }
        AddVolumeEqually(poolMap, pool, secondPool.Volume); 
        pool.Volume += secondPool.Volume;

        foreach (var pointToAdd in secondPool.Points) 
        {
            AddPointInPool(map, poolMap, pool, pointToAdd);
        }

        while (!IsPoolCorrect(map, poolMap, pool)) 
        {
            CorrectPoolOnce(map, poolMap, pool);
        }

        pools.Remove(secondPool);
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

        float x = posX - coordX;
        float y = posY - coordY;

        float heightNW = nodes[coordY, coordX];
        float heightNE = nodes[coordY, coordX + 1];
        float heightSW = nodes[coordY + 1, coordX];
        float heightSE = nodes[coordY + 1, coordX + 1];

        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

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

