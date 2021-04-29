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
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();
        private readonly WaitTime _waitForNextTry = new WaitTime(10000);
        private Vector2 _clickWindowOffset;
        private uint _coroutineCounter;
        private bool _fullWork = true;
        private Coroutine _gemUpCoroutine;
        private readonly Stopwatch _idleWatch = new Stopwatch();

        public GemUp()
        {
            Name = "GemUp";
        }

        public override bool Initialise()
        {
            _gemUpCoroutine = new Coroutine(MainWorkCoroutine(), this, "Gem Up");
            Core.ParallelRunner.Run(_gemUpCoroutine);
            _gemUpCoroutine.Pause();
            _debugTimer.Reset();
            return true;
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
            if (Input.GetKeyState(Keys.Escape)) _gemUpCoroutine.Pause();

            if (GameController?.Player?.GetComponent<Actor>()?.CurrentAction != null ||
                GameController?.Player?.GetComponent<Actor>()?.isMoving != false ||
                Input.IsKeyDown(Keys.LButton) ||
                Input.IsKeyDown(Keys.MButton))
            {
                _idleWatch.Restart();
            }

            if (_idleWatch.ElapsedMilliseconds > 2000)
            {
                _debugTimer.Restart();

                if (_gemUpCoroutine.IsDone)
                {
                    var firstOrDefault =
                        Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(GemUp));

                    if (firstOrDefault != null)
                        _gemUpCoroutine = firstOrDefault;
                }

                _gemUpCoroutine.Resume();
                _fullWork = false;
            }
            else
            {
                if (_fullWork)
                {
                    _gemUpCoroutine.Pause();
                    _debugTimer.Reset();
                }
            }

            if (_debugTimer.ElapsedMilliseconds > 2000)
            {
                _fullWork = true;
                LogMessage("Error gem up stop after time limit 2000 ms");
                _debugTimer.Reset();
            }

            return null;
        }


        //main
        private IEnumerator GemItUp()
        {
            if (!GameController.Window.IsForeground()) yield break;
            if (GameController.IngameState.IngameUi.InventoryPanel.IsVisible) yield break;
            yield return TryToGemUp();
            _fullWork = true;
        }

        private IEnumerator TryToGemUp()
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

        public override void OnPluginDestroyForHotReload()
        {
            _gemUpCoroutine.Done(true);
        }
    }
}