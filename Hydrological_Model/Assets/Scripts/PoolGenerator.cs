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
    [SerializeField] float inertia = .05f; // При нулевом значении вода мгновенно изменит направление и потечет вниз по склону.
                                           // При значении 1 вода никогда не изменит направление.
    [Range(0, 1)]
    [SerializeField] float depositSpeed = .3f;
    [Range(0, 1)]
    [SerializeField] float evaporateSpeed = .01f;
    [SerializeField] float gravity = 4;
    [SerializeField] int maxDropletLifetime = 300;

    [SerializeField] float initialWaterVolume = 0.001f;
    [SerializeField] float initialSpeed = 1;

    // Indices and weights of erosion brush precomputed for every node
    System.Random prng;

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
                    /* poolMap[nodeY, nodeX] += droplet.volume; //навалили объёма
                     var isDropletAdded = TryAddDropletToPool(map, poolMap, mapSize, nodeY, nodeX, droplet);
                     if (!isDropletAdded)
                     {
                         poolMap[nodeY, nodeX] -= droplet.volume;
                     }
                     else
                     {
                         Debug.Log("droplet beacme a part of a existing pool");
                         break;
                     }*/
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
        var pool = new Pool(points, initialPoolVolume);  //Создан бассейн в одной точке 
        poolMap[coordY, coordX] = map[coordY, coordX] + initialPoolVolume;

        FindLowestPointOfLeak(map, poolMap, pool, mapSize); //Точка утечки новая или равна уровню воды

        var testingPoint = pool.Points.First();
        while (pool.LowestPointOfLeak.Height < poolMap[testingPoint.Y, testingPoint.X])
        {
            Debug.Log("while iteration");
            if (pool.Points.Contains(pool.LowestPointOfLeak))
            {
                Debug.Log("pool contains lowestPointOfLeak");
                break;
            }

            AddPointInPool(map, poolMap, pool);

            while (!IsPoolCorrect(map, poolMap, pool))
            {
                CorrectPool(map, poolMap, pool);
            }

            testingPoint = pool.Points.First(); //Обновляем точку тестирования, потому что старая могла выйти из бассейна

            FindLowestPointOfLeak(map,poolMap, pool, mapSize);
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
                    if (pool.Points.FirstOrDefault(poolPoint => poolPoint.Y == point.Y + y && poolPoint.X == point.X + x) == null) //Если соседняя точка не в бассейне
                    {
                        if (poolMap[point.Y, point.X] > map[point.Y + y, point.X + x]) //Найдена более низкая точка 
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
        var lowestPoint = pool.LowestPointOfLeak; //Для удобства
        var testingPoint = pool.Points.First(); //Для удобства

        var dif = poolMap[testingPoint.Y, testingPoint.X] - map[lowestPoint.Y, lowestPoint.X];

        pool.Points.Add(lowestPoint); //Точка стала частью бассейна
        poolMap[lowestPoint.Y, lowestPoint.X] = map[lowestPoint.Y, lowestPoint.X] + dif; //Посчитали в точке пулмап

        foreach (var point in pool.Points) //Пересчитали пулмап для других точек
        {
            poolMap[point.Y, point.X] -= dif / pool.Points.Count;
            // Debug.Log($"poolmap в точке из которой вычитаем {poolMap[point.Y, point.X]}");
        }


        foreach (var point in pool.Points)
        {
            Debug.Log(poolMap[point.Y, point.X]);
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

    void CorrectPool(float[,] map, float[,] poolMap, Pool pool)
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
            Debug.Log("Point was removed");
            foreach (var point in pool.Points)
            {
                poolMap[point.Y, point.X] -= biggestDif / pool.Points.Count;
            }
        }
    }

    /* bool TryAddDropletToPool(float[,] map, float[,] poolMap, int mapSize, int nodeY, int nodeX, WaterDroplet droplet)
     {
         GetPoolData(map, poolMap, mapSize, nodeY, nodeX);

         if (lowestPointOfLeakHeight < poolHeight / pointsInPoolCount)
         {
             droplet.posX = lowestPointOfLeakX;
             droplet.posY = lowestPointOfLeakY;
             droplet.speed = initialSpeed;


             pointsInPoolCount = 0;
             lowestPointOfLeakHeight = float.MaxValue;
             lowestPointOfLeakX = 0;
             lowestPointOfLeakY = 0;
             poolHeight = 0;
             checkedYs = new List<int>();
             checkedXs = new List<int>();

             return false;
         }

         foreach (var y in checkedYs)
         {
             foreach (var x in checkedXs)
             {
                 poolMap[y, x] = poolHeight / pointsInPoolCount;
             }
         }

         pointsInPoolCount = 0;
         lowestPointOfLeakHeight = float.MaxValue;
         lowestPointOfLeakX = 0;
         lowestPointOfLeakY = 0;
         poolHeight = 0;
         checkedYs = new List<int>();
         checkedXs = new List<int>();
         return true;
     }*/

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



/*void CorrectPoolCoordinates(float[,] map, float[,] poolMap, int mapSize, int nodeY, int nodeX)
{
    if (poolMap[nodeY, nodeX] <= map[nodeY, nodeX]) //Если в точке нет воды, рекурсивная корректировка бассейна кончается
        return;

    var sumToCorrect = 0;


    for (int y = -1; y <= 1; y++)
    {
        if (nodeY + y < 0 || nodeY + y >= mapSize)
        {
            continue;
        }
        for (int x = -1; x <= 1; x++)
        {
            if (nodeX + x < 0 || nodeX + x >= mapSize)
            {
                continue;
            }
            if (poolMap[nodeY, nodeX] <= poolMap[nodeY + y, nodeX + x] || poolMap[nodeY, nodeX] <= map[nodeY + y, nodeX + x])
                sumToCorrect++;
        }
    }
    if (sumToCorrect == 0)
        return;

    for (int y = -1; y <= 1; y++)
    {
        if (nodeY + y < 0 || nodeY + y >= mapSize)
        {
            continue;
        }
        for (int x = -1; x <= 1; x++)
        {
            if (nodeX + x < 0 || nodeX + x >= mapSize)
            {
                continue;
            }
            if (poolMap[nodeY, nodeX] > poolMap[nodeY + y, nodeX + x])
            {
                float volumeToSplit;
                if (poolMap[nodeY + y, nodeX + x] > map[nodeY + y, nodeX + x]) //Если в точке есть вода
                {
                    volumeToSplit = poolMap[nodeY, nodeX] - poolMap[nodeY + y, nodeX + x] / 2.0f;
                }
                else //Если в точке нет воды
                {
                    poolMap[nodeY + y, nodeX + x] += map[nodeY + y, nodeX + x]; //Поднимаем воду до уровня суши
                    volumeToSplit = poolMap[nodeY, nodeX] - map[nodeY + y, nodeX + x] / 2.0f;
                }
                poolMap[nodeY + y, nodeX + x] += volumeToSplit;
                poolMap[nodeY, nodeX] -= volumeToSplit;
            }
            else
            {
                break;
            }

            if (x != 0 && y != 0)
            {
                CorrectPoolCoordinates(map, poolMap, mapSize, nodeX + x, nodeY + y);
            }
        }
    }
}*/