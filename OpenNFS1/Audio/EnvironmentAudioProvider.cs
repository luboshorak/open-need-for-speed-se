using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenNFS1.Parsers.Audio;

namespace OpenNFS1.Audio
{
    class EnvironmentAudioProvider
    {
        private static EnvironmentAudioProvider _instance;
        public static EnvironmentAudioProvider Instance => _instance ??= new EnvironmentAudioProvider();
        private readonly Dictionary<int, OriginalSoundVoice> _impacts = new();
        private long _lastImpact;
        private EnvironmentAudioProvider()
        {
            string name = OriginalAudioRules.CollisionBank(GameConfig.SelectedTrackDescription?.FileName);
            try
            {
                var bank = new BnkFile(name);
                for (int slot = 33; slot <= 37; slot++)
                    try { _impacts.Add(slot, new OriginalSoundVoice(bank.GetBySlot(slot), name + ":" + slot + " impact", false)); }
                    catch (Exception ex) { AudioDiagnostics.Record(ex); }
            }
            catch (Exception ex) { AudioDiagnostics.Record(ex); }
        }
        public void PlayVehicleFenceCollision(float closingSpeed = 35) => Hit(OriginalAudioRules.Wall, closingSpeed);
        public void PlayVehicleCollision(float closingSpeed) => Hit(OriginalAudioRules.CarImpact(closingSpeed), closingSpeed);
        private void Hit(int slot, float speed)
        {
            if (speed < 2) return;
            long now = Stopwatch.GetTimestamp();
            if (_lastImpact != 0 && (now - _lastImpact) / (double)Stopwatch.Frequency < .12) return;
            _lastImpact = now;
            PlaySlot(slot, Math.Clamp(speed / 90f, .15f, .9f));
        }
        public void PlayCollision(int index, float volume = .5f) => PlaySlot(33 + Math.Clamp(index, 0, 4), volume);
        public void PlaySlot(int slot, float volume)
        {
            if (GameConfig.SoundEffectsEnabled && _impacts.TryGetValue(slot, out var voice)) voice.Trigger(volume);
        }
        public static void StopAll()
        {
            if (_instance == null) return;
            foreach (var voice in _instance._impacts.Values) voice.Dispose();
            _instance = null;
        }
    }
}
