using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlightSim.Build
{
    /// <summary>
    /// Plays the game by itself, start to finish, and fails loudly if anything goes wrong.
    ///
    /// It presses Play on scene 1 and walks the player (with PlayerController's autopilot) to
    /// the gate, which also proves the walls and seats leave a way through. Then it boards. In
    /// the cabin it walks down the aisle and into the cockpit and presses take-off. In scene 3 it
    /// flies the take-off with Aircraft's autopilot, climbs away, looks at the plane from the
    /// outside camera, and begins the approach. In scene 4 it watches the landing all the way to
    /// the arrival card.
    ///
    /// It FAILS if there is any runtime error, if the player gets stuck, if a zone can't be
    /// reached, if a scene never loads, if the plane never leaves the ground or crashes, or if the
    /// landing never finishes. Screenshots land in Playtest/.
    ///
    /// Domain reload is switched off for the run, otherwise entering play mode would wipe the
    /// static variables that remember how far the test has got.
    /// </summary>
    public static class Playtest
    {
        const string OutputDir = "Playtest";

        // Game time. The walk takes about a minute, the climb another forty seconds and the
        // scripted landing nearly a minute, so the whole run is a few minutes of game time.
        const float GiveUpSeconds = 420f;
        const float RealTimeLimitSeconds = 2700f;    // wall-clock safety net
        const float StuckSeconds = 25f;

        /// <summary>How high the autopilot climbs before it starts the approach.</summary>
        const float ClimbToAltitude = FlightLayout.Flight.LandingPromptAltitude + 60f;

        enum Phase
        {
            WaitForTerminal, WalkTerminal,
            WaitForCabin, WalkCabin,
            WaitForFlight, FlyTakeoff,
            WaitForLanding, WatchLanding
        }

        static Phase phase;
        static int waypoint;
        static bool walking;
        static float startTime, stepStart, phaseStart;
        static bool started, finished, batch;

        // Screenshots that should only be taken the first time their moment arrives.
        static bool shotAirborne, shotChase, shotTouchdown;

        static readonly List<string> errors = new List<string>();
        static readonly HashSet<string> scenesSeen = new HashSet<string>();

        [MenuItem("Tools/Flight Sim/Playtest", priority = 61)]
        public static void Run() { Begin(false); }

        /// <summary>Command-line entry point. Quits Unity with exit code 0 (pass) or 1 (fail).</summary>
        public static void BatchPlaytest() { Begin(true); }

        static void Begin(bool isBatch)
        {
            batch = isBatch;
            Directory.CreateDirectory(OutputDir);

            phase = Phase.WaitForTerminal;
            waypoint = 0;
            walking = false;
            started = false;
            finished = false;
            shotAirborne = false;
            shotChase = false;
            shotTouchdown = false;
            errors.Clear();
            scenesSeen.Clear();

            string scene1 = SceneKit.ScenePath(FlightLayout.TerminalScene);
            if (!File.Exists(scene1))
            {
                Debug.LogError("[Playtest] Missing " + scene1 + ". Run Build Scenes first.");
                if (batch) EditorApplication.Exit(1);
                return;
            }

            BuildAll.AddScenesToBuildSettings();
            EditorSceneManager.OpenScene(scene1);

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(type + ": " + message);
        }

        static void Tick()
        {
            if (finished || !EditorApplication.isPlaying) return;

            // GAME time, not wall-clock time. A headless editor can run only a few frames per real
            // second, so a player walking at normal speed looks "stuck" on a wall-clock timer.
            // The real-time limit below only exists to stop a genuinely frozen run going forever.
            float now = Time.time;
            if (!started)
            {
                started = true;
                startTime = Time.realtimeSinceStartup;
                phaseStart = now;
                Debug.Log("[Playtest] Play mode entered");
            }

            string active = SceneManager.GetActiveScene().name;
            scenesSeen.Add(active);

            if (now > GiveUpSeconds || Time.realtimeSinceStartup - startTime > RealTimeLimitSeconds)
            {
                Fail("Timed out in phase " + phase + " (game time " + now.ToString("0.0") + "s)");
                return;
            }

            switch (phase)
            {
                case Phase.WaitForTerminal:
                    // Wait for the fade-in to finish so the first screenshot isn't black.
                    if (active == FlightLayout.TerminalScene && Player() != null && now - phaseStart > 1.5f)
                    {
                        Capture("1_start");
                        Next(Phase.WalkTerminal, now);
                    }
                    break;

                case Phase.WalkTerminal:
                    if (Walk(FlightLayout.Terminal.PlaytestRoute, "1", now))
                    {
                        Capture("1_at_gate");
                        if (UseZone("boarding")) Next(Phase.WaitForCabin, now);
                    }
                    break;

                case Phase.WaitForCabin:
                    if (active == FlightLayout.CabinScene && Player() != null && now - phaseStart > 2.5f)
                    {
                        Capture("2_start");
                        Next(Phase.WalkCabin, now);
                    }
                    else if (now - phaseStart > 15f)
                    {
                        Fail("Pressed board, but the cabin scene never loaded");
                    }
                    break;

                case Phase.WalkCabin:
                    if (Walk(FlightLayout.Cabin.PlaytestRoute, "2", now))
                    {
                        Capture("2_in_cockpit");
                        if (UseZone("take-off")) Next(Phase.WaitForFlight, now);
                    }
                    break;

                case Phase.WaitForFlight:
                    if (active == FlightLayout.TakeoffScene && Plane() != null && now - phaseStart > 2.5f)
                    {
                        Capture("3_on_the_runway");

                        // Full power, and aim for a steady climb once the wheels are up.
                        Plane().Autopilot(1f, 12f);
                        Next(Phase.FlyTakeoff, now);
                    }
                    else if (now - phaseStart > 15f)
                    {
                        Fail("Pressed take-off, but the flight scene never loaded");
                    }
                    break;

                case Phase.FlyTakeoff:
                    FlyTakeoff(now);
                    break;

                case Phase.WaitForLanding:
                    if (active == FlightLayout.LandingScene && LandingSequence.Instance != null && now - phaseStart > 2.5f)
                    {
                        Capture("4_on_approach");
                        Next(Phase.WatchLanding, now);
                    }
                    else if (now - phaseStart > 15f)
                    {
                        Fail("Began the approach, but the landing scene never loaded");
                    }
                    break;

                case Phase.WatchLanding:
                    WatchLanding(now);
                    break;
            }
        }

        /// <summary>
        /// Flies the take-off and the climb. It checks the things that actually prove the flight
        /// model works: the plane leaves the ground, the wheels come up, and it gains height.
        /// </summary>
        static void FlyTakeoff(float now)
        {
            var plane = Plane();
            if (plane == null)
            {
                Fail("The aeroplane disappeared during the take-off");
                return;
            }

            if (plane.Crashed)
            {
                Fail("The plane crashed during the automated take-off");
                return;
            }

            // A few snapshots along the way, each taken once.
            if (!plane.OnGround && !shotAirborne)
            {
                shotAirborne = true;
                Capture("3_airborne");
            }

            if (plane.Altitude > 150f && !shotChase)
            {
                shotChase = true;

                // Prove the outside camera works, then go back to the pilot's seat.
                if (FlightCamera.Instance != null)
                {
                    FlightCamera.Instance.Toggle();
                    Capture("3_chase_view");
                    FlightCamera.Instance.Toggle();
                }
            }

            if (plane.Altitude > ClimbToAltitude)
            {
                Capture("3_climbing_away");

                if (!plane.ReadyToLand)
                {
                    Fail("Climbed to " + Mathf.RoundToInt(plane.Altitude) +
                         " m but the landing prompt never became available (gear down: " + plane.GearDown + ")");
                    return;
                }

                Debug.Log("[Playtest] Beginning the approach");
                SceneFader.GoTo(FlightLayout.LandingScene, null);
                Next(Phase.WaitForLanding, now);
                return;
            }

            // The take-off roll and climb have a generous but finite budget.
            if (now - phaseStart > 120f)
            {
                Fail("Ran out of time climbing: altitude " + Mathf.RoundToInt(plane.Altitude) +
                     " m, speed " + Mathf.RoundToInt(plane.Knots) + " kt, on the ground: " + plane.OnGround);
            }
        }

        /// <summary>Watches the scripted landing through to the arrival card.</summary>
        static void WatchLanding(float now)
        {
            var sequence = LandingSequence.Instance;
            if (sequence == null)
            {
                Fail("The landing sequence disappeared");
                return;
            }

            if (sequence.Current == LandingSequence.Stage.RollOut && !shotTouchdown)
            {
                shotTouchdown = true;
                Capture("4_touchdown");
            }

            if (EndCard.Instance != null && EndCard.Instance.Showing)
            {
                Capture("4_arrived");
                Debug.Log("[Playtest] The landing finished and the arrival card appeared");
                Finish();
                return;
            }

            float budget = FlightLayout.Landing.DescentSeconds + FlightLayout.Landing.FlareSeconds +
                           FlightLayout.Landing.RollOutSeconds + FlightLayout.Landing.EndCardDelay + 30f;

            if (now - phaseStart > budget)
                Fail("The landing never reached the arrival card - it stopped at stage " + sequence.Current);
        }

        static void Next(Phase p, float now)
        {
            phase = p;
            phaseStart = now;
            waypoint = 0;
            walking = false;
        }

        static PlayerController Player()
        {
            return Object.FindFirstObjectByType<PlayerController>();
        }

        static Aircraft Plane()
        {
            return Object.FindFirstObjectByType<Aircraft>();
        }

        /// <summary>Walks the route one point at a time. Returns true once the last point is reached.</summary>
        static bool Walk(Vector3[] route, string label, float now)
        {
            if (waypoint >= route.Length) return true;

            var player = Player();
            if (player == null) return false;

            if (!walking)
            {
                player.WalkTo(route[waypoint]);
                walking = true;
                stepStart = now;
            }
            else if (!player.IsWalkingToTarget)
            {
                Capture(label + "_waypoint" + waypoint);
                waypoint++;
                walking = false;
            }
            else if (now - stepStart > StuckSeconds)
            {
                Fail("Player got stuck walking to " + route[waypoint] + " - it is at " + player.transform.position +
                     ". Something solid is in the way.");
            }

            return false;
        }

        static bool UseZone(string what)
        {
            foreach (var zone in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                if (!zone.PlayerInside) continue;

                zone.Use();
                return true;
            }

            Fail("Reached the end of the route but the player is not inside the " + what + " zone");
            return false;
        }

        static void Capture(string label)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            shot.Apply();
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;

            File.WriteAllBytes(Path.Combine(OutputDir, label + ".png"), shot.EncodeToPNG());
            Object.DestroyImmediate(shot);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log("[Playtest] Captured " + label + " in '" + SceneManager.GetActiveScene().name + "'");
        }

        static void Fail(string reason)
        {
            errors.Add(reason);
            Finish();
        }

        static void Finish()
        {
            if (finished) return;
            finished = true;

            foreach (string scene in BuildAll.Scenes)
                if (!scenesSeen.Contains(scene))
                    errors.Add("Scene never became active: '" + scene + "'");

            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;

            if (errors.Count > 0)
            {
                Debug.Log("[Playtest] FAILED with " + errors.Count + " problem(s):");
                foreach (string e in errors) Debug.Log("[Playtest]   " + e);
            }
            else
            {
                Debug.Log("[Playtest] PASSED - boarded, walked the cabin, took off, climbed away, " +
                          "landed and arrived, no errors.");
            }

            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.Exit(errors.Count > 0 ? 1 : 0);
        }
    }
}
