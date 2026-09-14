using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FlightSim.Build
{
    /// <summary>
    /// The parts every scene needs, whatever is in it: a sun, a sky, the player, the HUD and
    /// the fader. Both scene builders call these, so the two scenes are set up the same way.
    /// </summary>
    public static class SceneKit
    {
        public static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static string ScenePath(string sceneName)
        {
            return FlightLayout.SceneFolder + "/" + sceneName + ".unity";
        }

        public static void Save(Scene scene, string sceneName)
        {
            Directory.CreateDirectory(FlightLayout.SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath(sceneName));
            Debug.Log("[Flight Sim] Saved " + ScenePath(sceneName));
        }

        /// <summary>
        /// The sky, the sun and the ambient light. Ambient is "trilight" (separate sky, horizon
        /// and ground colours) rather than sampled from the skybox, because skybox ambient only
        /// becomes correct after a lighting bake, and these scenes are never baked.
        /// </summary>
        public static Light Environment(Vector3 sunEuler, float sunIntensity, float fogStart, float fogEnd)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.Euler(sunEuler);

            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;

            RenderSettings.skybox = SkyMaterial();
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.54f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.23f);

            // A little haze, so the far end of the runway fades into the sky instead of stopping.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.9f);
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            return sun;
        }

        /// <summary>Unity's procedural sky: blue above, a bright horizon and a sun disc. Updated in place.</summary>
        static Material SkyMaterial()
        {
            const string path = Prim.MaterialDir + "/Sky.mat";
            Directory.CreateDirectory(Prim.MaterialDir);

            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Skybox/Procedural"));

            m.SetFloat("_SunSize", 0.035f);
            m.SetFloat("_AtmosphereThickness", 0.9f);
            m.SetColor("_SkyTint", new Color(0.5f, 0.6f, 0.78f));
            m.SetColor("_GroundColor", new Color(0.45f, 0.47f, 0.45f));
            m.SetFloat("_Exposure", 1.25f);

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = Prim.Empty(parent, name, pos);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>
        /// The player: a CharacterController body with a camera at eye height. The body turns
        /// left and right; the camera, as a child, tilts up and down.
        /// </summary>
        public static PlayerController Player(Vector3 feet, float yaw)
        {
            var body = new GameObject("Player");
            body.transform.position = feet + Vector3.up * 0.05f;   // just above the floor, so it settles rather than sticks
            body.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var cc = body.AddComponent<CharacterController>();
            cc.height = FlightLayout.PlayerHeight;
            cc.radius = FlightLayout.PlayerRadius;
            cc.center = new Vector3(0f, FlightLayout.PlayerHeight * 0.5f, 0f);
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.02f;

            // The default (0.001 m) silently throws away any move smaller than that. At a high
            // frame rate each frame's step is tiny, so the player barely moves. Unity recommends 0.
            cc.minMoveDistance = 0f;

            var camGo = Prim.Empty(body.transform, "Player Camera", new Vector3(0f, FlightLayout.EyeHeight, 0f));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2500f;
            camGo.AddComponent<AudioListener>();

            var pc = body.AddComponent<PlayerController>();
            pc.playerCamera = camGo.transform;
            return pc;
        }

        /// <summary>One object holding the HUD and the fader for this scene.</summary>
        public static void GameSystems(string title, string subtitle)
        {
            var go = new GameObject("Game Systems");

            var hud = go.AddComponent<PromptHUD>();
            hud.title = title;
            hud.subtitle = subtitle;

            go.AddComponent<SceneFader>();
        }

        /// <summary>A walk-up-and-press-E zone that loads a scene.</summary>
        public static Interactable Zone(string name, Vector3 centre, Vector3 size, string prompt,
                                        string sceneToLoad, string notBuiltMessage)
        {
            var go = new GameObject(name);
            go.transform.position = centre;

            var it = go.AddComponent<Interactable>();
            it.zoneSize = size;
            it.prompt = prompt;
            it.sceneToLoad = sceneToLoad;
            it.notBuiltMessage = notBuiltMessage;
            return it;
        }

        /// <summary>A flashing light: a glowing sphere, plus a real point light if a range is given.</summary>
        public static BlinkingLight Blinker(Transform parent, string name, Vector3 pos, float diameter,
                                            Material bulbMat, Color lightColor, float lightRange,
                                            float onSeconds, float offSeconds, float offset)
        {
            var bulb = Prim.NoShadow(Prim.Sphere(parent, name, pos, Vector3.one * diameter, bulbMat));
            var blink = bulb.AddComponent<BlinkingLight>();
            blink.bulb = bulb.GetComponent<Renderer>();
            blink.onSeconds = onSeconds;
            blink.offSeconds = offSeconds;
            blink.offset = offset;

            if (lightRange > 0f)
            {
                // The light is a sibling, not a child, so hiding the bulb's renderer doesn't matter to it.
                blink.glow = PointLight(parent, name + " Light", pos, lightColor, 3f, lightRange);
            }

            return blink;
        }
    }
}
