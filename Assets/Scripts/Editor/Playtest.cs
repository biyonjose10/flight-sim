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
    /// the cabin it walks down the aisle and into the cockpit and presses take-off, and checks
    /// that the "scene 3 isn't built yet" message appears rather than an error.
    ///
    /// It FAILS if there is any runtime error, if the player gets stuck, if a zone can't be
    /// reached, or if a scene never loads. Screenshots land in Playtest/.
    ///
    /// Domain reload is switched off for the run, otherwise entering play mode would wipe the
    /// static variables that remember how far the test has got.
    /// </summary>
    public static class Playtest
    {
        const string OutputDir = "Playtest";
        const float GiveUpSeconds = 180f;            // game time
        const float RealTimeLimitSeconds = 1500f;    // wall-clock safety net
        const float StuckSeconds = 25f;

        enum Phase { WaitForTerminal, WalkTerminal, WaitForCabin, WalkCabin, WaitForMessage }

        static Phase phase;
        static int waypoint;
        static bool walking;
        static float startTime, stepStart, phaseStart;
        static bool started, finished, batch;
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
                        if (UseZone("take-off")) Next(Phase.WaitForMessage, now);
                    }
                    break;

                case Phase.WaitForMessage:
                    var hud = PromptHUD.Instance;
                    if (hud != null && hud.CurrentMessage != null && hud.CurrentMessage.Contains("scene 3"))
                    {
                        Debug.Log("[Playtest] Take-off showed: \"" + hud.CurrentMessage + "\"");
                        Finish();
                    }
                    else if (now - phaseStart > 5f)
                    {
                        Fail("Pressed take-off, but the 'scene 3 not built yet' message never appeared");
                    }
                    break;
            }
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
                Debug.Log("[Playtest] PASSED - boarded, walked the cabin, reached the cockpit, no errors.");
            }

            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.Exit(errors.Count > 0 ? 1 : 0);
        }
    }
}
