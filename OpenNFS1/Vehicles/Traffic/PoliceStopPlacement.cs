using System;
using System.Numerics;
namespace OpenNFS1.Vehicles
{
    internal static class PoliceStopPlacement
    {
        public static Vector3 ToRight(Vector3 position, Vector3 left, Vector3 right, float width)
        {
            Vector3 across = right-left;
            float length = across.Length();
            if (length < .001f) return position;
            across /= length;
            float desired = Math.Max(0,length-Math.Max(0,width)*.5f);
            return position+across*(desired-Vector3.Dot(position-left,across));
        }
    }
}
