using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using OpenNFS1.Parsers.Audio;

namespace OpenNFS1.Audio
{
    internal static class AudioAudit
    {
        // Exercise the actual MonoGame backend without a race, graphics or audible output.
        public static int Run(string root, string report)
        {
            var log = new StringBuilder(); int passed = 0, failed = 0;
            GameConfig.CdDataPath = root;
            float master = SoundEffect.MasterVolume;
            SoundEffect.MasterVolume = 0;
            void Check(BnkSample sample, string name, bool loop)
            {
                try
                {
                    using var voice = new OriginalSoundVoice(sample, name, loop);
                    voice.Set(.5f);
                    if (voice.State != SoundState.Playing) throw new InvalidOperationException("Voice did not start");
                    voice.Stop();
                    if (voice.State != SoundState.Stopped) throw new InvalidOperationException("Voice did not stop");
                    voice.Set(.5f);
                    if (voice.State != SoundState.Playing) throw new InvalidOperationException("Voice did not restart");
                    passed++; log.AppendLine("PASS " + name);
                }
                catch (Exception ex) { failed++; log.AppendLine("FAIL " + name + " " + ex); }
            }
            void Bank(string name, int[] slots, Func<int, bool> loop)
            {
                try
                {
                    var bank = new BnkFile(name);
                    foreach (int slot in slots) Check(bank.GetBySlot(slot), name + ":" + slot, loop(slot));
                }
                catch (Exception ex) { failed++; log.AppendLine("FAIL " + name + " " + ex); }
            }
            try
            {
                foreach (string bank in new[] { "911_SW.BNK", "SUPRA_SW.BNK", "RX7_SW.BNK", "NSX_SW.BNK",
                    "ZR1_SW.BNK", "VIPERSW.BNK", "TR512_SW.BNK", "DIABLOSW.BNK" })
                {
                    Bank(bank, new[] { 1,2,3,32 }, slot => slot != 32);
                    Bank(OriginalAudioRules.OpponentBank(bank), new[] { 65,67 }, slot => true);
                }
                foreach (string bank in new[] { "COLLSWWT.BNK", "COLLSWMT.BNK" })
                    Bank(bank, new[] {33,34,35,36,37,38,41,42,48,56,63,80,OriginalAudioRules.AmbienceSlot(bank)},
                        slot => slot > 37 && slot != 80);
                Bank("NFS_FMMB.BNK", new[] {97,98,100}, slot => slot != 97);
                foreach (string path in Directory.GetFiles(Path.Combine(root,"FRONTEND","SPEECH"), "*.EAS"))
                {
                    string file = Path.GetFileName(path);
                    try { Check(EacsFile.Read(Path.Combine(root,"FRONTEND","SPEECH",file)), file, false); }
                    catch (Exception ex) { failed++; log.AppendLine("FAIL " + file + " " + ex); }
                }
            }
            finally
            {
                SoundEffect.MasterVolume = master;
                log.AppendLine($"Backend start/stop/restart: {passed} passed, {failed} failed. Output muted; no listening test.");
                File.WriteAllText(report, log.ToString());
            }
            return failed == 0 ? 0 : 1;
        }
    }
}
