using System;
using System.Collections.Generic;
using UnityEngine;
using static WaterDropletExtensions;
using Random = UnityEngine.Random;

public class ErosionGenerator : MonoBehaviour
{
    public static ErosionGenerator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    [SerializeField] bool randomizeSeed;
    [SerializeField] int seed;
    [Range(2, 8)]
    [SerializeField] int erosionRadius = 3;
    [Range(0, 1)]
    [SerializeField] float inertia = .05f; // ѕри нулевом значении вода мгновенно изменит направление и потечет вниз по склону.
                                           // ѕри значении 1 вода никогда не изменит направление.

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

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            var droplet = new WaterDroplet(prng.Next(0, mapSize - 1), prng.Next(0, mapSize - 1), 0, 0, initialSpeed, initialWaterVolume, 0);

            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                int nodeX = (int)droplet.posX;
                int nodeY = (int)droplet.posY;

                float cellOffsetX = droplet.posX - nodeX;
                float cellOffsetY = droplet.posY - nodeY;

                var shouldDropletContinue = droplet.TryMoveDroplet(map, mapSize, inertia, gravity, evaporateSpeed);
                if (!shouldDropletContinue)
                {
                    break;
                }

                float sedimentCapacity = Mathf.Max(-LastDeltaHeight * droplet.speed * droplet.volume * sedimentCapacityFactor, minSedimentCapacity);

                // If carrying more sediment than capacity, or if flowing uphill:
                if (droplet.sediment > sedimentCapacity || LastDeltaHeight > 0)
                {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (LastDeltaHeight > 0) ? Mathf.Min(LastDeltaHeight, droplet.sediment) : (droplet.sediment - sedimentCapacity) * depositSpeed;
                    droplet.sediment -= amountToDeposit;

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
                    float amountToErode = Mathf.Min((sedimentCapacity - droplet.sediment) * erodeSpeed, -LastDeltaHeight);

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    foreach (var element in precalculatedWeights[nodeY, nodeX])
                    {
                        var extraX = element.x;
                        var extraY = element.y;
                        float weighedErodeAmount = amountToErode * element.weight;
                        float deltaSediment = (map[extraY, extraX] < weighedErodeAmount) ? map[extraY, extraX] : weighedErodeAmount;

                        map[extraY, extraX] -= deltaSediment;

                        droplet.sediment += deltaSediment;
                    }
                }
            }
        }
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
}
