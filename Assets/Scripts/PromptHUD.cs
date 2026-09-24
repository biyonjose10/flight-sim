using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Everything drawn flat on the screen: the dot in the middle, the "Press E to ..." prompt,
    /// short messages, the scene title and the controls hint.
    ///
    /// It uses OnGUI, Unity's code-only way of drawing to the screen. A Canvas would need
    /// several objects wired together in the editor, while this is one script you can read top
    /// to bottom. OnGUI runs every frame and just redraws whatever should be visible right now.
    /// </summary>
    [DisallowMultipleComponent]
    public class PromptHUD : MonoBehaviour
    {
        /// <summary>The HUD in the current scene, so other scripts can reach it without a reference.</summary>
        public static PromptHUD Instance { get; private set; }

        [Header("Scene title (shown for the first few seconds)")]
        public string title = "";
        public string subtitle = "";
        public float titleSeconds = 4f;

        [Header("Controls hint (top left)")]
        [Tooltip("The keys this scene uses. The walking scenes and the flying scenes need different ones.")]
        public string controlsHint = "WASD walk    Mouse look    E use    Esc free cursor";
        public float controlsHintSeconds = 12f;

        Interactable promptOwner;   // whoever is currently showing a prompt
        string promptText;
        string message;
        float messageUntil;

        GUIStyle promptStyle, messageStyle, titleStyle, subtitleStyle, hintStyle;
        Texture2D white;

        /// <summary>The prompt on screen right now, or null.</summary>
        public string CurrentPrompt { get { return promptOwner != null ? promptText : null; } }

        /// <summary>The message on screen right now, or null.</summary>
        public string CurrentMessage { get { return Time.time < messageUntil ? message : null; } }

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

        public void ShowPrompt(Interactable owner, string text)
        {
            promptOwner = owner;
            promptText = text;
        }

        /// <summary>Only the interactable that showed the prompt can hide it.</summary>
        public void HidePrompt(Interactable owner)
        {
            if (promptOwner == owner) promptOwner = null;
        }

        public void ShowMessage(string text, float seconds)
        {
            message = text;
            messageUntil = Time.time + seconds;
            Debug.Log("[HUD] " + text);
        }

        void OnGUI()
        {
            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;
            float since = Time.timeSinceLevelLoad;

            // Crosshair dot, so you can tell where you're looking.
            DrawBox(new Rect(w / 2f - 2f, h / 2f - 2f, 4f, 4f), new Color(1f, 1f, 1f, 0.8f));

            // Scene title, fading out over its last second.
            if (!string.IsNullOrEmpty(title) && since < titleSeconds)
            {
                float alpha = Mathf.Clamp01(titleSeconds - since);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(0f, h * 0.16f, w, h * 0.08f), title, titleStyle);
                GUI.Label(new Rect(0f, h * 0.24f, w, h * 0.05f), subtitle, subtitleStyle);
                GUI.color = Color.white;
            }

            if (since < controlsHintSeconds && !string.IsNullOrEmpty(controlsHint))
            {
                var hint = new Rect(16f, 16f, h * 0.78f, h * 0.045f);
                DrawBox(hint, new Color(0f, 0f, 0f, 0.45f));
                GUI.Label(hint, controlsHint, hintStyle);
            }

            if (CurrentPrompt != null)
            {
                var box = new Rect(w / 2f - h * 0.34f, h * 0.72f, h * 0.68f, h * 0.075f);
                DrawBox(box, new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(box, CurrentPrompt, promptStyle);
            }

            if (CurrentMessage != null)
            {
                var box = new Rect(w / 2f - h * 0.5f, h * 0.36f, h * 1.0f, h * 0.1f);
                DrawBox(box, new Color(0.05f, 0.1f, 0.2f, 0.8f));
                GUI.Label(box, CurrentMessage, messageStyle);
            }
        }

        void DrawBox(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = Color.white;
        }

        // GUI styles can only be created inside OnGUI. Font sizes follow the window height, so
        // the text reads the same in a small editor Game view and full screen.
        void EnsureStyles()
        {
            int unit = Mathf.Max(10, Mathf.RoundToInt(Screen.height * 0.028f));

            if (promptStyle == null)
            {
                promptStyle = MakeStyle(FontStyle.Bold);
                messageStyle = MakeStyle(FontStyle.Bold);
                titleStyle = MakeStyle(FontStyle.Bold);
                subtitleStyle = MakeStyle(FontStyle.Normal);
                hintStyle = MakeStyle(FontStyle.Normal);
                hintStyle.alignment = TextAnchor.MiddleLeft;
                hintStyle.padding = new RectOffset(10, 10, 0, 0);
            }

            promptStyle.fontSize = unit;
            messageStyle.fontSize = unit;
            titleStyle.fontSize = unit * 2;
            subtitleStyle.fontSize = unit;
            hintStyle.fontSize = Mathf.RoundToInt(unit * 0.7f);
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
