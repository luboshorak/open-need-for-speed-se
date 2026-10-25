using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameEngine;
using OpenNFS1.Parsers.Audio;
using OpenNFS1.Physics;
using OpenNFS1.Vehicles;

namespace OpenNFS1.Audio
{
    internal sealed class TrafficAudioProvider
    {
        private sealed class Emitter : IDisposable
        {
            public OriginalSoundVoice Engine, Horn, Skid;
            public float LastDistance = -1, HornWait, HornTime;
            public void Dispose() { Engine?.Dispose(); Horn?.Dispose(); Skid?.Dispose(); }
        }
        private readonly Dictionary<Vehicle, Emitter> _emitters = new();
        private readonly Dictionary<string, BnkFile> _banks = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _failed = new(StringComparer.OrdinalIgnoreCase);
        private BnkSample Sample(string bank, int slot)
        {
            if (!_banks.TryGetValue(bank, out var data)) { data = new BnkFile(bank); _banks.Add(bank, data); }
            return data.GetBySlot(slot);
        }
        public void Update(Race race)
        {
            if (!GameConfig.SoundEffectsEnabled) { StopAll(); return; }
            var listener = race.Player.Vehicle;
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            string common = OriginalAudioRules.CollisionBank(race.Track.Description.FileName);
            var audible = race.Drivers.Select(d => d.Vehicle)
                .Where(v => v != listener && Vector3.DistanceSquared(v.Position, listener.Position) < 250f * 250f)
                .OrderBy(v => Vector3.DistanceSquared(v.Position, listener.Position)).Take(2).ToHashSet();
            foreach (var vehicle in _emitters.Keys.ToArray())
                if (!audible.Contains(vehicle)) { _emitters[vehicle].Dispose(); _emitters.Remove(vehicle); }
            foreach (var vehicle in audible)
            {
                string bank = vehicle is DrivableVehicle car ? OriginalAudioRules.OpponentBank(car.Descriptor.SoundBnkFile) : common;
                if (_failed.Contains(bank)) continue;
                try
                {
                    if (!_emitters.TryGetValue(vehicle, out var emitter))
                    {
                        emitter = new Emitter(); _emitters.Add(vehicle, emitter);
                        int engineSlot = vehicle is DrivableVehicle ? 67 : 48;
                        int hornSlot = vehicle is DrivableVehicle ? 65 : 63;
                        emitter.Engine = new OriginalSoundVoice(Sample(bank, engineSlot), bank + ":" + engineSlot + " nearby engine", true, true);
                        emitter.Horn = new OriginalSoundVoice(Sample(bank, hornSlot), bank + ":" + hornSlot + " traffic horn", true, true);
                        emitter.Skid = new OriginalSoundVoice(Sample(common, 56), common + ":56 nearby skid", true, true);
                    }
                    Vector3 offset = vehicle.Position - listener.Position;
                    float distance = offset.Length();
                    float gain = Math.Clamp(1f - distance / 250f, 0, 1);
                    float pan = distance < .01f ? 0 : Math.Clamp(Vector3.Dot(offset / distance, listener.Right), -1, 1);
                    float radial = emitter.LastDistance < 0 || dt <= 0 ? 0 : (distance - emitter.LastDistance) / dt;
                    emitter.LastDistance = distance;
                    float doppler = 343f / (343f + Math.Clamp(radial, -100f, 100f));
                    int bend = (int)Math.Clamp((vehicle is DrivableVehicle ? 64 : 95) * doppler, 0, 127);
                    float skid = vehicle is DrivableVehicle driven && driven.HandbrakeInput && Math.Abs(driven.Speed) > 10 ? .7f : 0;
                    emitter.Engine.Set(.4f * gain * (1 - skid), bend, pan);
                    emitter.Skid.Set(.6f * gain * skid, 64, pan);
                    emitter.HornWait -= dt;
                    emitter.HornTime = Math.Max(0, emitter.HornTime - dt);
                    // Horn request: close oncoming car or a faster car blocked behind the player.
                    bool approaching = Vector3.Dot(vehicle.Direction, listener.Direction) < -.5f;
                    bool behind = Vector3.Dot(offset, listener.Direction) < 0 && vehicle.Speed > listener.Speed + 10;
                    if (distance < 35 && Math.Abs(vehicle.Speed) > 15 && (approaching || behind) && emitter.HornWait <= 0)
                    { emitter.HornTime = .35f; emitter.HornWait = 4f; }
                    emitter.Horn.Set(emitter.HornTime > 0 ? .7f * gain : 0, 64, pan);
                }
                catch (Exception ex)
                {
                    _failed.Add(bank); AudioDiagnostics.Record(ex);
                    if (_emitters.Remove(vehicle, out var broken)) broken.Dispose();
                }
            }
        }
        public void StopAll()
        {
            foreach (var emitter in _emitters.Values) emitter.Dispose();
            _emitters.Clear(); _banks.Clear(); _failed.Clear();
        }
    }
}
