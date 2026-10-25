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
        private float _engineBlend;

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
                        _skidEffect = CreateEffect(environmentBank.Samples[0]);
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
            // Each car already selects its own original SoundBnkFile. Keep
            // both genuine BNK engine loops running and crossfade by load,
            // rather than abruptly stopping/restarting at each throttle tap.
            SetPlayback(_accelerating, true);
            SetPlayback(_coasting, true);
            float throttle = Math.Clamp(_car.Motor.Throttle, 0f, 1f);
            float target = Math.Clamp(throttle + (_car.Motor.AtRedline ? 0.2f : 0f), 0f, 1f);
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0f, .1f);
            _engineBlend = MathHelper.Lerp(_engineBlend, target,
                Math.Clamp(dt * 5f, 0f, 1f));
            float normalized = Math.Clamp(
                (_car.Motor.Rpm - 0.8f) / Math.Max(0.1f, _car.Motor.RedlineRpm - 0.8f),
                0f, 1f);
            float pitch = Math.Clamp((normalized - 0.5f) * 1.2f, -1f, 1f);
            if (_accelerating != null)
            {
                _accelerating.Pitch = pitch;
                _accelerating.Volume = 0.32f * _engineBlend;
            }
            if (_coasting != null)
            {
                _coasting.Pitch = pitch;
                _coasting.Volume = 0.32f * (1f - _engineBlend);
            }
        }

        public void PlaySkid(bool play)
        {
            if (!_isActive || _skidEffect == null || !play) return;
            // One-shots with MonoGame SoundEffect.Play avoid leaking looping instances.
            if (_skidDelay <= 0f)
            {
                _skidEffect.Play(0.25f, 0f, 0f);
                _skidDelay = 0.45f;
            }
        }
        private float _skidDelay;

        public void Tick(float seconds)
        {
            if (_skidDelay > 0f) _skidDelay -= seconds;
        }

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
        }
    }
}
