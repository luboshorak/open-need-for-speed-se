using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using GameEngine;
using OpenNFS1.Audio;
using OpenNFS1.Parsers.Audio;
using OpenNFS1.Physics;

namespace OpenNFS1
{
    class VehicleAudioProvider
    {
        private readonly DrivableVehicle _car;
        private readonly Dictionary<string, OriginalSoundVoice> _voices = new();
        private bool _active;
        private float _load, _skid, _offRoad, _ambience, _gravelWait, _roadPhase;
        private string _commonBank;
        public VehicleAudioProvider(DrivableVehicle car) { _car = car; }
        public void Initialize()
        {
            if (_active) return;
            _active = true;
            _commonBank = OriginalAudioRules.CollisionBank(GameConfig.SelectedTrackDescription?.FileName);
            LoadBank(_car.Descriptor.SoundBnkFile, new[] { ("power", 1, true), ("coast", 2, true),
                ("horn", 3, true), ("gear", 32, false) });
            LoadBank(_commonBank, new[] { ("skid", 42, true), ("offroad", 38, true),
                ("road", 41, true), ("ambience", OriginalAudioRules.AmbienceSlot(_commonBank), true),
                ("gravel", 80, false) });
        }
        private void LoadBank(string name, (string key, int slot, bool loop)[] entries)
        {
            BnkFile bank;
            try { bank = new BnkFile(name); }
            catch (Exception ex) { AudioDiagnostics.Record(ex); return; }
            foreach (var entry in entries)
            {
                try { _voices.Add(entry.key, new OriginalSoundVoice(bank.GetBySlot(entry.slot),
                    name + ":" + entry.slot + " " + entry.key, entry.loop)); }
                catch (Exception ex) { AudioDiagnostics.Record(ex); }
            }
        }
        internal static SoundEffect CreateEffect(BnkSample sample, bool full = false)
        {
            if (sample == null) throw new InvalidDataException("Missing BNK sound slot");
            return new SoundEffect(full ? sample.FullPCMData : sample.PCMData, sample.SampleRate,
                sample.NbrChannels == 2 ? AudioChannels.Stereo : AudioChannels.Mono);
        }
        private void Set(string key, float volume, int bend = 64, float pan = 0)
        {
            if (_active && _voices.TryGetValue(key, out var voice)) voice.Set(volume, bend, pan);
        }
        private void Trigger(string key, float volume)
        {
            if (_active && _voices.TryGetValue(key, out var voice)) voice.Trigger(volume);
        }
        public void UpdateEngine()
        {
            if (!_active) return;
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            float target = _car.Motor.Gearbox.GearEngaged ? Math.Clamp(_car.Motor.Throttle, 0, 1) : 0;
            // Original load follows throttle faster on attack than release (30 Hz physics).
            float response = target > _load ? .5f : .125f;
            _load += (target - _load) * (1f - MathF.Pow(1f - response, dt * 30f));
            int bend = (int)Math.Clamp(_car.Motor.Rpm * 127f / (_car.Motor.RedlineRpm + 2f), 0, 127);
            float level = Math.Clamp((bend + 20f) / 127f, .1f, 1f);
            Set("power", .55f * level * MathF.Sqrt(_load), bend);
            Set("coast", .55f * level * MathF.Sqrt(1f - _load), bend);
        }
        public void PlaySkid(float intensity)
        {
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            _skid += (Math.Clamp(intensity, 0, 1) - _skid) * (1f - MathF.Exp(-12f * dt));
            Set("skid", .8f * _skid, (int)(48 + 60 * _skid));
        }
        public void PlayOffRoad(float intensity)
        {
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            _offRoad += (Math.Clamp(intensity, 0, 1) - _offRoad) * (1f - MathF.Exp(-10f * dt));
            Set("offroad", .65f * _offRoad);
        }
        public void UpdateRoad(float speed, bool grounded, int outside, byte property)
        {
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            bool tunnel = OriginalAudioRules.Tunnel(property);
            bool rough = OriginalAudioRules.Rough(property);
            float speedMps = Math.Abs(speed) / 3.6f;
            float gain = grounded ? Math.Clamp(speedMps * 2 / 127f, 0, 1) : 0;
            int bend = tunnel ? 94 : outside > 0 ? 24 : 64;
            _roadPhase += dt * speedMps;
            if (rough) bend = 64 + (int)(30 * (.5f + .5f * MathF.Sin(_roadPhase)));
            if (grounded && speedMps > 0 && tunnel) gain = Math.Min(1, gain + 20f / 127f);
            Set("road", gain * .65f, bend);
            int side = OriginalAudioRules.AmbienceSide(property);
            _ambience += ((side != 0 ? .6f : 0) - _ambience) * (1f - MathF.Exp(-6f * dt));
            Set("ambience", _ambience, _commonBank == "COLLSWMT.BNK" ? 0 : 64, side);
            _gravelWait -= dt;
            bool rally = Path.GetFileNameWithoutExtension(GameConfig.SelectedTrackDescription?.FileName ?? "") == "tr5";
            if (rally && grounded && speedMps > 4 && (rough || outside > 0) && _gravelWait <= 0)
            {
                Trigger("gravel", Math.Clamp(speedMps / 40, .1f, .6f));
                _gravelWait = .25f + (float)Engine.Instance.Random.NextDouble() * .6f;
            }
        }
        public void Tick(float seconds) { }
        public void ChangeGear() => Trigger("gear", .65f);
        public void HitGround() { if (_active) EnvironmentAudioProvider.Instance.PlaySlot(OriginalAudioRules.Landing, .65f); }
        public void PlayHorn(bool held) => Set("horn", held ? .7f : 0f);
        public void StopAll()
        {
            foreach (var voice in _voices.Values) voice.Dispose();
            _voices.Clear(); _active = false;
            _load = _skid = _offRoad = _ambience = _gravelWait = 0;
        }
    }
}
