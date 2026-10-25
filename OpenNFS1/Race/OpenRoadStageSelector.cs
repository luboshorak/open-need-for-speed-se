using System;
using System.IO;

namespace OpenNFS1
{
    /// <summary>
    /// Original NFS SE multi-segment roads (AL1-3, CL1-3, CY1-3).
    /// Each segment is an independent TRI file, not merely a different photo.
    /// </summary>
    internal static class OpenRoadStageSelector
    {
        public static string Family(string trackFile, bool isOpenRoad)
        {
            if (!isOpenRoad || string.IsNullOrWhiteSpace(trackFile))
                return null;

            string file = Path.GetFileNameWithoutExtension(
                trackFile.Replace('\\', '/')).ToUpperInvariant();
            if (file.Length != 3 || file[2] < '1' || file[2] > '3')
                return null;

            string family = file.Substring(0, 2);
            return family == "AL" || family == "CL" || family == "CY"
                ? family : null;
        }

        /// <summary>
        /// Resolve a selected hidden segment to the visible entry for its
        /// course, without accidentally falling back to Alpine.
        /// </summary>
        public static bool IsVisibleCourseForSelection(
            string visibleFile, bool visibleOpenRoad,
            string selectedFile, bool selectedOpenRoad)
        {
            if (string.IsNullOrWhiteSpace(visibleFile) ||
                string.IsNullOrWhiteSpace(selectedFile))
                return false;
            string visible = visibleFile.Replace('\\', '/');
            string selected = selectedFile.Replace('\\', '/');
            return string.Equals(visible, selected, StringComparison.OrdinalIgnoreCase)
                || SameFamily(visibleFile, visibleOpenRoad, selectedFile, selectedOpenRoad);
        }

        public static bool SameFamily(string leftFile, bool leftOpenRoad,
            string rightFile, bool rightOpenRoad)
        {
            string a = Family(leftFile, leftOpenRoad);
            string b = Family(rightFile, rightOpenRoad);
            return a != null && a == b;
        }
    }
}
