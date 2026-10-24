using System;
using System.IO;
using System.Linq;
using System.Text;
using DiscUtils.Iso9660;
using OpenNFS1;

internal static class Program
{
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "OpenNFS1-import-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string isoPath = Path.Combine(root, "original.iso");
            var builder = new CDBuilder { UseJoliet = true, VolumeIdentifier = "NFSSE_TEST" };
            builder.AddFile(@"NFSSE\FRONTEND\ART\CONTROL\CENTRAL.QFS", Encoding.ASCII.GetBytes("control"));
            builder.AddFile(@"NFSSE\SIMDATA\MISC\AL1.TRI", Encoding.ASCII.GetBytes("track"));
            builder.AddFile(@"NFSSE\SIMDATA\CARFAMS\MRX7.CFM", Encoding.ASCII.GetBytes("car"));
            builder.Build(isoPath);

            string installed = Path.Combine(root, "CD_Data");
            Directory.CreateDirectory(installed);
            File.WriteAllText(Path.Combine(installed, "README.txt"), "placeholder");
            int lastProgress = -1;
            IsoDataImporter.Import(isoPath, installed, (percent, description) => lastProgress = percent);

            Check(lastProgress == 100, "Did not reach 100% progress.");
            Check(File.ReadAllText(Path.Combine(installed, "FRONTEND", "ART", "CONTROL", "CENTRAL.QFS")) == "control",
                "Did not import CENTRAL.QFS correctly.");
            Check(File.ReadAllText(Path.Combine(installed, "SIMDATA", "MISC", "AL1.TRI")) == "track",
                "Did not import AL1.TRI correctly.");
            Check(File.ReadAllText(Path.Combine(installed, "SIMDATA", "CARFAMS", "MRX7.CFM")) == "car",
                "Did not import additional game data.");
            Check(!File.Exists(Path.Combine(installed, "README.txt")), "Old placeholder survived extraction.");

            // Do not ever overwrite an existing installation.
            bool refusedOverwrite = false;
            try { IsoDataImporter.Import(isoPath, installed, null); }
            catch (IOException) { refusedOverwrite = true; }
            Check(refusedOverwrite, "Importer overwrote existing CD data.");

            // An unsupported ISO must not overwrite even an empty target.
            string invalidIso = Path.Combine(root, "invalid.iso");
            var invalidBuilder = new CDBuilder { UseJoliet = true };
            invalidBuilder.AddFile("README.TXT", Encoding.ASCII.GetBytes("different game"));
            invalidBuilder.Build(invalidIso);
            string invalidDestination = Path.Combine(root, "Other_CD_Data");
            Directory.CreateDirectory(invalidDestination);
            File.WriteAllText(Path.Combine(invalidDestination, "README.txt"), "untouched");
            bool rejected = false;
            try { IsoDataImporter.Import(invalidIso, invalidDestination, null); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Importer accepted an unrelated ISO.");
            Check(File.ReadAllText(Path.Combine(invalidDestination, "README.txt")) == "untouched",
                "Importer damaged existing data after an invalid ISO.");

            Check(Directory.GetDirectories(root, "*.import-*", SearchOption.TopDirectoryOnly).Length == 0,
                "Importer left a staging folder behind.");

            Console.WriteLine("PASS: ISO importer extracts NFSSE data, finds nested CD root, preserves existing files, and rejects invalid CDs.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
