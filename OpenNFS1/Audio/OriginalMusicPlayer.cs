using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;

namespace OpenNFS1
{
    /// <summary>
    /// Plays ORIGINAL music from the user's own CD. EACS/AS4 is not a WAV/Ogg
    /// format, so FFmpeg (optional, user-installed executable) converts only
    /// the selected track to cached 16-bit PCM. No proprietary audio is bundled.
    /// Conversion runs on a worker; MonoGame audio objects stay on game thread.
    /// </summary>
    internal sealed class OriginalMusicPlayer : IDisposable
    {
        public static OriginalMusicPlayer Instance { get; } = new OriginalMusicPlayer();

        private string _wanted;
        private Task<string> _pending;
        private SoundEffect _effect;
        private SoundEffectInstance _player;
        private bool _wasDriving;
        private int _raceTrackNumber;
        private string _status = "Original NFS SE music (EACS): FFmpeg required for first conversion.";
        public string Status => _status;

        private OriginalMusicPlayer() { }

        private static string CacheDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenNFS1", "MusicCache");

        private static string Decode(string source)
        {
            Directory.CreateDirectory(CacheDirectory);
            var file = new FileInfo(source);
            string stem = Path.GetFileNameWithoutExtension(source) + "_" +
                file.Length.ToString() + "_" + file.LastWriteTimeUtc.Ticks;
            string target = Path.Combine(CacheDirectory, stem + ".wav");
            if (File.Exists(target) && new FileInfo(target).Length > 44)
                return target;

            string localFfmpeg = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg.exe");
            string ffmpeg = File.Exists(localFfmpeg) ? localFfmpeg : "ffmpeg";
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp.wav";
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        Arguments = "-hide_banner -loglevel error -nostdin -y -i \"" +
                            source.Replace("\"", "") + "\" -vn -map_metadata -1 " +
                            "-ac 2 -ar 22050 -c:a pcm_s16le \"" + temporary + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };
                try { process.Start(); }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    throw new FileNotFoundException(
                        "Music requires FFmpeg. Install it or put ffmpeg.exe in Tools.", ex);
                }
                if (!process.WaitForExit(120000))
                {
                    process.Kill(entireProcessTree: true);
                    throw new IOException("Converting original music exceeded two minutes.");
                }
                string errors = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0 || !File.Exists(temporary))
                    throw new InvalidDataException("FFmpeg could not decode the original EACS music: " + errors);
                File.Move(temporary, target, overwrite: true);
                return target;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
            }
        }

        private void StopCurrent()
        {
            _player?.Stop();
            _player?.Dispose();
            _effect?.Dispose();
            _player = null;
            _effect = null;
        }

        /// <summary>Call after the game engine updates its current screen.</summary>
        public void Update(bool driving, bool paused)
        {
            if (paused)
            {
                if (_player?.State == SoundState.Playing) _player.Pause();
                return;
            }

            if (!GameConfig.MusicEnabled || (!driving && !GameConfig.MenuMusicEnabled))
            {
                StopCurrent();
                _wanted = null;
                _pending = null;
                _wasDriving = false;
                return;
            }

            if (driving && !_wasDriving) _raceTrackNumber = _raceTrackNumber % 6 + 1;
            _wasDriving = driving;
            string file = driving
                ? (GameConfig.RockMusic ? "ROCK" : "TECHNO") + _raceTrackNumber + ".AS4"
                : "SHOWCASE.ASF";
            string source = Path.Combine(GameConfig.CdDataPath, "FRONTEND", "MUSIC", file);
            if (!File.Exists(source))
            {
                _status = "Original CD music file missing: " + file;
                StopCurrent();
                return;
            }

            if (_wanted != source)
            {
                StopCurrent();
                _wanted = source;
                _pending = Task.Run(() => Decode(source));
                _status = "Preparing original music: " + file;
            }

            if (_pending != null && _pending.IsCompleted)
            {
                var completed = _pending;
                _pending = null;
                if (completed.IsFaulted)
                {
                    _status = completed.Exception.GetBaseException().Message;
                    return;
                }
                try
                {
                    using var stream = File.OpenRead(completed.Result);
                    _effect = SoundEffect.FromStream(stream);
                    _player = _effect.CreateInstance();
                    _player.IsLooped = true;
                    _player.Volume = GameConfig.MusicVolume;
                    _player.Play();
                    _status = "Playing " + file;
                }
                catch (Exception ex)
                {
                    _status = "Could not play decoded music: " + ex.Message;
                    StopCurrent();
                }
            }
            if (_player != null)
            {
                _player.Volume = GameConfig.MusicVolume;
                if (_player.State == SoundState.Paused) _player.Resume();
            }
        }

        public void Dispose()
        {
            StopCurrent();
        }
    }
}
