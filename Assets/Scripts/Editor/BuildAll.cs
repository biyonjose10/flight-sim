using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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
        public static readonly string[] Scenes =
        {
            FlightLayout.TerminalScene,
            FlightLayout.CabinScene,
            FlightLayout.TakeoffScene,
            FlightLayout.LandingScene
        };

        [MenuItem("Tools/Flight Sim/Build Scenes", priority = 0)]
        public static void Build()
        {
            var started = DateTime.Now;

            ApplyProjectSettings();
            Prim.ResetCache();

            // The sounds are assets the scenes point at, so they have to exist before the scenes
            // are built. Generating them is quick and skips anything already there.
            AudioBank.Generate();

            TerminalBuilder.Build();
            CabinBuilder.Build();
            FlightBuilder.Build();
            LandingBuilder.Build();

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

        [MenuItem("Tools/Flight Sim/Open Scene 3 (Take-off)", priority = 22)]
        public static void OpenScene3()
        {
            Open(FlightLayout.TakeoffScene);
        }

        [MenuItem("Tools/Flight Sim/Open Scene 4 (Landing)", priority = 23)]
        public static void OpenScene4()
        {
            Open(FlightLayout.LandingScene);
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

        // ------------------------------------------------------------------------- android

        // Where the Android tools live on this machine. Unity's Android module was installed
        // without its own copies, so it borrows the ones Android Studio already put here.
        const string AndroidSdk = @"C:\Users\biyon\AppData\Local\Android\Sdk";
        const string AndroidJdk = @"C:\Program Files\Android\Android Studio\jbr";

        [MenuItem("Tools/Flight Sim/Build Android APK", priority = 81)]
        public static void BuildAndroidApk()
        {
            AddScenesToBuildSettings();
            ApplyAndroidSettings();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            Directory.CreateDirectory("Build");

            var options = new BuildPlayerOptions
            {
                scenes = Scenes.Select(SceneKit.ScenePath).ToArray(),
                locationPathName = "Build/FlightSim.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log(string.Format("[Flight Sim] Android build {0} -> {1} ({2:0.0} MB, {3} errors)",
                                    summary.result, options.locationPathName,
                                    summary.totalSize / (1024f * 1024f), summary.totalErrors));
        }

        /// <summary>
        /// The Android settings that matter, set from code so they are written down rather than
        /// living invisibly in a settings window.
        ///
        /// **Mono, and 32-bit ARM.** Unity's other scripting backend, IL2CPP, is what you need for
        /// a 64-bit build, but it compiles through the Android NDK - and the NDK on this machine
        /// (r27) is not the version this Unity expects (r23b), so IL2CPP cannot run. Mono needs no
        /// NDK at all. The cost is that the APK is armeabi-v7a only: it runs on the great majority
        /// of phones, because 64-bit Android devices almost all still support 32-bit apps, but it
        /// will refuse to install on a 64-bit-only device. Installing NDK r23b would lift that.
        /// </summary>
        static void ApplyAndroidSettings()
        {
            EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetString("AndroidSdkRoot", AndroidSdk);
            EditorPrefs.SetBool("JdkUseEmbedded", false);
            EditorPrefs.SetString("JdkPath", AndroidJdk);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.biyonjose.flightsim");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // A flight sim is unplayable in portrait, and the on-screen buttons are laid out for a
            // wide screen.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.companyName = "Biyon";
            PlayerSettings.productName = "Flight Sim";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            EditorUserBuildSettings.buildAppBundle = false;   // a plain .apk you can sideload
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        }

        /// <summary>Command-line entry point for the APK. Exit code 1 if it fails.</summary>
        public static void BatchBuildAndroid()
        {
            int code = 0;
            try
            {
                BuildAndroidApk();
                if (!File.Exists("Build/FlightSim.apk")) code = 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[Flight Sim] Android build failed: " + e);
                code = 1;
            }

            if (Application.isBatchMode) EditorApplication.Exit(code);
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
