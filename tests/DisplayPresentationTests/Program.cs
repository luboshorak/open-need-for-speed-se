using System;
using System.IO;
using System.Xml.Linq;
using GameEngine;

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
            var regions = xml.Root?.Element("Asset")?.Element("CharacterRegions");
            bool supportsFallback = false;
            if (regions != null)
                foreach (var region in regions.Elements("CharacterRegion"))
                {
                    int start = char.Parse(region.Element("Start").Value);
                    int end = char.Parse(region.Element("End").Value);
                    if (start <= '?' && '?' <= end) supportsFallback = true;
                }
            Check(supportsFallback, font + " fallback is missing from its glyph atlas.");
        }

        Console.WriteLine("PASS: fullscreen scaling, mouse mapping, and safe UI font fallback");
    }
}