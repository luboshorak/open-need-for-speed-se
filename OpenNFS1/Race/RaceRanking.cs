using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNFS1
{
    internal static class RaceRanking
    {
        // A gate may precede node zero by a few nodes. Do not award an extra
        // full lap between crossing that gate and wrapping to node zero.
        internal static double Progress(int laps, float node, int nodes, int finish, bool openRoad) =>
            openRoad ? node : laps * (double)nodes + node - (laps > 0 && node >= finish ? nodes : 0);

        internal static List<T> Order<T>(IEnumerable<T> racers, Func<T, TimeSpan?> finish,
            Func<T, double> progress) => racers.OrderBy(r => finish(r).HasValue ? 0 : 1)
                .ThenBy(r => finish(r) ?? TimeSpan.MaxValue)
                .ThenByDescending(progress).ToList();
    }
}
