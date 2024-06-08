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

    public void GeneratePools(float[,] map, float[,] poolMap, int mapSize, int numIterations = 30000)
    {
        Initialize();

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            // Создание капли в рандомной точке карты
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

                // Меняем направление и положение капли (меняем положение на 1 независимо от скорости).
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

                nodeX = (int)droplet.posX;
                nodeY = (int)droplet.posY;

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
            foreach (var point in pool.Points)
            {
                if (coordX == point.X && coordY == point.Y)
                {
                    return pool;
                }
            }
        }
        Debug.Log($"point with coordinates has poolMap > map, but not in pool - X: {coordX}, Y: {coordY}");
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
        Debug.Log("Adding droplet to pool");

        //DebugPointsCountThatHasWaterButNotInPool(poolMap, map, mapSize);

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

        //DebugPointsCountThatHasWaterButNotInPool(poolMap, map, mapSize);

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

        CorrectPool(poolMap, map, mapSize, pool);

        //if (pool.Id == 678)
        //{
        //    Debug.Log("pool 678 volume based");
        //    DebugVolTemp(map, poolMap, pool);
        //    Debug.Log($"pool 678 volume unbased = {pool.Volume}");
        //}

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
                //  Debug.Log("pool contains lowestPointOfLeak");
                break; 
            }
            //if (pool.Points.Contains(pool.LowestPointOfLeak))
            //{
            //   break;
            //}

            if (pool.Id == 678)
            {
                Debug.Log($"point in 678 pool =  X : {pool.Points.First().X}, Y: {pool.Points.First().Y}");
                Debug.Log($"lowest point of leak in 678 pool =  X : {pool.LowestPointOfLeak.X}, Y: {pool.LowestPointOfLeak.Y}");
            }

            if (poolMap[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X] > map[pool.LowestPointOfLeak.Y, pool.LowestPointOfLeak.X]) //В точке базируется другой бассейн
            {
                var anotherPool = FindPool(pool.LowestPointOfLeak.X, pool.LowestPointOfLeak.Y);
                MergeTwoPools(map, poolMap, pool, anotherPool);
                goto Again;
            }

            AddPointInPool(map, poolMap, pool, pool.LowestPointOfLeak); //После этого ломается бассейн 678 temp

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

            testingPoint = pool.Points.First();  //Обновляем точку тестирования, потому что старая могла выйти из бассейна

            FindLowestPointOfLeak(map, poolMap, pool, mapSize);
            // Debug.Log($"pool.LowestPointOfLeak.Height = {pool.LowestPointOfLeak.Height}");
            // Debug.Log($"poolMap[testingPoint.Y, testingPoint.X] = {poolMap[testingPoint.Y, testingPoint.X]}");
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
        var testingPoint = pool.Points.First(); //Для удобства

        var dif = poolMap[testingPoint.Y, testingPoint.X] - map[pointToAdd.Y, pointToAdd.X];
        //Debug.Log($"dif = {dif}");
        //Debug.Log($"pool.points.count = {pool.Points.Count}");


        pool.Points.Add(pointToAdd); //Точка стала частью бассейна
        poolMap[pointToAdd.Y, pointToAdd.X] = map[pointToAdd.Y, pointToAdd.X] + dif; //Посчитали в точке пулмап

        foreach (var point in pool.Points) //Пересчитали пулмап для других точек
        {
            poolMap[point.Y, point.X] -= dif / pool.Points.Count;
            // Debug.Log($"poolmap в точке из которой вычитаем {poolMap[point.Y, point.X]}");
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
            pool.Points.Remove(baddestPoint); //Есть некая проблема, что бассейн может разделиться на несколько, но на уровне кода это всё ещё один бассейн
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

        Debug.Log("second pool volume based");
        DebugVolTemp(map, poolMap, secondPool);
        Debug.Log($"Second pool volume unbased = {secondPool.Volume}");

        Debug.Log("first pool volume based");
        DebugVolTemp(map, poolMap, pool);
        Debug.Log($"first pool volume unbased = {pool.Volume}");

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

        Debug.Log("vol before correction");
        DebugVolTemp(map, poolMap, pool);
        while (!IsPoolCorrect(map, poolMap, pool)) //Коррекция
        {
            CorrectPoolOnce(map, poolMap, pool);
        }
        Debug.Log("vol after correction");
        DebugVolTemp(map, poolMap, pool);

        pools.Remove(secondPool);

        Debug.Log($"points in pool count after merge - {pool.Points.Count}");
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

