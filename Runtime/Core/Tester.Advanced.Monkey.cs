using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Utilities;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Advanced
        {
            private static CancellationTokenSource _monkeyCts;

            /// <summary>
            /// Starts an autonomous 'Chaos Monkey' that performs random valid interactions.
            /// Useful for finding edge-case crashes and state leaks.
            /// </summary>
            /// <param name="durationSeconds">How long to run the monkey (0 for infinite).</param>
            /// <param name="interactionInterval">Average time between random actions.</param>
            public static async Task StartChaosMonkey(float durationSeconds = 60f, float interactionInterval = 0.3f)
            {
                if (!RealPlayEnvironment.IsEnabled) return;

                StopChaosMonkey();
                _monkeyCts = new CancellationTokenSource();
                var token = _monkeyCts.Token;

                RealPlayLog.Info($"[Monkey] Starting Chaos Monkey for {durationSeconds:F1}s (Interval: {interactionInterval:F2}s)");
                
                float startTime = Time.realtimeSinceStartup;
                int actionCount = 0;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (durationSeconds > 0 && Time.realtimeSinceStartup - startTime > durationSeconds) break;

                        await PerformRandomAction();
                        actionCount++;

                        float jitter = UnityEngine.Random.Range(0.5f, 1.5f);
                        await Wait.Seconds(interactionInterval * jitter);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    RealPlayLog.Error($"[Monkey] Chaos Monkey crashed: {ex.Message}");
                }
                finally
                {
                    RealPlayLog.Info($"[Monkey] Chaos Monkey finished. Actions performed: {actionCount}");
                    _monkeyCts = null;
                }
            }

            /// <summary>
            /// Stops the active Chaos Monkey.
            /// </summary>
            public static void StopChaosMonkey()
            {
                _monkeyCts?.Cancel();
                _monkeyCts = null;
            }

            private static async Task PerformRandomAction()
            {
                var candidates = GetMonkeyActionTargets();
                if (candidates.Count == 0) return;

                var target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                string intent = PickRandomIntent(target);

                RealPlayLog.Info($"[Monkey] Action {intent} on '{target.name}'");
                await Interaction.Perform(intent, target);
            }

            private static List<GameObject> GetMonkeyActionTargets()
            {
                var all = SceneCache.Instance.AllActiveObjects;
                var actionable = new List<GameObject>();

                foreach (var go in all)
                {
                    if (go == null) continue;
                    
                    // Simple filter for "interesting" objects
                    bool isButton = go.GetComponent<UnityEngine.UI.Button>() != null;
                    bool isToggle = go.GetComponent<UnityEngine.UI.Toggle>() != null;
                    bool isSlider = go.GetComponent<UnityEngine.UI.Slider>() != null;
                    bool hasCollider = go.GetComponent<Collider>() != null || go.GetComponent<Collider2D>() != null;

                    if (isButton || isToggle || isSlider || hasCollider)
                    {
                        // Check visibility/interactability
                        if (UI.PanelStateMonitor.IsPanelReady(go))
                        {
                            actionable.Add(go);
                        }
                    }
                }

                return actionable;
            }

            private static string PickRandomIntent(GameObject target)
            {
                if (target.GetComponent<UnityEngine.UI.Slider>() != null) return "drag";
                if (target.GetComponent<UnityEngine.UI.ScrollRect>() != null) return "drag";
                
                string[] intents = { "click", "move" };
                return intents[UnityEngine.Random.Range(0, intents.Length)];
            }
        }
    }
}
