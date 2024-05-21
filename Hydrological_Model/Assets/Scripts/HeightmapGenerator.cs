using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class HeightmapGenerator : MonoBehaviour
{
    public static HeightmapGenerator Instance { get; private set; }
    [SerializeField] int seed;
    [SerializeField] bool randomizeSeed;

    [SerializeField] int numOctaves = 7;
    [SerializeField] float persistence = .5f;
    [SerializeField] float lacunarity = 2;
    [SerializeField] float initialScale = 2;


    private void Awake()
    {
        Instance = this;
    }

    public float[,] GenerateHeightMap(int mapSize)
    {
        var map = new float[mapSize,mapSize];
        seed = (randomizeSeed) ? Random.Range(-10000, 10000) : seed;
        var prng = new System.Random(seed);

        Vector2[] offsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++)
        {
            offsets[i] = new Vector2(prng.Next(-1000, 1000), prng.Next(-1000, 1000));
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float noiseValue = 0;
                float scale = initialScale;
                float weight = 1;
                for (int i = 0; i < numOctaves; i++)
                {
                    Vector2 p = offsets[i] + new Vector2(x / (float)mapSize, y / (float)mapSize) * scale;
                    noiseValue += Mathf.PerlinNoise(p.x, p.y) * weight;
                    weight *= persistence;
                    scale *= lacunarity;
                }
                map[y,x] = noiseValue;
                minValue = Mathf.Min(noiseValue, minValue);
                maxValue = Mathf.Max(noiseValue, maxValue);
            }
        }

        // Normalize
        if (maxValue != minValue)
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    map[i,j] = (map[i,j] - minValue) / (maxValue - minValue);
                }
            }
        }
        Debug.Log("Map created succesfully");
        return map;
    }
}
