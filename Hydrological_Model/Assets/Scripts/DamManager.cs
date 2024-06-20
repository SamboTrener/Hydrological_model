using TMPro;
using UnityEngine;

public class DamManager : MonoBehaviour
{
    [SerializeField] TMP_InputField xStartInput;
    [SerializeField] TMP_InputField yStartInput;
    [SerializeField] TMP_InputField xFinishInput;
    [SerializeField] TMP_InputField yFinishInput;
    int xStart = 10;
    int xFinish = 15;
    int yStart = 20;
    int yFinish = 30;

    public static DamManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void BuildDam(float[,] map, float[,] poolMap, int mapSizeWithBorder)
    {
        xStart = int.Parse(xStartInput.text);
        xFinish = int.Parse(xFinishInput.text);
        yStart = int.Parse(yStartInput.text);
        yFinish = int.Parse(yFinishInput.text);

        var prevMap = map.Clone();
        var sum = 0f;
        var count = 0;
        for (int y = yStart; y < yFinish; y++)
        {
            for (int x = xStart; x < xFinish; x++)
            {
                sum += map[y, x];
                count++;
            }
        }

        for (int y = yStart; y < yFinish; y++)
        {
            for (int x = xStart; x < xFinish; x++)
            {
                map[y, x] = sum / count * 1.3f;
            }
        }
        PoolGeneratorNonDynamic.Instance.RebuildPools(prevMap as float[,], map, poolMap, mapSizeWithBorder, yStart, yFinish, xStart, xFinish);
    }
}
