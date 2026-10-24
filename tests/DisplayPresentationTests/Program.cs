using System;
using System.IO;
using System.Xml.Linq;
using GameEngine;
using OpenNFS1.UI;

static class Program
{
    private static void Check(bool b, string msg)
    {
        if (!b) throw new Exception(msg);
    }

    private static void Main()
    {
        var smooth = PresentationLayout.Calculate(1920, 1080, false);
        Check(smooth.Width == 1440 && smooth.Height == 1080 &&
            smooth.X == 240 && smooth.Y == 0, "Wrong 4:3 fullscreen pillarbox");

        var exact = PresentationLayout.Calculate(1920, 1080, true);
        Check(exact.Width == 1280 && exact.Height == 960 &&
            exact.X == 320 && exact.Y == 60, "Integer scale must use 2x pixels");

        var origin = PresentationLayout.ToVirtual(exact.X, exact.Y, exact);
        Check(origin.X == 0 && origin.Y == 0, "Image top-left maps to wrong virtual coordinates");

        var center = PresentationLayout.ToVirtual(960, 540, exact);
        Check(Math.Abs(center.X - 320) < 0.01f &&
            Math.Abs(center.Y - 240) < 0.01f,
            "Centered mouse coordinate no longer matches rendered UI");

        var bars = PresentationLayout.ToVirtual(100, 540, exact);
        Check(bars.X == -1 && bars.Y == -1, "Pillarboxes must not trigger UI clicks");

        var small = PresentationLayout.Calculate(800, 600, true);
        Check(small.Width == 640 && small.Height == 480 &&
            small.X == 80 && small.Y == 60,
            "Pixel-perfect option at sub-2x display should keep 1x art centered");

        var invalid = PresentationLayout.Calculate(0,0,true);
        Check(invalid.IsEmpty, "Zero-sized backbuffer must not create geometry");

        // MonoGame SpriteBatch.DrawString throws ArgumentException for any
        // character absent from a SpriteFont with no DefaultCharacter.
        // Keep every production UI font safe for locale-specific messages.
        foreach (string font in new[] { "ArialBlack", "ArialBlack-Italic" })
        {
            string fontPath = Path.Combine(AppContext.BaseDirectory, font + ".spritefont");
            var xml = XDocument.Load(fontPath);
            string fallback = xml.Root?.Element("Asset")?.Element("DefaultCharacter")?.Value;
            Check(fallback == "?", font + " has no valid fallback glyph.");

            // '?' must actually be inside the packed glyph ranges (32-126).
            // Read the region from the authored spritefont XML. MonoGame's
            // content builder already validates the range when it packs the
            // atlas; the test only needs to guard that ASCII '?' is included.
            string source = File.ReadAllText(fontPath);
            Check(source.Contains("<Start>&#32;</Start>") &&
                  source.Contains("<End>&#126;</End>"),
                font + " no longer guarantees ASCII '?' in its character region.");
        }

        // Never allow saved 2560x1440 window preferences to open partly
        // outside a 1920x1080 monitor, including taskbar/title-bar margins.
        var fitted = PresentationLayout.FitWindow(2560, 1440, 1920, 1080);
        Check(fitted.X <= 1920 - 64 && fitted.Y <= 1080 - 112,
            "Oversized saved window exceeds the available desktop.");
        Check(Math.Abs(fitted.X / (double)fitted.Y - 2560.0 / 1440.0) < 0.01,
            "Fitting the window must preserve its chosen aspect ratio.");

        var safeWindow = PresentationLayout.FitWindow(800, 600, 1920, 1080);
        Check(safeWindow.X == 800 && safeWindow.Y == 600,
            "Safe 800x600 emergency reset must remain unchanged.");

        var smallMonitor = PresentationLayout.FitWindow(1280, 960, 1366, 768);
        Check(smallMonitor.X <= 1366 - 64 && smallMonitor.Y <= 768 - 112,
            "Smaller laptop monitor must also be protected.");

        // Original 116x142 mpbd minimap coordinates must stay within the
        // right-hand source bitmap for both ordinary and degenerate circuits.
        var mapCenter = MapProjection.ToMap(50, 50, 0, 100, 0, 100);
        Check(mapCenter.X >= 510 && mapCenter.X <= 626 &&
            mapCenter.Y >= 303 && mapCenter.Y <= 445,
            "Track minimap center is outside original HUD mpbd.");

        var mapCorner = MapProjection.ToMap(100, 100, 0, 100, 0, 100);
        Check(mapCorner.X >= 510 && mapCorner.X <= 626 &&
            mapCorner.Y >= 303 && mapCorner.Y <= 445,
            "Track minimap corner is outside original HUD mpbd.");

        var degenerate = MapProjection.ToMap(0, 0, 0, 0, 0, 0);
        Check(!float.IsNaN(degenerate.X) && !float.IsNaN(degenerate.Y),
            "Degenerate road projection must not produce NaNs.");

        // The original `stat` icon begins at x=349. Speed digits
        // must never overwrite it, even at the 999 km/h edge case.
        foreach (int digits in new[] { 1, 2, 3 })
        {
            for (int i = 0; i < digits; i++)
            {
                var bounds = HudGeometry.SpeedDigit(digits, i);
                Check(bounds.X >= 266 && bounds.Right <= 349 &&
                    bounds.Y >= 0 && bounds.Bottom <= 32,
                    "Speed numeral overlaps native gear icon or leaves HUD.");
            }
        }
        foreach (int clock in new[] { 0, 1 })
        {
            for (int i = 0; i < 5; i++)
            {
                var digit = HudGeometry.ClockDigit(clock, i);
                Check(digit.X >= (clock == 0 ? 29 : 144) &&
                    digit.Right < (clock == 0 ? 114 : 254) &&
                    digit.Bottom <= 32,
                    "Original clock numeral overlaps one of the stat icons.");
            }
        }
        Check(HudGeometry.Gear.X >= 377 &&
            HudGeometry.Gear.Right <= 405, "Gear numeral overlaps finish flags.");

        Check(HudGeometry.ValueDigit(11, 0, 591, 6, 11, 16).X >= 570,
            "Two-digit player position runs into position icon.");
        Check(HudGeometry.ValueDigit(11, 1, 637, 6, 11, 16).Right <= 640,
            "Opponent count runs off HUD.");

        Console.WriteLine("PASS: display geometry, safe fonts, window recovery and original HUD layout and map projection");
    }
}