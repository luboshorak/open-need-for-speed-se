using System;
using Microsoft.Xna.Framework;

namespace OpenNFS1.UI
{
    /// <summary>
    /// Pure normalized minimap projection into the original MASKHI.FSH
    /// mpbd bitmap's usable central region (510,303,116x142).
    /// </summary>
    internal static class MapProjection
    {
        public static Vector2 ToMap(float x, float z,
            float minX, float maxX, float minZ, float maxZ)
        {
            const float centerX = 568;
            const float centerY = 374;
            const float halfWidth = 47;
            const float halfHeight = 57;

            float extentX = Math.Max(0.0001f, maxX - minX);
            float extentZ = Math.Max(0.0001f, maxZ - minZ);
            float scale = Math.Min(2 * halfWidth / extentX, 2 * halfHeight / extentZ);

            float translatedX = (x - (minX + maxX) * 0.5f) * scale;
            float translatedZ = (z - (minZ + maxZ) * 0.5f) * scale;
            return new Vector2(centerX + translatedX, centerY + translatedZ);
        }
    }
}
