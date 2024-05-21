using System;
using System.Collections;
using System.Collections.Generic;
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
            float posX = prng.Next(0, mapSize - 1);
            float posY = prng.Next(0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = initialSpeed;
            float water = initialWaterVolume;


            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {

                int nodeX = (int)posX;
                int nodeY = (int)posY;

                if (lifetime == maxDropletLifetime - 1)
                {
                    dirX = 0;
                    dirY = 0;
                }

                if (poolMap[nodeY, nodeX] > 0)
                {
                    poolMap[nodeY, nodeX] += water; //навалили объёма
                    CorrectPoolCoordinates(map, poolMap, mapSize, nodeY, nodeX);
                    break;
                }

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, mapSize, posX, posY);

                // Меняем направление и положение капли (меняем положение на 1 независимо от скорости).
                dirX = (dirX * inertia - heightAndGradient.gradientX * (1 - inertia));
                dirY = (dirY * inertia - heightAndGradient.gradientY * (1 - inertia));
                // Normalize direction
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len != 0)
                {
                    dirX /= len;
                    dirY /= len;
                }
                posX += dirX;
                posY += dirY;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if (posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1)
                {
                    break;
                }
                if (dirX == 0 && dirY == 0)
                {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient(map, mapSize, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // if flowing uphill:
                if (deltaHeight > 0)
                {
                    if (lifetime == maxDropletLifetime - 1)
                    {
                        poolMap[nodeY, nodeX] = map[nodeY, nodeX] + water; //Капля стала бассейном
                        CorrectPoolCoordinates(map, poolMap, mapSize, nodeY, nodeX);
                    }
                }
                else
                {
                    // Update droplet's speed and water content
                    speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
                    water *= (1 - evaporateSpeed);
                    if (lifetime == maxDropletLifetime - 1)
                    {
                        lifetime--;
                    }
                }
            }
        }
    }

    void CorrectPoolCoordinates(float[,] map, float[,] poolMap, int mapSize, int nodeY, int nodeX)
    {
        GetPoolData(map, poolMap, mapSize, nodeY, nodeX);

        foreach (var y in checkedYs)
        {
            foreach (var x in checkedXs)
            {
                poolMap[y, x] = poolHeight / pointsInPoolCount;
            }
        }

        if(lowestPointOfLeak < poolHeight / pointsInPoolCount)
        {

        }
    }

    float pointsInPoolCount;
    float lowestPointOfLeak = float.MaxValue;
    float poolHeight;
    List<int> checkedYs = new List<int>();
    List<int> checkedXs = new List<int>();
    void GetPoolData(float[,] map, float[,] poolMap, int mapSize, int nodeY, int nodeX)
    {
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

                if (poolMap[nodeY + y, nodeX + x] > map[nodeY + y, nodeX + x]) //Вызов того же для соседних точек, которые в бассейне
                {
                    if(!checkedYs.Contains(nodeY + y) && !checkedXs.Contains(nodeX + x))
                    {
                        checkedXs.Add(nodeX + x);
                        checkedYs.Add(nodeY + y);
                        GetPoolData(map, poolMap, mapSize, nodeY + y, nodeX + x);
                    }
                }
                else
                {
                    if(lowestPointOfLeak > map[nodeY + y, nodeX + x])
                    {
                        lowestPointOfLeak = map[nodeY + y, nodeX + x];
                    }
                }

                poolHeight += poolMap[nodeY, nodeX];
                pointsInPoolCount++;
            }
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

    HeightAndGradient CalculateHeightAndGradient(float[,] nodes, int mapSize, float posX, float posY)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

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

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }

    struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}
