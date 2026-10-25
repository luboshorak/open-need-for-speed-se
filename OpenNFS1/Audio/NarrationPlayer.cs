using System;
using System.IO;
using GameEngine;
using OpenNFS1.Parsers.Audio;
using OpenNFS1.UI.Screens;

namespace OpenNFS1.Audio
{
    // One frontend narrator; screen identity plus page identity prevent overlapping speech
    // and prevent replaying a completed description every frame.
    internal sealed class NarrationPlayer
    {
        public static NarrationPlayer Instance { get; } = new();
        private object _screen;
        private string _file;
        private OriginalSoundVoice _voice;
        private bool _welcome;
        private float _delay;
        public void Update(object screen, bool active)
        {
            string file = screen switch
            {
                VehicleShowroomScreen car => car.Narration,
                TrackDetailsScreen track => track.Narration,
                RaceStandingsScreen result => result.Narration,
                HomeScreen when !_welcome || ReferenceEquals(screen, _screen) && _file == "NFS_SE.EAS" => "NFS_SE.EAS",
                _ => null
            };
            if (!ReferenceEquals(screen, _screen) || file != _file)
            {
                Stop(); _screen = screen; _file = file; _delay = .3f;
            }
            if (!active || !GameConfig.SpeechEnabled) { Stop(); return; }
            if (_file == null || _delay < 0) return;
            _delay -= Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            if (_delay > 0) return;
            _delay = -1;
            try
            {
                _voice = new OriginalSoundVoice(EacsFile.Read(Path.Combine(GameConfig.CdDataPath,
                    "FRONTEND", "SPEECH", _file)), _file, false);
                _voice.Trigger(.85f);
                if (_file == "NFS_SE.EAS") _welcome = true;
            }
            catch (Exception ex) { Stop(); AudioDiagnostics.Record(ex); }
        }
        private void Stop() { _voice?.Dispose(); _voice = null; }
    }
}
