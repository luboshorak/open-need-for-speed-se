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
    /// scoring rules. It includes the six original class A/B/C venues.
    /// </summary>
    internal sealed class TournamentSeries
    {
        private readonly List<TrackDescription> _courses;
        private bool _currentResultRecorded;
        private readonly HashSet<string> _completedSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private TimeSpan _roadSegmentTimes;

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

            // The manual specifies City/Rusty Springs (C), Coastal/Autumn Valley
            // (B), Alpine/Vertigo Ridge (A). Bonus and later SE tracks are not
            // included in this six-course original championship.
            string[] names = { "City", "Rusty Springs", "Coastal",
                "Autumn Valley", "Alpine", "Vertigo Ridge" };
            var candidates = allTracks
                .Where(t => !t.HideFromMenu && names.Contains(t.Name))
                .OrderBy(t => Array.IndexOf(names, t.Name))
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("No playable tournament circuits were found.");

            int start = candidates.FindIndex(t => t == startTrack);
            if (start < 0) start = 0;
            _courses = candidates.Skip(start).Concat(candidates.Take(start)).ToList();
        }

        /// <summary>
        /// Open-road venues have three separately loaded segments. Accumulate the
        /// first two, then only score the venue after the third segment finishes.
        /// </summary>
        public void RecordRoadSegment(TrackDescription track, TimeSpan segmentTime)
        {
            if (_currentResultRecorded || IsComplete || track == null ||
                !_completedSegments.Add(track.FileName)) return;
            if (segmentTime > TimeSpan.Zero) _roadSegmentTimes += segmentTime;
        }

        /// <summary>Next venue after the just-finished race (null at championship end).</summary>
        public TrackDescription NextVenueAfterResult
        {
            get
            {
                if (_currentResultRecorded)
                    return StageIndex + 1 < _courses.Count ? _courses[StageIndex + 1] : null;
                return IsComplete ? null : _courses[StageIndex];
            }
        }

        /// <summary>
        /// Capture a restartable checkpoint, never a transient results screen.
        /// A recorded result projects to the NEXT venue without changing
        /// the live championship until the player chooses Continue.
        /// </summary>
        public TournamentSaveData CreateCheckpoint(TrackDescription upcomingTrack)
        {
            int index = StageIndex + (_currentResultRecorded ? 1 : 0);
            if (index < 0 || index >= _courses.Count || upcomingTrack == null)
                throw new InvalidOperationException("The championship has no further playable races.");

            TrackDescription venue = _courses[index];
            if (!IsTrackInVenue(venue, upcomingTrack))
                throw new InvalidOperationException("The upcoming segment does not belong to the current venue.");

            return new TournamentSaveData
            {
                FirstVenueFile = _courses[0].FileName,
                UpcomingTrackFile = upcomingTrack.FileName,
                StageIndex = index,
                CompletedStages = CompletedStages,
                Points = Points,
                TotalTimeSeconds = TotalTime.TotalSeconds,
                RoadSegmentSeconds = _currentResultRecorded ? 0 : _roadSegmentTimes.TotalSeconds,
                CompletedSegments = _currentResultRecorded
                    ? Array.Empty<string>() : _completedSegments.OrderBy(x => x).ToArray(),
                SavedAtUtc = DateTime.UtcNow
            };
        }

        private static bool IsTrackInVenue(TrackDescription venue, TrackDescription track)
        {
            if (venue == track) return true;
            if (!venue.IsOpenRoad) return false;
            TrackDescription second = TrackDescription.GetNextOpenRoadStage(venue);
            if (second == track) return true;
            return second != null && TrackDescription.GetNextOpenRoadStage(second) == track;
        }

        public static TournamentSeries Restore(TournamentSaveData data,
            IEnumerable<TrackDescription> available)
        {
            if (data == null || data.Version != 1 || available == null)
                throw new InvalidOperationException("Unsupported championship save format.");

            var all = available.ToList();
            TrackDescription first = all.Find(t => string.Equals(
                t.FileName, data.FirstVenueFile, StringComparison.OrdinalIgnoreCase));
            if (first == null) throw new InvalidOperationException("Saved starting venue is unavailable.");

            var series = new TournamentSeries(first, all);
            if (data.StageIndex < 0 || data.StageIndex >= series.StageCount ||
                data.CompletedStages != data.StageIndex ||
                data.Points < 0 || data.Points > 10 * data.CompletedStages ||
                double.IsNaN(data.TotalTimeSeconds) ||
                data.TotalTimeSeconds < 0 || data.TotalTimeSeconds > 1_000_000 ||
                double.IsNaN(data.RoadSegmentSeconds) ||
                data.RoadSegmentSeconds < 0 || data.RoadSegmentSeconds > 100_000)
                throw new InvalidOperationException("Invalid championship progression in save.");

            TrackDescription upcoming = all.Find(t => string.Equals(
                t.FileName, data.UpcomingTrackFile, StringComparison.OrdinalIgnoreCase));
            if (!IsTrackInVenue(series._courses[data.StageIndex], upcoming))
                throw new InvalidOperationException("Saved race is not in the active championship venue.");

            series.StageIndex = data.StageIndex;
            series.CompletedStages = data.CompletedStages;
            series.Points = data.Points;
            series.TotalTime = TimeSpan.FromSeconds(data.TotalTimeSeconds);
            series._roadSegmentTimes = TimeSpan.FromSeconds(data.RoadSegmentSeconds);

            // Only prior segments from THIS venue can appear in the checkpoint.
            string[] expected = series._courses[data.StageIndex].IsOpenRoad
                ? new[]
                {
                    series._courses[data.StageIndex].FileName,
                    TrackDescription.GetNextOpenRoadStage(series._courses[data.StageIndex])?.FileName
                } : Array.Empty<string>();
            string[] completed = data.CompletedSegments ?? Array.Empty<string>();
            if (completed.Length > 2 || completed.Any(v =>
                !expected.Any(e => string.Equals(e, v, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidOperationException("Invalid completed road segments.");
            foreach (string segment in completed) series._completedSegments.Add(segment);
            return series;
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
            TotalTime += _roadSegmentTimes + raceTime;
            _roadSegmentTimes = TimeSpan.Zero;
            _completedSegments.Clear();
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
