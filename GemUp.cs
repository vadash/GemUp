using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace GemUp
{
    public class GemUp : BaseSettingsPlugin<GemUpSettings>
    {
        private readonly WaitTime _waitForNextTry = new WaitTime(1000);
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
            Input.RegisterKey(Keys.RButton);
            return true;
        }

        private void Start()
        {

            if (GameController.Area.CurrentArea.IsHideout || GetPlayerLevel() < 85)
            {
                if (Core.ParallelRunner.FindByName("GemUp") != null) return;
                _gemUpCoroutine = new Coroutine(MainWorkCoroutine(), this, "GemUp");
                Core.ParallelRunner.Run(_gemUpCoroutine);                
            }
        }
        
        private int GetPlayerLevel()
        {
            try
            {
                return GameController.Player.Stats[GameStat.Level];
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private IEnumerator MainWorkCoroutine()
        {
            yield return GemItUp();
        }
        
        private IEnumerator GemItUp()
        {
            if (!GameController.Window.IsForeground()) yield break;
            if (GameController?.Player?.IsDead == true && GameController?.Player?.GetComponent<Life>()?.CurHP == 0) yield break;
            var skillGemLevelUps = GameController.Game.IngameState.IngameUi.GetChildAtIndex(4).GetChildAtIndex(1).GetChildAtIndex(0);
            if (skillGemLevelUps == null || !skillGemLevelUps.IsVisible) yield return _waitForNextTry;
            var oldMousePosition = Input.ForceMousePositionNum;
            if (skillGemLevelUps?.Children != null)
            {
                foreach (var element in skillGemLevelUps.Children.Reverse())
                {
                    var skillGemButton = element.GetChildAtIndex(1).GetClientRect();
                    var skillGemText = element.GetChildAtIndex(3).Text;
                    if (element.GetChildAtIndex(2).IsVisibleLocal) continue;
                    if (skillGemText?.ToLower() == "click to level up")
                    {
                        yield return ClickForLevelUp(skillGemButton, 100, 2);
                    }
                }
            }
            Input.SetCursorPos(oldMousePosition);
            yield return _waitForNextTry;
        }

        private IEnumerator ClickForLevelUp(RectangleF skillGemButton, int maxTimeMs, int extraDelayMs)
        {
            var hits = 0;
            for (int i = 0; i <= maxTimeMs; i++)
            {
                Input.SetCursorPos(skillGemButton.ClickRandomNum());
                yield return new WaitTime(1);
                if (i == maxTimeMs)
                {
                    break;
                }
                if (GameController?.IngameState?.UIHover?.PathFromRoot?.ToLower()?.Contains(@"gemlvlup") == true)
                {
                    hits += 2;
                }
                else
                {
                    if (hits > 0) hits -= 1;
                }
                if (hits >= 5)
                {
                    Input.Click(MouseButtons.Left);
                    yield return new WaitTime(extraDelayMs);
                    break;
                }
            }
        }

        public override Job Tick()
        {
            if (!Settings.Enable) return null;

            try
            {
                #region wait for idle keys
                if (GameController.IngameState.IngameUi.InventoryPanel.IsVisible ||
                    Input.IsKeyDown(Keys.Escape) ||
                    Input.IsKeyDown(Keys.LButton) ||
                    //Input.IsKeyDown(Keys.RButton) ||
                    Input.IsKeyDown(Keys.MButton))
                {
                    _idleWatch.Restart();
                    //_gemUpCoroutine.Done(true);
                    return null;
                }
                #endregion

                #region wait for high hp
                var lifeComponet = GameController.Player.GetComponent<Life>();
                if (lifeComponet?.CurHP < 0.90f * lifeComponet?.MaxHP)
                {
                    _idleWatch.Restart();
                    _gemUpCoroutine.Done(true);
                    return null;
                }
                #endregion

                #region wait for no monsters nearby
                var noMonstersNearby = GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Monster]?.All(x => x?.DistancePlayer > 15);
                if (noMonstersNearby == false)
                {
                    _idleWatch.Restart();
                    _gemUpCoroutine.Done(true);
                    return null;
                }
                #endregion

                if (_idleWatch.ElapsedMilliseconds > 1000) Start();
            }
            catch (Exception)
            {
                // hide exception
            }

            return null;
        }
    }
}