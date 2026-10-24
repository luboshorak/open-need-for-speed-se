using System;
using Microsoft.Xna.Framework;

namespace OpenNFS1.UI
{
    /// <summary>
    /// Fixed native 640x32 HUD fields, measured against the original
    /// SIMDATA/MISC/MASKHI.FSH 'stat', 'lap#' and 'mile' sprites.
    /// Do not let dynamic glyphs overlap the H gearbox icon (x=349),
    /// checkered flags (x=406) or built-in slashes.
    /// </summary>
    internal static class HudGeometry
    {
        private static readonly int[] FirstTime = { 30, 42, 64, 76, 99 };
        private static readonly int[] SecondTime = { 149, 161, 192, 204, 235 };

        public static Rectangle ClockDigit(int clock, int digitIndex)
        {
            if (clock < 0 || clock > 1 || digitIndex < 0 || digitIndex > 4)
                throw new ArgumentOutOfRangeException();
            var columns = clock == 0 ? FirstTime : SecondTime;
            return new Rectangle(columns[digitIndex], 8, 11, 13);
        }

        public static Rectangle SpeedDigit(int count, int digitIndex)
        {
            if (count < 1 || count > 3 || digitIndex < 0 || digitIndex >= count)
                throw new ArgumentOutOfRangeException();
            return new Rectangle(266 + (3 - count + digitIndex) * 26, 2, 28, 27);
        }

        public static Rectangle Gear => new Rectangle(377, 7, 15, 16);

        public static Rectangle ValueDigit(int value, int digitIndex,
            int rightEdge, int y, int width = 12, int height = 15)
        {
            string digits = Math.Clamp(value, 0, 99).ToString();
            if (digitIndex < 0 || digitIndex >= digits.Length)
                throw new ArgumentOutOfRangeException(nameof(digitIndex));
            return new Rectangle(rightEdge - digits.Length * width +
                digitIndex * width, y, width, height);
        }
    }
}
