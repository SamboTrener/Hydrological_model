using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using static WaterDropletExtensions;

public class DropletPathDrawer : MonoBehaviour
{
    public static DropletPathDrawer Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] float erodeOnceFrameSpeed;

    [SerializeField] float evaporateSpeed = .01f;
    [SerializeField] float gravity = 4;
    [SerializeField] int maxDropletLifetime = 30;

    [SerializeField] float initialWaterVolume = 1;
    [SerializeField] float initialSpeed = 1;
    [Range(0, 1)]
    [SerializeField] float inertia = .05f;

    [SerializeField] bool randomizeSeed;
    [SerializeField] int seed;

    System.Random prng;

    void Initialize()
    {
        seed = (randomizeSeed) ? UnityEngine.Random.Range(-10000, 10000) : seed;
        prng = new System.Random(seed);
    }

    public IEnumerator ShowOneDropPath(float[,] map, int mapSize, float scale, int numIterations = 30000)
    {
        Initialize();
        lineRenderer.positionCount = 0;


        var droplet = new WaterDroplet(prng.Next(0, mapSize - 1), prng.Next(0, mapSize - 1), 0, 0, initialSpeed, initialWaterVolume);

        for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
        {
            int nodeX = (int)droplet.posX;
            int nodeY = (int)droplet.posY;

            lineRenderer.positionCount++; //Плюс одна точка в лайн рендере
            lineRenderer.SetPosition(lifetime, new Vector3(nodeX, map[nodeY, nodeX] * scale, nodeY)); //Ставим эту точку туда где щас находимся

            var dropletShouldContinue = droplet.TryMoveDroplet(map, mapSize, inertia, gravity, evaporateSpeed);
            if (!dropletShouldContinue)
            {
                break;
            }

            yield return new WaitForSeconds(erodeOnceFrameSpeed);
        }
    }
}
