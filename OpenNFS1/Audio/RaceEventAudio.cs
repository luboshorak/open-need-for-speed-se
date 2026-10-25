using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GameEngine;
using Microsoft.Xna.Framework;
using OpenNFS1.Parsers.Audio;
using OpenNFS1.Vehicles;

namespace OpenNFS1.Audio
{
    internal sealed class RaceEventAudio
    {
        private readonly Dictionary<int, OriginalSoundVoice> _police = new();
        private readonly Dictionary<string, OriginalSoundVoice> _speech = new();
        private bool _loaded;
        private int _lap;
        private int _best = int.MaxValue;
        private float _beepWait;
        private void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                var bank = new BnkFile("NFS_FMMB.BNK");
                foreach (int slot in new[] { OriginalAudioRules.Radar, OriginalAudioRules.Siren, OriginalAudioRules.SirenEcho })
                    try { _police.Add(slot, new OriginalSoundVoice(bank.GetBySlot(slot), "NFS_FMMB.BNK:" + slot, slot != OriginalAudioRules.Radar, true)); }
                    catch (Exception ex) { AudioDiagnostics.Record(ex); }
            }
            catch (Exception ex) { AudioDiagnostics.Record(ex); }
            foreach (string name in new[] { "BESTTIME.EAS", "BESTLAST.EAS", "FINALLAP.EAS" })
            {
                try
                {
                    string path = Path.Combine(GameConfig.CdDataPath, "FRONTEND", "SPEECH", name);
                    _speech.Add(name, new OriginalSoundVoice(EacsFile.Read(path), name, false));
                }
                catch (Exception ex) { AudioDiagnostics.Record(ex); }
            }
        }
        public void Update(Race race)
        {
            if (!GameConfig.SoundEffectsEnabled) { StopAll(); return; }
            Load();
            var player = race.Player.Vehicle;
            var cop = (race.PolicePursuitActive || race.PoliceRadarNear) ? race.Drivers.OfType<PoliceDriver>().FirstOrDefault()?.Vehicle : null;
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            float volume = 0, pan = 0;
            if (cop != null)
            {
                Vector3 offset = cop.Position - player.Position;
                float distance = offset.Length();
                volume = race.PolicePursuitActive ? .75f * Math.Clamp(1 - distance / 400f, 0, 1) : 0;
                pan = distance > .01f ? Math.Clamp(Vector3.Dot(offset / distance, player.Right), -1, 1) : 0;
                int gap = (cop.CurrentNode?.Number ?? 0) - (player.CurrentNode?.Number ?? 0);
                float interval = race.PoliceRadarNear ? OriginalAudioRules.RadarPeriod(gap, gap < 0) : float.PositiveInfinity;
                _beepWait = Math.Min(_beepWait, interval) - dt;
                if (float.IsFinite(interval) && _beepWait <= 0)
                {
                    if (_police.TryGetValue(OriginalAudioRules.Radar, out var beep)) beep.Trigger(.85f);
                    _beepWait = interval;
                }
            }
            else _beepWait = 0;
            if (_police.TryGetValue(OriginalAudioRules.Siren, out var siren)) siren.Set(volume, 64, pan);
            if (_police.TryGetValue(OriginalAudioRules.SirenEcho, out var echo))
                echo.Set(OriginalAudioRules.Tunnel(player.CurrentNode?.NodeProperty ?? 0) ? volume * .3f : 0, 64, -pan);
            if (!GameConfig.SpeechEnabled) foreach (var voice in _speech.Values) voice.Stop();
            int lap = race.PlayerStats.CurrentLap;
            if (lap != _lap)
            {
                bool best = false;
                if (lap > 1 && race.PlayerStats.LapTimes.Count > 0)
                {
                    int last = race.PlayerStats.LapTimes.Last();
                    best = last < _best;
                    _best = Math.Min(_best, last);
                }
                string speech = OriginalAudioRules.LapSpeech(race.Track.IsOpenRoad, lap, race.NbrLaps, best);
                if (GameConfig.SpeechEnabled && speech != null && _speech.TryGetValue(speech, out var voice))
                {
                    foreach (var previous in _speech.Values) previous.Stop();
                    voice.Trigger(.9f);
                }
                _lap = lap;
            }
        }
        public void StopAll()
        {
            foreach (var voice in _police.Values) voice.Dispose();
            foreach (var voice in _speech.Values) voice.Dispose();
            _police.Clear(); _speech.Clear(); _loaded = false; _beepWait = 0;
        }
    }
}
