using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class Erosion : MonoBehaviour
{
    /*[SerializeField] int iterationsCount;
    [SerializeField] int maxDropletLifetime;

    struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }

    void Erode(float[,] map)
    {
        var prng = new System.Random();
        for (var iter = 0; iter < iterationsCount; iter++)
        {
            var posX = prng.Next(0, map.GetLength(0) - 1);
            var posY = prng.Next(0, map.GetLength(1) - 1);
            var dirX = 0f;
            var dirY = 0f;
            var speed = 1f;
            var initialWaterVolume = 1f;
            var sediment = 0f;

            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;


            }
        }
    }

    HeightAndGradient CalculateHeightAndGradient(float[,] nodes, float posX, float posY)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        var heightsRight = nodes[coordX, coordY];

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }*/
}
