using System;
using System.Numerics;

namespace OpenNFS1.Physics
{
    /// <summary>
    /// 2D oriented rectangles on the road surface. This math is independent
    /// of MonoGame, so it can be exercised in deterministic unit tests.
    /// Coordinates are world X,Z, while Forward and Right are unit vectors.
    /// </summary>
    internal readonly struct CollisionFootprint
    {
        public readonly Vector2 Center;
        public readonly Vector2 Forward;
        public readonly Vector2 Right;
        public readonly float HalfWidth;
        public readonly float HalfLength;

        public CollisionFootprint(Vector2 center, Vector2 direction, float width, float length)
        {
            Center = center;
            Forward = direction.LengthSquared() < 0.000001f
                ? new Vector2(0, -1) : Vector2.Normalize(direction);
            Right = new Vector2(-Forward.Y, Forward.X);
            HalfWidth = Math.Max(0.01f, width / 2);
            HalfLength = Math.Max(0.01f, length / 2);
        }

        private float ProjectedRadius(Vector2 axis)
        {
            return HalfWidth * Math.Abs(Vector2.Dot(Right, axis)) +
                   HalfLength * Math.Abs(Vector2.Dot(Forward, axis));
        }

        /// <summary>
        /// Returns minimum displacement to move A away from B, and a unit
        /// contact normal pointing from A towards B. Never normalizes zero.
        /// </summary>
        public bool Overlaps(CollisionFootprint other, out Vector2 normal, out float penetration)
        {
            normal = Vector2.Zero;
            penetration = 0;
            float minOverlap = float.MaxValue;
            Vector2 delta = other.Center - Center;
            Vector2[] axes = { Forward, Right, other.Forward, other.Right };

            foreach (Vector2 axis in axes)
            {
                float combined = ProjectedRadius(axis) + other.ProjectedRadius(axis);
                float distance = Math.Abs(Vector2.Dot(delta, axis));
                float overlap = combined - distance;
                if (overlap <= 0)
                    return false;
                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    normal = Vector2.Dot(delta, axis) >= 0 ? axis : -axis;
                }
            }

            penetration = minOverlap;
            return true;
        }
    }
}
