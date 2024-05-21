using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

public class ErosionGenerator : MonoBehaviour
{
    public static ErosionGenerator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] LineRenderer lineRenderer; //test
    [SerializeField] float erodeOnceFrameSpeed;

    [SerializeField] bool randomizeSeed;
    [SerializeField] int seed;
    [Range(2, 8)]
    [SerializeField] int erosionRadius = 3;
    [Range(0, 1)]
    [SerializeField] float inertia = .05f; // При нулевом значении вода мгновенно изменит направление и потечет вниз по склону.
                                           // При значении 1 вода никогда не изменит направление.

    [SerializeField] float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
    [SerializeField] float minSedimentCapacity = .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain
    [Range(0, 1)]
    [SerializeField] float erodeSpeed = .3f;
    [Range(0, 1)]
    [SerializeField] float depositSpeed = .3f;
    [Range(0, 1)]
    [SerializeField] float evaporateSpeed = .01f;
    [SerializeField] float gravity = 4;
    [SerializeField] int maxDropletLifetime = 30;

    [SerializeField] float initialWaterVolume = 1;
    [SerializeField] float initialSpeed = 1;

    // Indices and weights of erosion brush precomputed for every node
    System.Random prng;

    PointAndWeight[,][] precalculatedWeights;

    // Initialization creates a System.Random object and precomputes indices and weights of erosion brush
    void Initialize(int mapSize, float[,] map)
    {
        seed = (randomizeSeed) ? Random.Range(-10000, 10000) : seed;
        prng = new System.Random(seed);
        if (precalculatedWeights == null)
        {
            precalculatedWeights = PrecalculateWeights(map, mapSize);
        }
    }

    public void Erode(float[,] map, int mapSize, int numIterations = 30000)
    {
        Initialize(mapSize, map);

        //temp
        lineRenderer.positionCount = 0;

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            // Создание капли в рандомной точке карты
            float posX = prng.Next(0, mapSize - 1);
            float posY = prng.Next(0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = initialSpeed;
            float water = initialWaterVolume;
            float sediment = 0; //Осадок 


            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

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
                if(water <= 0)
                {
                    Debug.Log("water droplet volume less then 0");
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient(map, mapSize, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                // If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    map[nodeY, nodeX] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[nodeY, nodeX + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    map[nodeY + 1, nodeX] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[nodeY + 1, nodeX + 1] += amountToDeposit * cellOffsetX * cellOffsetY; 
                }
                else
                {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    foreach (var element in precalculatedWeights[nodeY, nodeX])
                    {
                        var extraX = element.x;
                        var extraY = element.y;
                        float weighedErodeAmount = amountToErode * element.weight;
                        float deltaSediment = (map[extraY, extraX] < weighedErodeAmount) ? map[extraY, extraX] : weighedErodeAmount;

                        map[extraY, extraX] -= deltaSediment;

                        sediment += deltaSediment;
                        water -= deltaSediment; //Вроде поломка краёв теперь через раз 
                    }

                    // Update droplet's speed and water content
                    speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
                    water *= (1 - evaporateSpeed);
                }
            }
        }
    }

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


    class PointAndWeight
    {
        public PointAndWeight(int y, int x, float weight)
        {
            this.y = y;
            this.x = x;
            this.weight = weight;
        }

        public int x;
        public int y;
        public float weight;
    }

    PointAndWeight[] GetWeights(float[,] map, int mapSize, int currentY, int currentX)
    {
        var pointsAndWeights = new List<PointAndWeight>();
        float weightsSum = 0f;
        for (int y = -erosionRadius; y < erosionRadius; y++)
        {
            var extraY = currentY - y;
            if (extraY < 0 || extraY >= mapSize)
            {
                continue;
            }
            for (int x = -erosionRadius; x < erosionRadius; x++)
            {
                var extraX = currentX - x;
                if (extraX < 0 || extraX >= mapSize)
                {
                    continue;
                }
                var linearDicrease = Mathf.Abs(map[extraY, extraX] - map[currentY, currentX]);
                var weight = Mathf.Max(0, erosionRadius - linearDicrease);
                weightsSum += weight;
                pointsAndWeights.Add(new PointAndWeight(extraY, extraX, weight));
            }
        }

        for (int i = 0; i < pointsAndWeights.Count; i++)
        {
            pointsAndWeights[i].weight = pointsAndWeights[i].weight / weightsSum;
        }

        return pointsAndWeights.ToArray();
    }

    PointAndWeight[,][] PrecalculateWeights(float[,] map, int mapSize)
    {
        var precalculatedWeights = new PointAndWeight[mapSize, mapSize][];
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                var weights = GetWeights(map, mapSize, y, x);
                precalculatedWeights[y, x] = weights;
            }
        }
        return precalculatedWeights;
    }

    public IEnumerator ErodeOnce(float[,] map, int mapSize, float scale, int numIterations = 30000)
    {
        Initialize(mapSize, map);
        lineRenderer.positionCount = 0;

        float posX = prng.Next(0, mapSize - 1);
        float posY = prng.Next(0, mapSize - 1);
        float dirX = 0;
        float dirY = 0;
        float speed = initialSpeed;
        float water = initialWaterVolume;
        float sediment = 0; //Осадок

        for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
        {
            int nodeX = (int)posX;
            int nodeY = (int)posY;
            // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float cellOffsetX = posX - nodeX;
            float cellOffsetY = posY - nodeY;

            lineRenderer.positionCount++; //Плюс одна точка в лайн рендере
            lineRenderer.SetPosition(lifetime, new Vector3(nodeX, map[nodeY, nodeX] * scale + 0.1f, nodeY)); //Ставим эту точку туда где щас находимся


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

            // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
            float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

            // If carrying more sediment than capacity, or if flowing uphill:
            if (sediment > sedimentCapacity || deltaHeight > 0)
            {
                // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                sediment -= amountToDeposit;

                // Add the sediment to the four nodes of the current cell using bilinear interpolation
                // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                map[nodeY, nodeX] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                map[nodeY, nodeX + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                map[nodeY + 1, nodeX] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                map[nodeY + 1, nodeX + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

            }
            else
            {
                // Erode a fraction of the droplet's current carry capacity.
                // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);
                // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                Debug.Log(precalculatedWeights[nodeY, nodeX].Length);
                foreach (var element in precalculatedWeights[nodeY, nodeX])
                {
                    var extraX = element.x;
                    var extraY = element.y;
                    float weighedErodeAmount = amountToErode * element.weight;
                    float deltaSediment = (map[extraY, extraX] < weighedErodeAmount) ? map[extraY, extraX] : weighedErodeAmount;

                    map[extraY, extraX] -= deltaSediment;

                    sediment += deltaSediment;
                }

                // Update droplet's speed and water content
                speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
                water *= (1 - evaporateSpeed);
            }
            yield return new WaitForSeconds(erodeOnceFrameSpeed);
        }
    }
}
