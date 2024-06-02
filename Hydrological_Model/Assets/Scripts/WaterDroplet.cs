using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterDroplet 
{
    public float posX;
    public float posY;
    public float dirX;
    public float dirY;
    public float speed;
    public float volume;

    public WaterDroplet(float posX, float posY, float dirX, float dirY, float speed, float volume) 
    {
        this.posX = posX;
        this.posY = posY;
        this.dirX = dirX;
        this.dirY = dirY;
        this.speed = speed;
        this.volume = volume;
    }
}
