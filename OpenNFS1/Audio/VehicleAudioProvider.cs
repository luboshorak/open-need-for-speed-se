using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
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

                _accelerating = CreateEffect(carBank.Samples[0]).CreateInstance();
                _coasting = CreateEffect(carBank.Samples[1]).CreateInstance();
                _accelerating.IsLooped = true;
                _coasting.IsLooped = true;

                if (carBank.Samples.Count > 3)
                    _gearChange = CreateEffect(carBank.Samples[3]);

                try
                {
                    var environmentBank = new BnkFile("COLL_SW.BNK");
                    // Index 5 is the grass-slide effect in the original sound bank.
                    if (environmentBank.Samples.Count > 5)
                    {
                        _offRoad = CreateEffect(environmentBank.Samples[5]).CreateInstance();
                        _offRoad.IsLooped = true;
                    }
                    if (environmentBank.Samples.Count > 0)
                    {
                        _skidEffect = CreateEffect(environmentBank.Samples[0]);
                        _skidLoop = _skidEffect.CreateInstance();
                        _skidLoop.IsLooped = true;
                        _skidLoop.Volume = 0.24f;
                    }
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

        internal static SoundEffect CreateEffect(BnkSample sample)
        {
            return new SoundEffect(sample.PCMData, sample.SampleRate,
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
            var acceleration = _car.Motor.Throttle != 0 || _car.Motor.AtRedline;
            SetPlayback(_accelerating, acceleration);
            SetPlayback(_coasting, !acceleration);
            float pitch = (_car.Motor.Rpm / Math.Max(1f, _car.Motor.RedlineRpm)) - 0.5f;
            pitch = Math.Clamp(pitch, -1f, 1f);
            if (_accelerating != null) { _accelerating.Pitch = pitch; _accelerating.Volume = 0.3f; }
            if (_coasting != null) { _coasting.Pitch = pitch; _coasting.Volume = 0.3f; }
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
        }
    }
}
