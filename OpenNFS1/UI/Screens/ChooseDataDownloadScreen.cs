using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameEngine;

namespace OpenNFS1.UI.Screens
{
    /// <summary>Interactive first-run ISO importer; all MonoGame rendering stays on the game thread.</summary>
    class ChooseDataDownloadScreen : BaseUIScreen, IGameScreen
    {
        private Task _importTask;
        private volatile string _status = "Press I or Enter to choose an ISO image.";
        private int _percent;
        private string _error;

        public void Update(GameTime gameTime)
        {
            if (_importTask != null)
            {
                if (!_importTask.IsCompleted)
                    return;

                Task completed = _importTask;
                _importTask = null;
                if (completed.IsFaulted)
                {
                    _error = completed.Exception.GetBaseException().Message;
                    _status = "Import failed. Press I to retry.";
                }
                else if (GameConfig.HasOriginalGameData())
                {
                    Engine.Instance.Screen = new HomeScreen();
                }
                else
                {
                    _error = "Required NFS SE game files are still missing.";
                }
                return;
            }

            if (Engine.Instance.Input.WasPressed(Keys.Escape))
            {
                Engine.Instance.Game.Exit();
                return;
            }

            if (Engine.Instance.Input.WasPressed(Keys.I) || Engine.Instance.Input.WasPressed(Keys.Enter))
                ChooseIso();
        }

        private void ChooseIso()
        {
            // Program.Main has [STAThread], as required for the native Windows file picker.
            using (var dialog = new OpenFileDialog
            {
                Title = "Select your original The Need for Speed SE CD image",
                Filter = "ISO disc images (*.iso)|*.iso",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                RestoreDirectory = true
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                string imagePath = dialog.FileName;
                _error = null;
                _percent = 0;
                _status = "Reading the ISO image...";
                _importTask = Task.Run(() =>
                    IsoDataImporter.Import(imagePath, GameConfig.CdDataPath, (percent, status) =>
                    {
                        Volatile.Write(ref _percent, percent);
                        _status = status;
                    }));
            }
        }

        public override void Draw()
        {
            base.Draw();
            WriteLine("Original NFS SE CD data", Color.Red, 8, 30, 0.7f);
            WriteLine("Select an ISO image of your own game CD.", TextColor, 57, 30, TextSize);
            WriteLine("I / Enter: Import ISO", Color.Yellow, 51, 30, 0.55f);
            WriteLine("Esc: Exit", Color.LightGray, 38, 30, TextSize);

            if (_importTask != null)
            {
                WriteLine("Importing... " + Volatile.Read(ref _percent) + "%", Color.Yellow, 53, 30, TextSize);
                WriteLine(Shorten(_status), TextColor, 27, 30, 0.40f);
            }
            else
            {
                WriteLine(Shorten(_status), TextColor, 53, 30, 0.43f);
            }

            if (!string.IsNullOrEmpty(_error))
            {
                WriteLine("Unable to import:", Color.Red, 35, 30, TextSize);
                WriteLine(Shorten(_error), Color.LightGray, 27, 30, 0.40f);
            }

            Engine.Instance.SpriteBatch.End();
        }

        private static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return text.Length > 55 ? text.Substring(0, 52) + "..." : text;
        }
    }
}
