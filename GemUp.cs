using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace GemUp
{
    public class GemUp : BaseSettingsPlugin<GemUpSettings>
    {
        private readonly WaitTime _waitForNextTry = new WaitTime(10000);
        private Vector2 _clickWindowOffset;
        private Coroutine _gemUpCoroutine;
        private readonly Stopwatch _idleWatch = new Stopwatch();

        public GemUp()
        {
            Name = "GemUp";
        }

        public override bool Initialise()
        {
            Input.RegisterKey(Keys.Escape);
            Input.RegisterKey(Keys.LButton);
            Input.RegisterKey(Keys.RButton);
            return true;
        }

        private void Start()
        {
            _gemUpCoroutine = new Coroutine(MainWorkCoroutine(), this, "Gem Up");
            Core.ParallelRunner.Run(_gemUpCoroutine);
        }
        
        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return GemItUp();
            }
            // ReSharper disable once IteratorNeverReturns
        }
        
        public override Job Tick()
        {
            if (!Settings.Enable) return null;

            if (!GameController.Window.IsForeground() ||
                GameController.IngameState.IngameUi.InventoryPanel.IsVisible ||
                GameController?.Player?.IsDead == true ||
                GameController?.Player?.GetComponent<Actor>()?.CurrentAction != null ||
                GameController?.Player?.GetComponent<Actor>()?.isMoving != false ||
                Input.GetKeyState(Keys.Escape) ||
                Input.IsKeyDown(Keys.LButton) ||
                Input.IsKeyDown(Keys.MButton))
            {
                _idleWatch.Restart();
                _gemUpCoroutine.Done(true);
                return null;
            }

            if (_idleWatch.ElapsedMilliseconds > 2500) Start();
            return null;
        }

        private IEnumerator GemItUp()
        {
            var skillGemLevelUps = GameController.Game.IngameState.IngameUi
                .GetChildAtIndex(4).GetChildAtIndex(1).GetChildAtIndex(0);
            if (skillGemLevelUps == null || !skillGemLevelUps.IsVisible) yield return _waitForNextTry;

            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;
            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;

            if (skillGemLevelUps?.Children != null)
                foreach (var element in skillGemLevelUps.Children.Reverse())
                {
                    var skillGemButton = element.GetChildAtIndex(1).GetClientRect();
                    var skillGemText = element.GetChildAtIndex(3).Text;
                    if (element.GetChildAtIndex(2).IsVisibleLocal) continue;
                    var clientRectCenter = skillGemButton.Center;
                    var vector2 = clientRectCenter + _clickWindowOffset;
                    if (skillGemText?.ToLower() == "click to level up")
                    {
                        Mouse.MoveCursorToPosition(vector2);
                        Mouse.MouseMove();
                        yield return new WaitTime(25);
                        if (GameController.IngameState.UIHoverElement.GetClientRectCache.Center.Distance(vector2) < 30)
                        {
                            yield return Mouse.LeftClick();
                            yield return new WaitTime(25);                            
                        }
                    }
                }

            Mouse.MoveCursorToPosition(oldMousePosition);
            yield return _waitForNextTry;
        }
    }
}