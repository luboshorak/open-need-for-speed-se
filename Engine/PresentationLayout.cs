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

        /// <summary>
        /// Keep the entire client area and standard window borders accessible.
        /// Fit oversized requested sizes proportionally; do not stretch 4:3
        /// presentation or let persisted settings hide the graphics controls.
        /// </summary>
        public static Point FitWindow(int requestedWidth, int requestedHeight,
            int displayWidth, int displayHeight)
        {
            if (requestedWidth <= 0 || requestedHeight <= 0 ||
                displayWidth <= 0 || displayHeight <= 0)
                return new Point(VirtualWidth, VirtualHeight);

            // Leave room for the OS title bar, taskbar and window borders.
            int widthLimit = Math.Max(1, displayWidth - 64);
            int heightLimit = Math.Max(1, displayHeight - 112);
            double scale = Math.Min(1.0,
                Math.Min(widthLimit / (double)requestedWidth,
                         heightLimit / (double)requestedHeight));

            return new Point(
                Math.Max(1, (int)Math.Floor(requestedWidth * scale)),
                Math.Max(1, (int)Math.Floor(requestedHeight * scale)));
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
