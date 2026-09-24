using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// The card that appears once the plane has stopped: where you flew from and to, how long it
    /// took, and an offer to do the whole thing again.
    ///
    /// Pressing R reloads scene 1, so the game can be shown twice in a row without quitting,
    /// reopening Unity or pressing Play again. That matters more than it sounds: the whole point
    /// of this project is being able to demonstrate it.
    ///
    /// The flight time is measured with Time.time in this scene only, so it is the length of the
    /// landing rather than of the entire journey. FlightClock keeps the real total across all four
    /// scenes, and this card uses that when it is available.
    /// </summary>
    [DisallowMultipleComponent]
    public class EndCard : MonoBehaviour
    {
        /// <summary>The card in the current scene, so the playtest can check it appeared.</summary>
        public static EndCard Instance { get; private set; }

        [Header("What it says")]
        public string headline = "FLIGHT FS 204 - ARRIVED";
        public string route = "Gate 7  →  Destination";

        [Tooltip("How long the card takes to fade up, in seconds.")]
        public float fadeSeconds = 1.2f;

        float showAt = -1f;
        bool restarting;

        /// <summary>True once the card is on screen. The playtest waits for this.</summary>
        public bool Showing { get { return showAt > 0f && Time.time >= showAt; } }

        GUIStyle headlineStyle, routeStyle, timeStyle, hintStyle;
        Texture2D white;

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

        /// <summary>Called by the landing sequence once the plane has stopped.</summary>
        public void Show(float afterSeconds)
        {
            showAt = Time.time + afterSeconds;
            Debug.Log("[LANDING] Arrival card in " + afterSeconds.ToString("0.0") + "s");
        }

        void Update()
        {
            if (!Showing || restarting) return;

            if (Input.GetKeyDown(FlightLayout.Landing.RestartKey))
            {
                restarting = true;
                Debug.Log("[SCENE] Flying again from the beginning");
                SceneFader.GoTo(FlightLayout.TerminalScene, null);
            }
        }

        void OnGUI()
        {
            if (!Showing) return;

            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;

            // Fade the card up rather than snapping it on, so it does not feel like an error box.
            float alpha = Mathf.Clamp01((Time.time - showAt) / Mathf.Max(0.01f, fadeSeconds));

            var panel = new Rect(w * 0.5f - h * 0.45f, h * 0.28f, h * 0.9f, h * 0.42f);
            DrawBox(panel, new Color(0.03f, 0.06f, 0.12f, 0.82f * alpha));

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(panel.x, panel.y + h * 0.04f, panel.width, h * 0.09f), headline, headlineStyle);
            GUI.Label(new Rect(panel.x, panel.y + h * 0.14f, panel.width, h * 0.05f), route, routeStyle);
            GUI.Label(new Rect(panel.x, panel.y + h * 0.21f, panel.width, h * 0.06f),
                      "flight time   " + FlightClock.Formatted, timeStyle);
            GUI.Label(new Rect(panel.x, panel.y + h * 0.32f, panel.width, h * 0.05f),
                      "Press R to fly again        Esc to free the cursor", hintStyle);
            GUI.color = Color.white;
        }

        void DrawBox(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = Color.white;
        }

        void EnsureStyles()
        {
            int unit = Mathf.Max(10, Mathf.RoundToInt(Screen.height * 0.028f));

            if (headlineStyle == null)
            {
                headlineStyle = MakeStyle(FontStyle.Bold);
                routeStyle = MakeStyle(FontStyle.Normal);
                timeStyle = MakeStyle(FontStyle.Bold);
                hintStyle = MakeStyle(FontStyle.Normal);
                hintStyle.normal.textColor = new Color(0.7f, 0.76f, 0.85f);
                routeStyle.normal.textColor = new Color(0.75f, 0.82f, 0.9f);
            }

            headlineStyle.fontSize = Mathf.RoundToInt(unit * 1.6f);
            routeStyle.fontSize = unit;
            timeStyle.fontSize = Mathf.RoundToInt(unit * 1.1f);
            hintStyle.fontSize = Mathf.RoundToInt(unit * 0.75f);
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
