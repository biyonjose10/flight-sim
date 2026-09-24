using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlightSim.Build
{
    /// <summary>
    /// Takes still pictures of both scenes from fixed viewpoints and saves them to Snapshots/.
    /// You can check the framing, the lighting and the geometry without opening Unity and
    /// walking around.
    ///
    /// The stills can't show anything that moves or happens in play mode: no blinking, no head
    /// turns, no HUD.
    /// </summary>
    public static class SceneSnapshot
    {
        public const string OutputDir = "Snapshots";
        const int Width = 1280;
        const int Height = 720;

        struct View
        {
            public string scene, name;
            public Vector3 pos, euler;
            public View(string scene, string name, Vector3 pos, Vector3 euler)
            {
                this.scene = scene; this.name = name; this.pos = pos; this.euler = euler;
            }
        }

        static readonly View[] Views =
        {
            // Scene 1: where you start, the gate, the hall from the gate end, and outside.
            new View(FlightLayout.TerminalScene, "1a_spawn_window_view", new Vector3(0f, 1.65f, -5f),   new Vector3(4f, 0f, 0f)),
            new View(FlightLayout.TerminalScene, "1b_gate",              new Vector3(6f, 1.65f, 1f),    new Vector3(6f, 55f, 0f)),
            new View(FlightLayout.TerminalScene, "1c_hall",              new Vector3(19.2f, 3f, 1.5f),  new Vector3(12f, -108f, 0f)),
            new View(FlightLayout.TerminalScene, "1d_outside",           new Vector3(42f, 5f, 55f),     new Vector3(8f, -125f, 0f)),

            // Scene 2: where you start, down the aisle, the cockpit, and out of a window.
            new View(FlightLayout.CabinScene, "2a_spawn",   new Vector3(4.6f, 1.65f, -0.9f), new Vector3(4f, 68f, 0f)),
            new View(FlightLayout.CabinScene, "2b_aisle",   new Vector3(5.2f, 1.65f, 0f),    new Vector3(6f, -90f, 0f)),
            new View(FlightLayout.CabinScene, "2c_cockpit", new Vector3(7.4f, 1.6f, 0f),     new Vector3(10f, 90f, 0f)),
            new View(FlightLayout.CabinScene, "2d_window",  new Vector3(-0.6f, 1.7f, -0.1f), new Vector3(18f, -155f, 0f)),

            // The captain from where you actually stand when you walk into the cockpit - between
            // the two seats - rather than from in front of him, which is somewhere you can never
            // get to. Judging the face from an impossible camera is how you end up "fixing"
            // something that was already right.
            new View(FlightLayout.CabinScene, "2e_captain", new Vector3(8.2f, 1.65f, 0f), new Vector3(24f, -5f, 0f)),

            // Scene 3: the plane on the threshold from outside and from the pilot's seat, the
            // airfield from the air, and the coast you fly out towards.
            // Straight down the runway from behind, which is also the only angle the painted
            // runway number reads from - it is meant for a pilot rolling towards it, not for
            // someone standing beside it.
            new View(FlightLayout.TakeoffScene, "3a_on_the_runway",  new Vector3(-1335f, 26f, 0f),  new Vector3(11f, 90f, 0f)),
            // Your seat is now the one the captain is NOT in, so these sit 0.7 m off the
            // centreline. He is on the left, which is why 3e looks that way.
            new View(FlightLayout.TakeoffScene, "3b_pilot_seat",     new Vector3(-1164.2f, 4.08f, -0.7f), new Vector3(2f, 90f, 0f)),
            new View(FlightLayout.TakeoffScene, "3e_captain_beside", new Vector3(-1164.2f, 4.08f, -0.7f), new Vector3(4f, 36f, 0f)),
            new View(FlightLayout.TakeoffScene, "3c_airfield",       new Vector3(-1500f, 620f, -820f), new Vector3(26f, 47f, 0f)),
            new View(FlightLayout.TakeoffScene, "3d_coast",          new Vector3(2600f, 1050f, 0f),  new Vector3(11f, 90f, 0f)),

            // Scene 4: final approach, from the seat and from outside.
            new View(FlightLayout.LandingScene, "4a_approach_seat",  new Vector3(-3762.2f, 184.08f, -0.7f), new Vector3(4f, 90f, 0f)),
            new View(FlightLayout.LandingScene, "4b_approach_chase", new Vector3(-3860f, 212f, -85f), new Vector3(11f, 58f, 0f))
        };

        [MenuItem("Tools/Flight Sim/Snapshot Scenes", priority = 60)]
        public static void Snapshot()
        {
            Directory.CreateDirectory(OutputDir);

            foreach (var view in Views)
            {
                string path = SceneKit.ScenePath(view.scene);
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[Snapshot] Missing " + path + ". Run Build Scenes first.");
                    continue;
                }

                if (EditorSceneManager.GetActiveScene().path != path)
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                Capture(view);
            }

            Debug.Log("[Snapshot] Written to " + OutputDir + "/");
        }

        public static void BatchSnapshot()
        {
            int exit = 0;
            try { Snapshot(); }
            catch (System.Exception e) { Debug.LogError("[Snapshot] Failed: " + e); exit = 1; }
            EditorApplication.Exit(exit);
        }

        static void Capture(View view)
        {
            // A temporary camera that is never saved into the scene.
            var go = new GameObject("Snapshot Camera") { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetPositionAndRotation(view.pos, Quaternion.Euler(view.euler));
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            // Far enough to see the coast and the horizon hills in the flying scenes. The two
            // indoor scenes only needed a couple of kilometres, and leaving it there made the
            // sea, the hills and the distant countryside vanish into the skybox in scenes 3 and
            // 4 - which looked like they had never been built.
            cam.farClipPlane = 22000f;

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(OutputDir, view.name + ".png"), shot.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(shot);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);

            Debug.Log("[Snapshot] " + view.name);
        }
    }
}
