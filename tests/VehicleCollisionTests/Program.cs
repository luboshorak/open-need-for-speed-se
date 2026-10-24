using System;
using System.Numerics;
using OpenNFS1.Physics;

static class Program
{
    static void Check(bool value, string msg)
    {
        if (!value) throw new Exception(msg);
    }

    static void Main()
    {
        var a = new CollisionFootprint(new Vector2(0, 0), new Vector2(0, 1), 3, 8);
        var distant = new CollisionFootprint(new Vector2(0, 20), new Vector2(0, 1), 3, 8);
        Check(!a.Overlaps(distant, out _, out _), "Distant cars must not collide.");

        var follower = new CollisionFootprint(new Vector2(0, 6), new Vector2(0, 1), 3, 8);
        Check(a.Overlaps(follower, out var front, out float depth), "Cars overlapping nose-to-tail must collide.");
        Check(depth > 0 && Math.Abs(front.Y - 1f) < 0.001f, "Rear-end normal/depth incorrect.");

        var crossing = new CollisionFootprint(new Vector2(0.5f, 0), new Vector2(1, 0), 3, 8);
        Check(a.Overlaps(crossing, out _, out _), "Perpendicular collision missed.");

        var parallel = new CollisionFootprint(new Vector2(8, 0), new Vector2(0, 1), 3, 8);
        Check(!a.Overlaps(parallel, out _, out _), "Parallel lanes should not collide.");

        var offset = front * (depth + 0.02f);
        var separated = new CollisionFootprint(a.Center - offset, a.Forward, 3, 8);
        Check(!separated.Overlaps(follower, out _, out _),
            "Minimum separation vector did not separate contact.");

        Console.WriteLine("PASS: collision detection, orientation, separation and free lanes");
    }
}