using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WaterDropletExtensions;

public class PoolGeneratorNonDynamic : MonoBehaviour
{
    public static PoolGeneratorNonDynamic Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] bool randomizeSeed;
    [SerializeField] int seed;
    [Range(0, 1)]
    [SerializeField] float inertia = .05f; // При нулевом значении вода мгновенно изменит направление и потечет вниз по склону.
                                           // При значении 1 вода никогда не изменит направление.
    [Range(0, 1)]
    [SerializeField] float evaporateSpeed = .01f;
    [SerializeField] float gravity = 4;
    [SerializeField] int maxDropletLifetime = 300;

    [SerializeField] float initialWaterVolume = 0.01f;
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

    public void GeneratePools(float[,] map, float[,] poolMap, int mapSize, int numIterations, int? xStart = null, int? yStart = null)
    {
        Initialize();

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            // Создание капли в рандомной точке карты
            int xStartBased = xStart ?? prng.Next(0, mapSize - 1);
            int yStartBased = yStart ?? prng.Next(0, mapSize - 1);
            var droplet = new WaterDroplet(xStartBased, yStartBased, 0, 0, initialSpeed, initialWaterVolume);

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

                var shouldDropletContinue = droplet.TryMoveDroplet(map, mapSize, inertia, gravity, evaporateSpeed);

                nodeX = (int)droplet.posX;
                nodeY = (int)droplet.posY;

                if (!shouldDropletContinue)
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


                // if flowing uphill:
                if (LastDeltaHeight > 0)
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
                    if (lifetime == maxDropletLifetime - 1)
                    {
                        lifetime--;
                    }
                }
            }
        }
        foreach (var pool in pools)
        {
            if (pool.Points.Count > 0)
            {
                CorrectPool(poolMap, map, mapSize, pool);
            }
        }
    }

    public void RebuildPools(float[,] prevMap, float[,] map, float[,] poolMap, int mapSize, int yStart, int yFinish, int xStart, int xFinish)
    {
        List<Pool> CheckedPools = new List<Pool>();
        for (int y = yStart; y < yFinish; y++)
        {
            for (int x = xStart; x < xFinish; x++)
            {
                if (poolMap[y, x] < map[y, x] && poolMap[y, x] > 0)
                {
                    var volumeToShare = poolMap[y, x] - prevMap[y, x];
                    poolMap[y, x] = 0;
                    var poolToFix = FindPool(x, y);
                    var pointToRemove = poolToFix.Points.First(point => point.X == x && point.Y == y);
                    poolToFix.Points.Remove(pointToRemove);

                    AddVolumeEqually(poolMap, poolToFix, volumeToShare);
                    if (!CheckedPools.Contains(poolToFix))
                    {
                        CheckedPools.Add(poolToFix);
                    }
                }
            }
        }
        var poolsToRemove = new List<Pool>();
        foreach (var pool in CheckedPools)
        {
            if (pool.Points.Count > 0)
            {
                CorrectPool(poolMap, map, mapSize, pool);
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

    void AddDropletToPool(float[,] poolMap, float[,] map, WaterDroplet droplet, int mapSize)
    {
        int coordY = (int)droplet.posY;
        int coordX = (int)droplet.posX;
        var pool = FindPool(coordX, coordY);

        pool.Volume += droplet.volume;
        AddVolumeEqually(poolMap, pool, droplet.volume);

        // CorrectPool(poolMap, map, mapSize, pool);
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
        var pool = new Pool(points, initialPoolVolume, id);  //Создан бассейн в одной точке 
        id++;
        poolMap[coordY, coordX] = map[coordY, coordX] + initialPoolVolume;
        pools.Add(pool);

        // CorrectPool(poolMap, map, mapSize, pool);
    }

    private void CorrectPool(float[,] poolMap, float[,] map, int mapSize, Pool pool)
    {
    Again:
        FindLowestPointOfLeak(map, poolMap, pool, mapSize); //Точка утечки новая или равна уровню воды

        var testingPoint = pool.Points.First();
        while (pool.LowestPointOfLeak.Height + epsilon < poolMap[testingPoint.Y, testingPoint.X])
        {
            if (pool.Points.FirstOrDefault(point => point.X == pool.LowestPointOfLeak.X && point.Y == pool.LowestPointOfLeak.Y) != null)
            {
                break;
            }

            if (poolMap[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X] > map[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X]) //В точке базируется другой бассейн
            {
                var anotherPool = FindPool(pool.LowestPointOfLeak.X, pool.LowestPointOfLeak.Y);
                MergeTwoPools(map, poolMap, pool, anotherPool);
                goto Again;
            }

            AddPointInPool(map, poolMap, pool, pool.LowestPointOfLeak); //После этого ломается бассейн 678 temp

            while (!IsPoolCorrect(map, poolMap, pool))
            {
                CorrectPoolOnce(map, poolMap, pool);
            }

            testingPoint = pool.Points.First();  //Обновляем точку тестирования, потому что старая могла выйти из бассейна

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
                    if (pool.Points.FirstOrDefault(poolPoint => poolPoint.Y == point.Y + y && poolPoint.X == point.X + x) == null) //Если соседняя точка не в бассейне
                    {
                        if (poolMap[point.Y, point.X] > map[point.Y + y, point.X + x]) //Найдена более низкая точка относительно уровня
                        {
                            if (poolMap[point.Y, point.X] - map[point.Y + y, point.X + x] > biggestDif)
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
        var testingPoint = pool.Points.First(); //Для удобства

        var dif = poolMap[testingPoint.Y, testingPoint.X] - map[pointToAdd.Y, pointToAdd.X];

        pool.Points.Add(pointToAdd); //Точка стала частью бассейна
        poolMap[pointToAdd.Y, pointToAdd.X] = map[pointToAdd.Y, pointToAdd.X] + dif; //Посчитали в точке пулмап

        foreach (var point in pool.Points) //Пересчитали пулмап для других точек
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
            pool.Points.Remove(baddestPoint); //Есть некая проблема, что бассейн может разделиться на несколько, но на уровне кода это всё ещё один бассейн
            foreach (var point in pool.Points)
            {
                poolMap[point.Y, point.X] -= biggestDif / pool.Points.Count;
            }
        }
    }

    void MergeTwoPools(float[,] map, float[,] poolMap, Pool pool, Pool secondPool)
    {
        foreach (var point in secondPool.Points) //poolMap зануляем, будем добавлять точки с нуля 
        {
            poolMap[point.Y, point.X] = 0;
        }
        AddVolumeEqually(poolMap, pool, secondPool.Volume); //Размазали объём второго бассейна по первому
        pool.Volume += secondPool.Volume;

        foreach (var pointToAdd in secondPool.Points) //Добавляем все точки второго бассейна в первый
        {
            AddPointInPool(map, poolMap, pool, pointToAdd);
        }
        secondPool.Points.Clear();

        while (!IsPoolCorrect(map, poolMap, pool)) //Коррекция
        {
            CorrectPoolOnce(map, poolMap, pool);
        }
    }
}
