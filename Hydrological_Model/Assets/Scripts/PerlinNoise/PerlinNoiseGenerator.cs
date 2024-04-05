using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class PerlinNoiseGenerator
{
    #region Public Methods
    /// <summary>
    /// ���������� ����� ����� �� ������ ���������� ����������
    /// </summary>
    /// <param name="side">������ �����.</param>
    /// <param name="height">����� �����.</param>
    /// <param name="scale">������ ���� �������.</param>
    /// <param name="offsetX">�������� ���� �� ��� X.</param>
    /// <param name="offsetY">�������� ���� �� ��� Y.</param>
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
