using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class PerlinNoiseGenerator
{
    #region Public Methods
    /// <summary>
    /// Генерирует карту высот на основе переданных параметров
    /// </summary>
    /// <param name="side">Ширина карты.</param>
    /// <param name="height">Длина карты.</param>
    /// <param name="scale">Размер шума Перлина.</param>
    /// <param name="offsetX">Смещение шума по оси X.</param>
    /// <param name="offsetY">Смещение шума по оси Y.</param>
    public static void GeneratePerlinNoiseHeightMap(this float[,] heights, int side, int height, float scale, float offsetX, float offsetY)
    {
        //var map = new float[side, height];

        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                float xCoord = (float)x / side * scale + offsetX; 
                float yCoord = (float)y / height * scale + offsetY;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                heights[x, y] = sample;
            }
        }

        //return map;
    }
    #endregion
}
