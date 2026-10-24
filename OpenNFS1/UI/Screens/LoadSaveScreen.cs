using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Restores the original LOADSAVE.QFS page, backed by safe local user-profile
    /// saves instead of writing into original NFS SE CD/game data.
    /// </summary>
    internal sealed class LoadSaveScreen : IGameScreen
    {
        private readonly BitmapChunk _art;
        private readonly SpriteFont _font;
        private readonly IGameScreen _returnTo;
        private readonly TrackDescription _upcomingTrackOverride;
        private int _slot = 1;
        private string _message = "Choose slot 1-3, then Save or Load.";
        private bool _confirmDelete;

        public LoadSaveScreen(IGameScreen returnTo, TrackDescription upcomingTrackOverride = null)
        {
            _returnTo = returnTo ?? new HomeScreen();
            _upcomingTrackOverride = upcomingTrackOverride;
            _art = new QfsFile(@"FRONTEND\ART\OPTION\LOADSAVE.QFS").Content.Header;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
        }

        private BitmapEntry Art(string id) => _art.FindByName(id);

        private bool Hit(string id, Vector2 p)
        {
            BitmapEntry bmp = Art(id);
            if (bmp?.Texture == null || p.X < 0) return false;
            Vector2 v = bmp.GetDisplayAt();
            return new Rectangle((int)v.X, (int)v.Y,
                bmp.Texture.Width, bmp.Texture.Height).Contains((int)p.X, (int)p.Y);
        }

        private void DrawArt(string id)
        {
            BitmapEntry bmp = Art(id);
            if (bmp?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(bmp.Texture, bmp.GetDisplayAt(), Color.White);
        }

        private void Save()
        {
            _confirmDelete = false;
            try
            {
                TrackDescription upcoming = _upcomingTrackOverride ??
                    TournamentSaveManager.SuggestedNextTrack();
                if (upcoming == null)
                    _message = "No active championship. Start Tournament first.";
                else
                {
                    TournamentSaveManager.Save(_slot, upcoming);
                    _message = "Tournament saved to slot " + _slot + ".";
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is InvalidOperationException)
            {
                _message = ex.Message;
            }
        }

        private void Load()
        {
            _confirmDelete = false;
            try
            {
                TournamentSaveManager.Restore(_slot);
                Engine.Instance.Screen = new LoadRaceScreen();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is InvalidOperationException || ex is System.Text.Json.JsonException)
            {
                _message = "Unable to load: " + ex.Message;
            }
        }

        private void Delete()
        {
            if (!_confirmDelete)
            {
                _confirmDelete = true;
                _message = "Press D again to delete slot " + _slot + ".";
                return;
            }

            try
            {
                TournamentSaveManager.Delete(_slot);
                _message = "Save slot " + _slot + " deleted.";
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _message = ex.Message;
            }
            finally
            {
                _confirmDelete = false;
            }
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape)) { Engine.Instance.Screen = _returnTo; return; }
            if (input.WasPressed(Keys.Left) || input.WasPressed(Keys.Up))
            {
                _slot = (_slot + 1) % 3 + 1; // backwards in 1,2,3
                _confirmDelete = false;
            }
            if (input.WasPressed(Keys.Right) || input.WasPressed(Keys.Down))
            {
                _slot = _slot % 3 + 1;
                _confirmDelete = false;
            }
            if (input.WasPressed(Keys.S)) Save();
            if (input.WasPressed(Keys.L) || input.WasPressed(Keys.Enter)) Load();
            if (input.WasPressed(Keys.D)) Delete();

            if (!input.WasLeftMouseClicked) return;
            Vector2 p = input.GameMousePosition;
            if (Hit("savd", p)) Save();
            else if (Hit("load", p)) Load();
            else if (Hit("dlgd", p)) Delete();
            else if (Hit("dond", p)) Engine.Instance.Screen = _returnTo;
            else if (p.X >= 310 && p.X <= 635 && p.Y >= 140 && p.Y < 350)
            {
                _slot = Math.Clamp((int)((p.Y - 140) / 70) + 1, 1, 3);
                _confirmDelete = false;
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
            DrawArt("bgnd");
            DrawArt("savd");
            DrawArt("load");
            DrawArt("dlgd");
            DrawArt("dond");

            // Keep currently unsupported original load/save menu actions visible
            // only as part of the source artwork; do not pretend they work.
            DrawArt("sard"); DrawArt("vwrd"); DrawArt("vwhd"); DrawArt("dlrd");

            Text("TOURNAMENT SAVE SLOTS", 332, 104, 0.43f, Color.Yellow);
            for (int slot = 1; slot <= 3; slot++)
            {
                int y = 151 + 70 * (slot - 1);
                Text((slot == _slot ? "> " : "  ") + "SLOT " + slot, 328, y, 0.38f,
                    slot == _slot ? Color.Yellow : Color.White);
                string line = TournamentSaveManager.Describe(slot);
                if (line.Length > 34) line = line.Substring(0, 34);
                Text(line, 334, y + 24, 0.28f, Color.White);
            }
            Text("S=Save  L=Load  D=Delete", 314, 360, 0.30f, Color.Yellow);
            if (_message.Length > 74) _message = _message.Substring(0, 71) + "...";
            Text(_message, 20, 409, 0.29f, Color.Yellow);
            sprite.End();
        }

        private void Text(string s, int x, int y, float scale, Color color)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, s,
                new Vector2(x, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
