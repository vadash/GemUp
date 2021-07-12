using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
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
            Input.RegisterKey(Keys.MButton);
            return true;
        }

        private void Start()
        {
            if (!GameController.Area.CurrentArea.HasWaypoint) return;
            if (Core.ParallelRunner.FindByName("GemUp") != null) return;
            _gemUpCoroutine = new Coroutine(MainWorkCoroutine(), this, "GemUp");
            Core.ParallelRunner.Run(_gemUpCoroutine);
        }
        
        private IEnumerator MainWorkCoroutine()
        {
            yield return GemItUp();
        }
        
        public override Job Tick()
        {
            if (!Settings.Enable) return null;

            if (!GameController.Window.IsForeground() ||
                GameController.IngameState.IngameUi.InventoryPanel.IsVisible ||
                GameController?.Player?.IsDead == true ||
                GameController?.Player?.GetComponent<Actor>()?.CurrentAction != null ||
                GameController?.Player?.GetComponent<Actor>()?.isMoving != false ||
                Input.IsKeyDown(Keys.Escape) ||
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
            var oldMousePosition = Input.ForceMousePosition;
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;

            if (skillGemLevelUps?.Children != null)
                foreach (var element in skillGemLevelUps.Children.Reverse())
                {
                    var skillGemButton = element.GetChildAtIndex(1).GetClientRect();
                    var skillGemText = element.GetChildAtIndex(3).Text;
                    if (element.GetChildAtIndex(2).IsVisibleLocal) continue;
                    if (skillGemText?.ToLower() == "click to level up")
                    {
                        Input.SetCursorPos(skillGemButton.Center + _clickWindowOffset);
                        Input.Click(MouseButtons.Left);
                        yield return new WaitTime(50);
                    }
                }

            Input.SetCursorPos(oldMousePosition);
            yield return _waitForNextTry;
        }
    }
}