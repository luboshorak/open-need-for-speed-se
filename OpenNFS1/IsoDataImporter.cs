using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DiscUtils;
using DiscUtils.Iso9660;

namespace OpenNFS1
{
    /// <summary>
    /// Imports the DATA track of an original NFS SE ISO image.
    /// Uses a staging directory so corrupt/unsupported images never leave a half-imported CD_Data.
    /// Does not require mounting the ISO or elevated privileges.
    /// </summary>
    internal static class IsoDataImporter
    {
        private const string ControlFile = "FRONTEND/ART/CONTROL/CENTRAL.QFS";
        private const string TrackFile = "SIMDATA/MISC/AL1.TRI";

        private sealed class ImageEntry
        {
            public DiscFileInfo Source;
            public string RelativePath;
        }

        public static void Import(string imagePath, string destinationPath, Action<int, string> progress)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException("The selected ISO image does not exist.", imagePath);
            if (!string.Equals(Path.GetExtension(imagePath), ".iso", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Select a standard .iso (ISO 9660/Joliet) CD image.");
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("CD data destination is empty.", nameof(destinationPath));

            string destination = Path.GetFullPath(destinationPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string parent = Path.GetDirectoryName(destination);
            if (string.IsNullOrEmpty(parent))
                throw new InvalidDataException("Invalid CD data destination.");
            Directory.CreateDirectory(parent);

            // A checkout may have a CD_Data directory containing only our placeholder README.
            // Never replace a directory containing user files, or follow a reparse-point destination.
            if (Directory.Exists(destination))
            {
                if ((File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("The CD_Data destination is a symbolic link; choose a normal directory.");
                if (Directory.EnumerateFileSystemEntries(destination, "*", SearchOption.AllDirectories)
                    .Any(path => !string.Equals(Path.GetRelativePath(destination, path), "README.txt",
                        StringComparison.OrdinalIgnoreCase)))
                    throw new IOException("CD_Data already contains files. Move or back them up before importing a new ISO.");
            }

            string staging = Path.Combine(parent, Path.GetFileName(destination) + ".import-" + Guid.NewGuid().ToString("N"));
            try
            {
                progress?.Invoke(0, "Reading the selected ISO image...");
                using (FileStream isoFile = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (CDReader cd = new CDReader(isoFile, true))
                {
                    // DiscUtils.GetFiles(root, wildcard, recursive) can return an empty
                    // sequence for ISO directories on Windows. Walk the directory tree
                    // explicitly instead, keeping DiscFileInfo for reliable OpenRead().
                    var entries = new List<ImageEntry>();
                    CollectFiles(cd.Root, entries);
                    if (entries.Count == 0)
                        throw new InvalidDataException("The ISO image contains no readable files.");

                    // CDs are sometimes mastered with an additional top-level directory (e.g. NFSSE/).
                    // Find the shortest prefix that contains BOTH required files.
                    string rootPrefix = null;
                    foreach (ImageEntry e in entries)
                    {
                        string path = e.RelativePath;
                        if (!path.EndsWith(ControlFile, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string candidate = path.Substring(0, path.Length - ControlFile.Length);
                        if (entries.Any(other => string.Equals(other.RelativePath, candidate + TrackFile,
                            StringComparison.OrdinalIgnoreCase)))
                        {
                            rootPrefix = candidate;
                            break;
                        }
                    }

                    if (rootPrefix == null)
                        throw new InvalidDataException("Not a supported Need for Speed SE CD: CENTRAL.QFS and AL1.TRI were not found.");

                    List<ImageEntry> selected = entries
                        .Where(e => e.RelativePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (selected.Count == 0)
                        throw new InvalidDataException("No supported game files found in this ISO.");

                    Directory.CreateDirectory(staging);
                    int completed = 0;
                    foreach (ImageEntry entry in selected)
                    {
                        string relativePath = entry.RelativePath.Substring(rootPrefix.Length);
                        string safePath = GetSafeDestination(staging, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(safePath));

                        using (Stream input = entry.Source.OpenRead())
                        using (FileStream output = File.Create(safePath))
                            input.CopyTo(output);

                        completed++;
                        int percent = (int)((long)completed * 100 / selected.Count);
                        progress?.Invoke(percent, "Extracting " + Path.GetFileName(relativePath));
                    }
                }

                if (!File.Exists(Path.Combine(staging, "FRONTEND", "ART", "CONTROL", "CENTRAL.QFS")) ||
                    !File.Exists(Path.Combine(staging, "SIMDATA", "MISC", "AL1.TRI")))
                    throw new InvalidDataException("The extracted image is missing required game files.");

                // Recheck just before committing the staging directory.
                if (Directory.Exists(destination))
                {
                    if ((File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0 ||
                        Directory.EnumerateFileSystemEntries(destination, "*", SearchOption.AllDirectories)
                            .Any(path => !string.Equals(Path.GetRelativePath(destination, path), "README.txt",
                                StringComparison.OrdinalIgnoreCase)))
                        throw new IOException("CD_Data was modified while importing; extracted files were not installed.");

                    Directory.Delete(destination, true); // removes only the project's README placeholder
                }
                Directory.Move(staging, destination);
                progress?.Invoke(100, "CD data imported successfully.");
            }
            finally
            {
                if (Directory.Exists(staging))
                {
                    try { Directory.Delete(staging, true); }
                    catch (IOException) { /* preserve original import error */ }
                    catch (UnauthorizedAccessException) { /* preserve original import error */ }
                }
            }
        }

        private static void CollectFiles(DiscDirectoryInfo folder, List<ImageEntry> entries)
        {
            foreach (DiscFileInfo file in folder.GetFiles())
            {
                string relativePath = NormalizeIsoPath(file.FullName);
                if (!string.IsNullOrEmpty(relativePath))
                    entries.Add(new ImageEntry { Source = file, RelativePath = relativePath });
            }
            foreach (DiscDirectoryInfo directory in folder.GetDirectories())
                CollectFiles(directory, entries);
        }

        private static string NormalizeIsoPath(string source)
        {
            // ISO 9660 may expose file-version suffixes such as CENTRAL.QFS;1.
            return string.Join("/", source.Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => Regex.Replace(part, @";[0-9]+$", string.Empty)));
        }

        private static string GetSafeDestination(string root, string relativePath)
        {
            string[] segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 ||
                segments.Any(segment => segment == "." || segment == ".." ||
                    segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    segment.EndsWith(".", StringComparison.Ordinal) ||
                    segment.EndsWith(" ", StringComparison.Ordinal)))
                throw new InvalidDataException("Invalid file name in the ISO: " + relativePath);

            string candidate = Path.GetFullPath(Path.Combine(new[] { root }.Concat(segments).ToArray()));
            string requiredPrefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsafe file path in ISO: " + relativePath);
            return candidate;
        }
    }
}
