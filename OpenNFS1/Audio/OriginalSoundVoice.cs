using System;
using Microsoft.Xna.Framework.Audio;
using OpenNFS1.Parsers.Audio;

namespace OpenNFS1.Audio
{
    internal sealed class OriginalSoundVoice : IDisposable
    {
        private readonly SoundEffect _effect;
        private SoundEffectInstance _voice;
        private readonly bool _loop;
        private bool _started;
        private readonly BnkSample _sample;
        private readonly string _label;
        private bool _logged;
        public SoundState State => _voice.State;
        public OriginalSoundVoice(BnkSample sample, string label, bool loop, bool spatial = false)
        {
            _sample = sample ?? throw new System.IO.InvalidDataException("Missing " + label);
            _label = label;
            _loop = loop;
            byte[] pcm = sample.FullPCMData;
            int channels = sample.NbrChannels;
            if (spatial && channels == 2)
            {
                var mono = new byte[pcm.Length / 2];
                for (int i = 0; i < mono.Length / 2; i++)
                {
                    short value = (short)((BitConverter.ToInt16(pcm, i * 4) + BitConverter.ToInt16(pcm, i * 4 + 2)) / 2);
                    mono[i * 2] = (byte)value; mono[i * 2 + 1] = (byte)(value >> 8);
                }
                pcm = mono; channels = 1;
            }
            _effect = loop ? new SoundEffect(pcm, 0, pcm.Length,
                sample.SampleRate, channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono,
                sample.LoopStart, sample.LoopLength) : new SoundEffect(pcm, sample.SampleRate,
                channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono);
            _voice = _effect.CreateInstance();
            _voice.IsLooped = loop;
            _voice.Volume = 0;
            AudioDiagnostics.Note("Loaded " + label);
        }
        public void Set(float volume, int bend = 64, float pan = 0)
        {
            _voice.Volume = Math.Clamp(volume * _sample.Gain, 0f, 1f);
            _voice.Pitch = OriginalAudioRules.Pitch(bend, _sample.PitchReach, _sample.PitchCents);
            _voice.Pan = Math.Clamp(pan, -1f, 1f);
            if (volume <= .002f) { Stop(); return; }
            if (_voice.State == SoundState.Stopped) _voice.Play();
            else if (_voice.State == SoundState.Paused) _voice.Resume();
            _started = true;
            if (!_logged) { AudioDiagnostics.Note("Playing " + _label + " state=" + _voice.State); _logged = true; }
        }
        public void Trigger(float volume = 1, float pan = 0) { Stop(); Set(volume, 64, pan); }
        public void Stop()
        {
            if (!_started) return;
            // XAudio flush is asynchronous: immediately calling Play on the old instance
            // can see Playing and be ignored. A new instance has no pending buffer.
            _voice.Stop(); _voice.Dispose();
            _voice = _effect.CreateInstance(); _voice.IsLooped = _loop; _voice.Volume = 0;
            _started = false;
        }
        public void Dispose() { _voice.Stop(); _voice.Dispose(); _effect.Dispose(); }
    }
}
