using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNFS1
{
    internal enum RaceMode
    {
        SingleRace = 0,
        Tournament = 1,
        TimeTrial = 2,
        HeadToHead = 3
    }

    /// <summary>
    /// Small, deterministic local tournament progression. This is a new
    /// implementation, not a claim to reproduce the exact 1996 championship
    /// scoring rules. It uses only circuits already supported by OpenNFS1.
    /// </summary>
    internal sealed class TournamentSeries
    {
        private readonly List<TrackDescription> _courses;
        private bool _currentResultRecorded;

        public int StageIndex { get; private set; }
        public int CompletedStages { get; private set; }
        public int Points { get; private set; }
        public TimeSpan TotalTime { get; private set; }

        public int StageCount { get { return _courses.Count; } }
        public bool IsComplete { get { return CompletedStages == _courses.Count; } }
        public TrackDescription CurrentCourse { get { return _courses[StageIndex]; } }

        public TournamentSeries(TrackDescription startTrack, IEnumerable<TrackDescription> allTracks)
        {
            if (allTracks == null)
                throw new ArgumentNullException(nameof(allTracks));

            var candidates = allTracks
                .Where(t => !t.IsOpenRoad && !t.HideFromMenu &&
                    t.Name.IndexOf("hidden track", StringComparison.OrdinalIgnoreCase) < 0)
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("No playable tournament circuits were found.");

            int start = candidates.FindIndex(t => t == startTrack);
            if (start < 0) start = 0;
            _courses = candidates.Skip(start).Concat(candidates.Take(start)).ToList();
        }

        /// <summary>Record the last completed race exactly once (0-based rank).</summary>
        public void RecordResult(int rank, TimeSpan raceTime)
        {
            if (_currentResultRecorded || IsComplete)
                return;
            if (rank < 0) rank = 0;
            if (raceTime < TimeSpan.Zero) raceTime = TimeSpan.Zero;

            // Simple prototype points: 10 for first, down to 1 for tenth+.
            Points += Math.Max(1, 10 - rank);
            TotalTime += raceTime;
            CompletedStages++;
            _currentResultRecorded = true;
        }

        /// <summary>Advance after the results page; returns false at championship end.</summary>
        public bool TryAdvance()
        {
            if (!_currentResultRecorded || IsComplete)
                return false;

            StageIndex++;
            _currentResultRecorded = false;
            return true;
        }
    }
}
