using System;
using System.IO;
using System.Linq;
using GameEngine;
using OpenNFS1.UI.Screens;
using OpenNFS1.Vehicles;

namespace OpenNFS1
{
    /// <summary>
    /// Start a race directly from Control Central, as in the original UI.
    /// Previously a custom pre-race text page obscured the actual game flow.
    /// </summary>
    internal static class RaceLauncher
    {
        public static void Start()
        {
            if (GameConfig.SelectedTrackDescription == null || GameConfig.SelectedVehicle == null)
                throw new InvalidOperationException("A track and a car must be selected.");

            if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
            {
                if (!RaceModeRules.IsVehicleAllowed(RaceMode.Tournament,
                    GameConfig.SelectedTrackDescription, GameConfig.SelectedVehicle))
                {
                    GameConfig.SelectedVehicle = VehicleDescription.Descriptions.First(c =>
                        RaceModeRules.IsVehicleAllowed(RaceMode.Tournament,
                            GameConfig.SelectedTrackDescription, c));
                }

                GameConfig.Tournament = new TournamentSeries(
                    GameConfig.SelectedTrackDescription, TrackDescription.Descriptions);
                GameConfig.SelectedTrackDescription = GameConfig.Tournament.CurrentCourse;
                try
                {
                    TournamentSaveManager.Save(0, GameConfig.SelectedTrackDescription);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            else
            {
                GameConfig.Tournament = null;
            }

            Engine.Instance.Screen = new LoadRaceScreen();
        }
    }
}
