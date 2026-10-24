using System;
using System.IO;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;
using OpenNFS1.Parsers.Track;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original NFS SE FINMENU.QFS shown after completing a race.
    /// Results/standings open separately on source STANDTRK/STANDSEG art.
    /// The established tournament scoring and save state remain unchanged.
    /// </summary>
    class RaceFinishedScreen : IGameScreen
    {
        private readonly Race _race;
        private readonly Track _raceTrack;
        private readonly BitmapChunk _menu;
        private readonly SpriteFont _font;
        private int _selected;
        private string _notice = "N: NEXT RACE   S: SAVE TOURNAMENT   ESC: MENU";

        private static readonly string[] Normal =
            { "cond", "repd", "higd", "gmod", "gmsd", "chad" };
        private static readonly string[] Active =
            { "conl", "repl", "higl", "gmol", "gmsl", "chal" };

        public RaceFinishedScreen(Race race, Track raceTrack)
        {
            _race = race;
            _raceTrack = raceTrack;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _menu = new QfsFile(@"FRONTEND\ART\CHECK\FINMENU.QFS").Content.Header;

            // Same checkpoint logic as the former results page. This constructor
            // is called only once; opening Game Standings must NOT score twice.
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                GameConfig.Tournament != null)
            {
                TrackDescription nextRoad = GameConfig.SelectedTrackDescription.IsOpenRoad
                    ? TrackDescription.GetNextOpenRoadStage(GameConfig.SelectedTrackDescription)
                    : null;
                if (nextRoad != null)
                    GameConfig.Tournament.RecordRoadSegment(
                        GameConfig.SelectedTrackDescription, race.RaceTime);
                else
                    GameConfig.Tournament.RecordResult(
                        race.PlayerStats.Position, race.RaceTime);

                TrackDescription checkpoint = TournamentSaveManager.NextAfterResults();
                if (checkpoint != null)
                {
                    try { TournamentSaveManager.Save(0, checkpoint); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        private void Art(string id)
        {
            BitmapEntry bitmap = _menu.FindByName(id);
            if (bitmap?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(bitmap.Texture, bitmap.GetDisplayAt(), Color.White);
        }

        private bool Hit(string id, Vector2 p)
        {
            BitmapEntry bmp = _menu.FindByName(id);
            if (bmp?.Texture == null || p.X < 0) return false;
            Vector2 xy = bmp.GetDisplayAt();
            return new Rectangle((int)xy.X, (int)xy.Y,
                bmp.Texture.Width, bmp.Texture.Height).Contains((int)p.X, (int)p.Y);
        }

        private void ReturnToMenu()
        {
            GameConfig.Tournament = null;
            Engine.Instance.Screen = new HomeScreen();
        }

        private void OpenSave()
        {
            TrackDescription upcoming = TournamentSaveManager.NextAfterResults();
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                GameConfig.Tournament != null && upcoming != null)
                Engine.Instance.Screen = new LoadSaveScreen(this, upcoming);
            else
                _notice = "NO ACTIVE TOURNAMENT CHECKPOINT TO SAVE.";
        }

        private void Continue()
        {
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                GameConfig.Tournament != null)
            {
                if (GameConfig.SelectedTrackDescription.IsOpenRoad)
                {
                    TrackDescription nextRoad = TrackDescription.GetNextOpenRoadStage(
                        GameConfig.SelectedTrackDescription);
                    if (nextRoad != null)
                    {
                        GameConfig.SelectedTrackDescription = nextRoad;
                        Engine.Instance.Screen = new LoadRaceScreen();
                        return;
                    }
                }

                if (GameConfig.Tournament.TryAdvance())
                {
                    GameConfig.SelectedTrackDescription = GameConfig.Tournament.CurrentCourse;
                    Engine.Instance.Screen = new LoadRaceScreen();
                }
                else ReturnToMenu();
                return;
            }

            if (GameConfig.SelectedTrackDescription.IsOpenRoad)
            {
                TrackDescription next = TrackDescription.GetNextOpenRoadStage(
                    GameConfig.SelectedTrackDescription);
                if (next != null)
                {
                    GameConfig.SelectedTrackDescription = next;
                    Engine.Instance.Screen = new LoadRaceScreen();
                }
                else ReturnToMenu();
            }
            else
                Engine.Instance.Screen = new DoRaceScreen(_raceTrack);
        }

        private void Activate()
        {
            switch (_selected)
            {
                case 0: ReturnToMenu(); break;
                case 1:
                    if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
                        _notice = "REPLAY DOES NOT CHANGE CHAMPIONSHIP RESULTS.";
                    else
                        Engine.Instance.Screen = new DoRaceScreen(_raceTrack);
                    break;
                case 2:
                    _notice = "ORIGINAL HIGHLIGHT VIDEO PLAYBACK IS NOT READY.";
                    break;
                case 3:
                    Engine.Instance.Screen = new OptionsScreen();
                    break;
                case 4:
                    Engine.Instance.Screen = new RaceStandingsScreen(_race, this);
                    break;
                case 5:
                    _notice = "CHAT MODE IS NOT AVAILABLE IN SINGLE PLAYER.";
                    break;
            }
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.M))
            {
                ReturnToMenu();
                return;
            }
            if (input.WasPressed(Keys.N))
            {
                Continue();
                return;
            }
            if (input.WasPressed(Keys.S))
            {
                OpenSave();
                return;
            }

            if (input.WasLeftMouseClicked)
            {
                Vector2 p = input.GameMousePosition;
                for (int i = 0; i < Normal.Length; i++)
                {
                    if (Hit(Normal[i], p) || Hit(Active[i], p))
                    {
                        _selected = i;
                        Activate();
                        return;
                    }
                }
            }
            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
                _selected = (_selected + 1) % Normal.Length;
            else if (input.WasPressed(Keys.Up))
                _selected = (_selected + Normal.Length - 1) % Normal.Length;
            else if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
                Activate();
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");
            for (int i = 0; i < Normal.Length; i++)
                Art(_selected == i ? Active[i] : Normal[i]);

            string prefix = GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                GameConfig.Tournament != null
                ? "TOURNAMENT " + GameConfig.Tournament.CompletedStages + "/" +
                    GameConfig.Tournament.StageCount + "  "
                : "";
            string notice = prefix + _notice;
            if (notice.Length > 83) notice = notice.Substring(0, 83);
            sprite.DrawString(_font, notice, new Vector2(16, 453),
                Color.Yellow, 0, Vector2.Zero, 0.29f, SpriteEffects.None, 0);
            sprite.End();
        }
    }
}
