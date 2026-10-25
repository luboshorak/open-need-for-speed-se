using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using GameEngine;
using OpenNFS1.Audio;
using OpenNFS1.Parsers.Audio;
using OpenNFS1.Physics;

namespace OpenNFS1
{
    // Use original BNK sounds; the 2015 ConvertedAudio/*.xnb folder is not on the CD.
    class VehicleAudioProvider
    {
        private readonly DrivableVehicle _car;
        private SoundEffectInstance _accelerating;
        private SoundEffectInstance _coasting;
        private SoundEffectInstance _offRoad;
        private SoundEffect _gearChange;
        private SoundEffect _skidEffect;
        private bool _isActive;
        private float _engineLoadBlend;
        private SoundEffectInstance _skidLoop;

        public VehicleAudioProvider(DrivableVehicle car) { _car = car; }

        public void Initialize()
        {
            if (_isActive) return;
            try
            {
                var carBank = new BnkFile(_car.Descriptor.SoundBnkFile);
                if (carBank.Samples.Count < 2)
                    throw new InvalidDataException("Vehicle sound bank contains fewer than two engine samples.");

                _accelerating = CreateEffect(carBank.GetBySlot(1)).CreateInstance();
                _coasting = CreateEffect(carBank.GetBySlot(2)).CreateInstance();
                _accelerating.IsLooped = true;
                _coasting.IsLooped = true;

                if (carBank.GetBySlot(3) != null)
                    _gearChange = CreateEffect(carBank.GetBySlot(3), full: true);

                try
                {
                    var environmentBank = new BnkFile("COLL_SW.BNK");
                    // Index 5 is the grass-slide effect in the original sound bank.
                    if (environmentBank.GetBySlot(38) != null)
                    {
                        _offRoad = CreateEffect(environmentBank.GetBySlot(38)).CreateInstance();
                        _offRoad.IsLooped = true;
                    }
                    // Identified by listening to the ORIGINAL NFS SE sound
                    // bank (user verified): COLL_SW.BNK slot 42 is tire skid.
                    // Use only its actual BNK loop region; never replay one
                    // of the one-shot impact samples as steering audio.
                    BnkSample tireSkid = environmentBank.GetBySlot(42);
                    if (tireSkid == null)
                        throw new InvalidDataException(
                            "Original COLL_SW.BNK tire-skid sample slot 42 is missing.");
                    _skidEffect = CreateEffect(tireSkid);
                    _skidLoop = _skidEffect.CreateInstance();
                    _skidLoop.IsLooped = true;
                    _skidLoop.Volume = 0.27f;
                }
                catch (Exception ex) { AudioDiagnostics.Record(ex); }

                _isActive = true;
            }
            catch (Exception ex)
            {
                // Missing or unknown CD sound data must not crash the race.
                _isActive = false;
                AudioDiagnostics.Record(ex);
            }
        }

        internal static SoundEffect CreateEffect(BnkSample sample, bool full = false)
        {
            if (sample == null)
                throw new InvalidDataException("Referenced BNK sound slot is absent.");
            return new SoundEffect(full ? sample.FullPCMData : sample.PCMData,
                sample.SampleRate,
                sample.NbrChannels == 2 ? AudioChannels.Stereo : AudioChannels.Mono);
        }

        private static void SetPlayback(SoundEffectInstance instance, bool play)
        {
            if (instance == null) return;
            if (play)
            {
                if (instance.State == SoundState.Paused) instance.Resume();
                else if (instance.State == SoundState.Stopped) instance.Play();
            }
            else if (instance.State == SoundState.Playing)
                instance.Pause();
        }

        public void UpdateEngine()
        {
            if (!_isActive) return;
            // Original car-specific BNK contains separate power/coasting
            // samples. Keep both playing and crossfade by throttle; repeated
            // Pause/Resume every key press produces pops and audible gaps.
            SetPlayback(_accelerating, true);
            SetPlayback(_coasting, true);
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0f, .1f);
            float load = Math.Clamp(_car.Motor.Throttle, 0f, 1f);
            _engineLoadBlend += (load - _engineLoadBlend) *
                Math.Clamp(dt * 6f, 0f, 1f);
            float redline = Math.Max(1f, _car.Motor.RedlineRpm);
            float fraction = Math.Clamp(_car.Motor.Rpm / redline, 0f, 1f);
            float pitch = Math.Clamp(fraction * 1.15f - .57f, -1f, 1f);
            if (_accelerating != null)
            {
                _accelerating.Pitch = pitch;
                _accelerating.Volume = Math.Clamp(0.35f * _engineLoadBlend, 0, 1);
            }
            if (_coasting != null)
            {
                _coasting.Pitch = pitch;
                _coasting.Volume = Math.Clamp(0.35f * (1f - _engineLoadBlend), 0, 1);
            }
        }

        public void PlaySkid(bool play)
        {
            if (!_isActive || _skidLoop == null) return;
            // The original effect is a continuous tire sound, not a single
            // transient fired only on the first slip frame.
            SetPlayback(_skidLoop, play);
        }

        public void Tick(float seconds) { }

        public void ChangeGear() { if (_isActive && _gearChange != null) _gearChange.Play(0.5f, 0f, 0f); }

        public void HitGround()
        {
            if (!_isActive) return;
            EnvironmentAudioProvider.Instance.PlayCollision(2);
        }

        public void PlayOffRoad(bool play)
        {
            if (!_isActive || _offRoad == null) return;
            _offRoad.Volume = 0.4f;
            SetPlayback(_offRoad, play);
        }

        public void StopAll()
        {
            _accelerating?.Stop();
            _coasting?.Stop();
            _offRoad?.Stop();
            _skidLoop?.Stop();
            _engineLoadBlend = 0;
        }
    }
}
