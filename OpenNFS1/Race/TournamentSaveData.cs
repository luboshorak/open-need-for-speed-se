using System;

namespace OpenNFS1
{
    // Portable save-data contract. Contains no CD assets or absolute paths.
    internal sealed class TournamentSaveData
    {
        public int Version { get; set; } = 1;
        public string FirstVenueFile { get; set; }
        public string UpcomingTrackFile { get; set; }
        public int StageIndex { get; set; }
        public int CompletedStages { get; set; }
        public int Points { get; set; }
        public double TotalTimeSeconds { get; set; }
        public double RoadSegmentSeconds { get; set; }
        public string[] CompletedSegments { get; set; } = Array.Empty<string>();
        public string PlayerCarName { get; set; }
        public string OpponentCarName { get; set; }
        public bool UseOpponentPack { get; set; }
        public bool ManualGearbox { get; set; }
        public bool AlternativeTimeOfDay { get; set; }
        public int CircuitLaps { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }
}
