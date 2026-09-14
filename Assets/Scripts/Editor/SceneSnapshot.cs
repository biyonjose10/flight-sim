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
            new View(FlightLayout.CabinScene, "2d_window",  new Vector3(-0.6f, 1.7f, -0.1f), new Vector3(18f, -155f, 0f))
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
            cam.farClipPlane = 2500f;

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
