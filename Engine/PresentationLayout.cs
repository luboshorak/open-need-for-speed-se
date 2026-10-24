using System;
using Microsoft.Xna.Framework;

namespace GameEngine
{
    /// <summary>
    /// Maps the original 640x480 screen into a modern backbuffer without
    /// stretching 4:3 artwork. This same rectangle is used for presentation
    /// and native-coordinate mouse hit testing.
    /// </summary>
    public static class PresentationLayout
    {
        public const int VirtualWidth = 640;
        public const int VirtualHeight = 480;

        public static Rectangle Calculate(int backWidth, int backHeight, bool integerScale)
        {
            if (backWidth <= 0 || backHeight <= 0)
                return Rectangle.Empty;

            double scale = Math.Min(backWidth / (double)VirtualWidth,
                backHeight / (double)VirtualHeight);
            if (integerScale && scale >= 1)
                scale = Math.Max(1, Math.Floor(scale));
            int w = Math.Max(1, (int)Math.Round(VirtualWidth * scale));
            int h = Math.Max(1, (int)Math.Round(VirtualHeight * scale));
            return new Rectangle((backWidth - w) / 2, (backHeight - h) / 2, w, h);
        }

        public static Vector2 ToVirtual(int mouseX, int mouseY, Rectangle destination)
        {
            if (destination.Width <= 0 || destination.Height <= 0 ||
                !destination.Contains(mouseX, mouseY))
                return new Vector2(-1, -1);
            return new Vector2(
                (mouseX - destination.X) * VirtualWidth / (float)destination.Width,
                (mouseY - destination.Y) * VirtualHeight / (float)destination.Height);
        }
    }
}
