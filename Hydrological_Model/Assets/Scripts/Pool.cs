using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pool
{
    public List<Point> Points;
    public float Volume;
    public Point LowestPointOfLeak;

    public Pool(List<Point> points, float volume)
    {
        Points = points;
        Volume = volume;
        LowestPointOfLeak = points.First();
    }
}
