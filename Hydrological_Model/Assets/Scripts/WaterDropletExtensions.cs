using System;
using UnityEngine;

public static class WaterDropletExtensions
{
    public static float LastDeltaHeight;

    public static bool TryMoveDroplet(this WaterDroplet droplet, float[,] map, int mapSize, float inertia, float gravity, float evaporateSpeed)
    {
        var isCalculated = TryCalculateHeightAndGradient(droplet, map, mapSize, out HeightAndGradient heightAndGradient);
        if (!isCalculated)
        {
            return false;
        }

        droplet.dirX = (droplet.dirX * inertia - heightAndGradient.gradientX * (1 - inertia));
        droplet.dirY = (droplet.dirY * inertia - heightAndGradient.gradientY * (1 - inertia));

        float len = Mathf.Sqrt(droplet.dirX * droplet.dirX + droplet.dirY * droplet.dirY);
        if (len != 0)
        {
            droplet.dirX /= len;
            droplet.dirY /= len;
        }
        droplet.posX += droplet.dirX;
        droplet.posY += droplet.dirY;


        if (droplet.posX < 0 || droplet.posX >= mapSize - 1 || droplet.posY < 0 || droplet.posY >= mapSize - 1)
        {
            return false;
        }
        if (droplet.dirX == 0 && droplet.dirY == 0)
        {
            return false;
        }

        isCalculated = TryCalculateHeightAndGradient(droplet, map, mapSize, out HeightAndGradient newHeightAndGradient);
        if (!isCalculated)
        {
            return false;
        }
        float newHeight = newHeightAndGradient.height;
        float deltaHeight = newHeight - heightAndGradient.height;

        LastDeltaHeight = deltaHeight;

        droplet.speed = Mathf.Sqrt(droplet.speed * droplet.speed + Math.Abs(deltaHeight) * gravity);
        droplet.volume *= (1 - evaporateSpeed);
        return true;
    }

    public static bool TryCalculateHeightAndGradient(this WaterDroplet droplet, float[,] nodes, int mapSize, out HeightAndGradient heightAndGradient)
    {
        int coordX = (int)droplet.posX;
        int coordY = (int)droplet.posY;

        if (coordX >= mapSize - 1 || coordY >= mapSize - 1 || coordX < 0 || coordY < 0)
        {
            heightAndGradient = new HeightAndGradient();
            return false;
        }

        float x = droplet.posX - coordX;
        float y = droplet.posY - coordY;

        float heightNW = nodes[coordY, coordX];
        float heightNE = nodes[coordY, coordX + 1];
        float heightSW = nodes[coordY + 1, coordX];
        float heightSE = nodes[coordY + 1, coordX + 1];

        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        heightAndGradient = new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
        return true;
    }

    public struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}
