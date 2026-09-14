using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlightSim.Build
{
    /// <summary>
    /// The Tools > Flight Sim menu, and the project settings the scenes rely on.
    ///
    /// "Build Scenes" is the important one. It runs both scene builders, which create
    /// Assets/Scenes/01_Terminal.unity and 02_Cabin.unity from scratch, and lists them in the
    /// build settings so one can load the other.
    /// </summary>
    public static class BuildAll
    {
        public static readonly string[] Scenes = { FlightLayout.TerminalScene, FlightLayout.CabinScene };

        [MenuItem("Tools/Flight Sim/Build Scenes", priority = 0)]
        public static void Build()
        {
            var started = DateTime.Now;

            ApplyProjectSettings();
            Prim.ResetCache();

            TerminalBuilder.Build();
            CabinBuilder.Build();

            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(SceneKit.ScenePath(FlightLayout.TerminalScene));

            Debug.Log(string.Format("[Flight Sim] Built {0} scenes in {1:0.0}s. Scene 1 is open - press Play.",
                                    Scenes.Length, (DateTime.Now - started).TotalSeconds));
        }

        [MenuItem("Tools/Flight Sim/Open Scene 1 (Terminal)", priority = 20)]
        public static void OpenScene1()
        {
            Open(FlightLayout.TerminalScene);
        }

        [MenuItem("Tools/Flight Sim/Open Scene 2 (Cabin)", priority = 21)]
        public static void OpenScene2()
        {
            Open(FlightLayout.CabinScene);
        }

        static void Open(string sceneName)
        {
            string path = SceneKit.ScenePath(sceneName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Flight Sim] " + path + " doesn't exist yet. Run Tools > Flight Sim > Build Scenes.");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        /// <summary>A scene can only load another scene that is listed in the build settings.</summary>
        [MenuItem("Tools/Flight Sim/Add Scenes To Build Settings", priority = 40)]
        public static void AddScenesToBuildSettings()
        {
            EditorBuildSettings.scenes = Scenes
                .Select(s => new EditorBuildSettingsScene(SceneKit.ScenePath(s), true))
                .ToArray();

            Debug.Log("[Flight Sim] Build settings now list: " + string.Join(", ", Scenes));
        }

        [MenuItem("Tools/Flight Sim/Build Windows Player", priority = 80)]
        public static void BuildPlayer()
        {
            AddScenesToBuildSettings();

            var options = new BuildPlayerOptions
            {
                scenes = Scenes.Select(SceneKit.ScenePath).ToArray(),
                locationPathName = "Build/FlightSim/FlightSim.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[Flight Sim] Player build " + report.summary.result + " -> " + options.locationPathName);
        }

        /// <summary>
        /// Settings the look depends on. Set here rather than by hand, because getting them
        /// wrong doesn't cause an error. It just looks worse, which is much harder to notice.
        /// </summary>
        public static void ApplyProjectSettings()
        {
            // Lighting maths is done in linear space. In gamma, lit surfaces go washed out.
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "Flight Sim";

            // The hall has eight ceiling lights. Unity's default of four per-pixel lights would
            // leave half of them lighting things crudely.
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 8);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 90f);
        }

        /// <summary>Command-line entry point: build the scenes and quit, exit code 1 on failure.</summary>
        public static void BatchBuild()
        {
            int code = 0;
            try
            {
                Build();
            }
            catch (Exception e)
            {
                Debug.LogError("[Flight Sim] Build failed: " + e);
                code = 1;
            }

            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
