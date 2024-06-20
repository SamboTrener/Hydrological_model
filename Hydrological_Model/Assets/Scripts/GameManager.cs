using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject cubeRed;
    [SerializeField] GameObject cubeGreen;
    [SerializeField] GameObject resultsWindow;
    [SerializeField] TMP_Text resultsText;

    Point pointOfLose;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnTargets(float[,] map, int x, int y, float elevationScale, int xStartOfWater, int yStartOfWater) 
    {
        pointOfLose = new Point(x,y, map[y,x]);
        Instantiate(cubeRed, new Vector3(x, map[y,x] * elevationScale,y), Quaternion.identity);
        Instantiate(cubeGreen, new Vector3(xStartOfWater, map[yStartOfWater, xStartOfWater] * elevationScale, yStartOfWater), Quaternion.identity);
    }

    public void GetResults(float[,] poolMap)
    {
        resultsWindow.SetActive(true);

        if (poolMap[pointOfLose.Y, pointOfLose.X] > 0)
        {
            resultsText.text = "Вы проиграли";
        }
        else
        {
            resultsText.text = "Вы победили";
        }
    }
}
