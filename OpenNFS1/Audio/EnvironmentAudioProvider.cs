using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using OpenNFS1.Parsers.Audio;

namespace OpenNFS1.Audio
{
    class EnvironmentAudioProvider
    {
        private static EnvironmentAudioProvider _instance;
        public static EnvironmentAudioProvider Instance => _instance ??= new EnvironmentAudioProvider();

        private readonly List<SoundEffect> _collisions = new List<SoundEffect>();
        private SoundEffectInstance _collisionInstance;

        private EnvironmentAudioProvider()
        {
            try
            {
                var bank = new BnkFile("COLL_SW.BNK");
                // The five original, one-shot impact samples occupy BNK
                // slots 33–37. 38+ are continuous road/vehicle sounds.
                for (int slot = 33; slot <= 37; slot++)
                {
                    var sample = bank.GetBySlot(slot);
                    if (sample != null)
                        _collisions.Add(VehicleAudioProvider.CreateEffect(sample, full: true));
                }
            }
            catch (Exception ex) { AudioDiagnostics.Record(ex); }
        }

        public void PlayVehicleFenceCollision()
        {
            if (_collisions.Count == 0) return;
            if (_collisionInstance != null && _collisionInstance.State == SoundState.Playing) return;
            _collisionInstance = _collisions[GameEngine.Utility.RandomGenerator.Next(_collisions.Count)].CreateInstance();
            _collisionInstance.Play();
        }

        public void PlayCollision(int index)
        {
            if (_collisions.Count == 0) return;
            _collisions[Math.Clamp(index, 0, _collisions.Count - 1)].Play(0.5f, 0f, 0f);
        }
    }
}
