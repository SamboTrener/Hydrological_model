using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Pool
{
    public List<Point> Points;
    public Point LowestPointOfLeak;
    public float Volume;
    public int Id;

    public Pool(List<Point> points, float volume, int id)
    {
        Id = id;
        Points = points;
        LowestPointOfLeak = points.First();
        Volume = volume;
    }
}
