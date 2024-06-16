using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamManager : MonoBehaviour
{
    [SerializeField] int damXStart = 10;
    [SerializeField] int damXFinish = 15;
    [SerializeField] int damYStart = 20;
    [SerializeField] int damYFinish = 30;

    public static DamManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void BuildDam(float[,] map, float[,] poolMap, int mapSizeWithBorder)
    {
        var prevMap = map.Clone();
        var sum = 0f;
        var count = 0;
        for (int y = damYStart; y < damYFinish; y++)
        {
            for (int x = damXStart; x < damXFinish; x++)
            {
                sum += map[y, x];
                count++;
            }
        }

        for (int y = damYStart; y < damYFinish; y++)
        {
            for (int x = damXStart; x < damXFinish; x++)
            {
                map[y, x] = sum / count * 1.3f;
            }
        }
        PoolGeneratorNonDynamic.Instance.RebuildPools(prevMap as float[,], map, poolMap, mapSizeWithBorder, damYStart, damYFinish, damXStart, damXFinish);
    }
}
