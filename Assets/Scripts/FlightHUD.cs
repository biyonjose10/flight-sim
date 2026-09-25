using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlightSim
{
    /// <summary>
    /// The flying half of the on-screen display: speed, height, compass heading, the throttle
    /// bar, whether the wheels are down, and which camera you are looking through.
    ///
    /// PromptHUD already draws the scene title, the walking prompts and short messages, and it
    /// carries on doing that here. This script only adds the things that exist when you are
    /// flying, which keeps each file short enough to read in one go.
    ///
    /// It also owns the two moments the flight can end:
    ///
    ///   * **the landing prompt** - once the wheels are up and you are above the height in
    ///     FlightLayout.Flight, "Press L to begin landing" appears and L loads scene 4,
    ///   * **the crash message** - it says what went wrong, waits a few seconds, and reloads the
    ///     scene so you can try again. There is deliberately no way to get stuck.
    ///
    /// Like PromptHUD this uses OnGUI, Unity's code-only way of drawing on the screen, so the
    /// whole display is one file you can read top to bottom instead of a tree of Canvas objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlightHUD : MonoBehaviour
    {
        /// <summary>The flight HUD in the current scene, so Aircraft can report a crash to it.</summary>
        public static FlightHUD Instance { get; private set; }

        [Header("What to show")]
        [Tooltip("Turn off in scene 4, where the landing flies itself and L would make no sense.")]
        public bool offerLanding = true;

        [Tooltip("Shown in the corner so you always know which scene you are in.")]
        public string flightName = "FS 204";

        Aircraft plane;
        string crashReason;
        float crashAt = -1f;
        bool restarting;

        GUIStyle readoutStyle, labelStyle, promptStyle, crashStyle;
        Texture2D white;

        /// <summary>The crash message on screen right now, or null. The playtest reads this.</summary>
        public string CrashReason { get { return crashReason; } }

        void Awake()
        {
            Instance = this;

            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (white != null) Destroy(white);
        }

        void Start()
        {
            plane = Aircraft.Instance;

            if (plane == null)
                Debug.LogWarning("[HUD] FlightHUD found no aeroplane in the scene");
        }

        void Update()
        {
            if (plane == null) plane = Aircraft.Instance;
            if (plane == null) return;

            if (crashAt > 0f && !restarting && Time.time - crashAt > FlightLayout.Flight.CrashRestartSeconds)
            {
                restarting = true;
                string here = SceneManager.GetActiveScene().name;
                Debug.Log("[SCENE] Restarting '" + here + "' after a crash");
                SceneFader.GoTo(here, null);
                return;
            }

            bool landPressed = Input.GetKeyDown(FlightLayout.Flight.LandingKey) || VirtualInput.ConsumeLand();

            if (offerLanding && plane.ReadyToLand && landPressed)
            {
                Debug.Log("[FLIGHT] Beginning the approach");
                SceneFader.GoTo(FlightLayout.LandingScene, "The landing is scene 4 - it hasn't been built yet.");
            }
        }

        /// <summary>Called by Aircraft when the flight ends in the ground.</summary>
        public void ShowCrash(string why)
        {
            crashReason = why;
            crashAt = Time.time;
            Debug.Log("[HUD] " + why);
        }

        void OnGUI()
        {
            if (plane == null) return;

            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            DrawReadouts(w, h);
            DrawThrottle(w, h);

            if (crashReason != null)
            {
                var box = new Rect(w * 0.5f - h * 0.5f, h * 0.38f, h, h * 0.16f);
                DrawBox(box, new Color(0.35f, 0.04f, 0.04f, 0.85f));
                GUI.Label(box, crashReason + "\nRestarting...", crashStyle);
            }
            else if (offerLanding && plane.ReadyToLand)
            {
                var box = new Rect(w * 0.5f - h * 0.34f, h * 0.78f, h * 0.68f, h * 0.07f);
                DrawBox(box, new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(box, "Press L to begin landing", promptStyle);
            }
        }

        /// <summary>Speed, height and heading along the bottom, where they do not block the view.</summary>
        void DrawReadouts(float w, float h)
        {
            float boxW = h * 0.16f;
            float boxH = h * 0.09f;
            float y = h - boxH - h * 0.03f;
            float gap = h * 0.012f;
            float x = w * 0.5f - (boxW * 3f + gap * 2f) * 0.5f;

            Readout(new Rect(x, y, boxW, boxH), "SPEED", Mathf.Round(plane.Knots) + " kt");
            Readout(new Rect(x + boxW + gap, y, boxW, boxH), "ALTITUDE", Mathf.Round(plane.Altitude) + " m");
            Readout(new Rect(x + (boxW + gap) * 2f, y, boxW, boxH), "HEADING",
                    Mathf.Round(plane.Heading).ToString("000") + "°");

            // Top right: the things that are either on or off.
            var corner = new Rect(w - h * 0.3f - 16f, 16f, h * 0.3f, h * 0.05f);
            DrawBox(corner, new Color(0f, 0f, 0f, 0.45f));

            string view = FlightCamera.Instance != null ? FlightCamera.Instance.ViewName : "";
            string gear = plane.GearDown ? "GEAR DOWN" : "GEAR UP";
            GUI.Label(corner, flightName + "    " + gear + "    " + view, labelStyle);
        }

        void Readout(Rect r, string caption, string value)
        {
            DrawBox(r, new Color(0f, 0f, 0f, 0.55f));

            var top = new Rect(r.x, r.y + r.height * 0.08f, r.width, r.height * 0.3f);
            var bottom = new Rect(r.x, r.y + r.height * 0.38f, r.width, r.height * 0.55f);

            GUI.Label(top, caption, labelStyle);
            GUI.Label(bottom, value, readoutStyle);
        }

        /// <summary>A bar rather than a number, because a bar is readable at a glance.</summary>
        void DrawThrottle(float w, float h)
        {
            float barW = h * 0.028f;
            float barH = h * 0.26f;
            var frame = new Rect(w - barW - h * 0.05f, h * 0.5f - barH * 0.5f, barW, barH);

            DrawBox(frame, new Color(0f, 0f, 0f, 0.5f));

            float filled = barH * Mathf.Clamp01(plane.throttle);
            var fill = new Rect(frame.x, frame.yMax - filled, barW, filled);
            DrawBox(fill, new Color(0.3f, 0.85f, 0.4f, 0.85f));

            var caption = new Rect(frame.x - barW, frame.yMax + h * 0.008f, barW * 3f, h * 0.035f);
            GUI.Label(caption, Mathf.RoundToInt(plane.throttle * 100f) + "%", labelStyle);
        }

        void DrawBox(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = Color.white;
        }

        // GUI styles can only be built inside OnGUI. Sizes follow the window height, so the text
        // reads the same in a small editor Game view and full screen.
        void EnsureStyles()
        {
            int unit = Mathf.Max(10, Mathf.RoundToInt(Screen.height * 0.028f));

            if (readoutStyle == null)
            {
                readoutStyle = MakeStyle(FontStyle.Bold);
                labelStyle = MakeStyle(FontStyle.Normal);
                promptStyle = MakeStyle(FontStyle.Bold);
                crashStyle = MakeStyle(FontStyle.Bold);
                labelStyle.normal.textColor = new Color(0.75f, 0.8f, 0.85f);
            }

            readoutStyle.fontSize = unit;
            labelStyle.fontSize = Mathf.RoundToInt(unit * 0.62f);
            promptStyle.fontSize = unit;
            crashStyle.fontSize = Mathf.RoundToInt(unit * 1.1f);
        }

        static GUIStyle MakeStyle(FontStyle fontStyle)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.alignment = TextAnchor.MiddleCenter;
            s.fontStyle = fontStyle;
            s.wordWrap = true;
            s.normal.textColor = Color.white;
            return s;
        }
    }
}
