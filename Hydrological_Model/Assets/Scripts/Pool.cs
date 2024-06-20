using System.Collections.Generic;
using System.Linq;

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
