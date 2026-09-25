using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// The on-screen controls you play with on a phone: an analogue joystick on the left, buttons
    /// on the right, and drag-anywhere-else to look around.
    ///
    /// **It reads Input.touches itself rather than using GUI buttons, and that is the whole
    /// point.** Unity's built-in GUI controls only ever follow ONE finger on a phone, because they
    /// were written for a mouse and a mouse has one pointer. With GUI buttons you cannot hold the
    /// joystick and press the throttle at the same time, which makes a flight sim unplayable.
    /// Handling the touches directly means every finger is tracked separately: joystick, throttle
    /// and looking around can all happen at once.
    ///
    /// The joystick is analogue, not four arrows. How far you push it decides how hard you pitch
    /// or roll, so you can make a small correction instead of only ever having "hard left".
    ///
    /// Its centre appears wherever your thumb lands rather than being painted in a fixed spot,
    /// which is what makes it comfortable: you never have to look down to find it.
    ///
    /// Drawing still happens in OnGUI, but only DrawTexture and Label - nothing interactive - so
    /// the drawing code cannot steal a touch from the logic above.
    /// </summary>
    [DisallowMultipleComponent]
    public class TouchControls : MonoBehaviour
    {
        public enum Mode { Walking, Flying, Watching }

        enum Act { None, Use, Gear, View, Land, Restart, ThrottleUp, ThrottleDown }

        struct Button
        {
            public Rect rect;
            public string label;
            public Act act;
            public bool hold;     // true keeps firing while held, false fires once on touch
        }

        [Header("What this scene needs")]
        public Mode mode = Mode.Walking;

        [Tooltip("Show the controls on a PC too, so the layout can be checked without a phone.")]
        public bool alwaysShow = false;

        [Header("Feel")]
        [Tooltip("How far the view turns per pixel of drag.")]
        public float dragSensitivity = 0.11f;

        [Tooltip("How far the joystick has to be pushed for full deflection, as a fraction of screen height.")]
        [Range(0.05f, 0.3f)] public float stickRadius = 0.13f;

        [Range(0f, 1f)] public float controlAlpha = 0.3f;

        readonly Button[] buttons = new Button[6];
        int buttonCount;

        int stickFinger = -1, lookFinger = -1;
        Vector2 stickOrigin, stickNow;

        Texture2D dot, box;
        GUIStyle labelStyle;

        /// <summary>True when the on-screen controls should be drawn and read at all.</summary>
        public bool Showing { get { return alwaysShow || Application.isMobilePlatform; } }

        void Awake()
        {
            box = Solid(Color.white);
            dot = Disc();
            VirtualInput.Reset();
        }

        void OnDestroy()
        {
            if (box != null) Destroy(box);
            if (dot != null) Destroy(dot);
            VirtualInput.Reset();
        }

        void Update()
        {
            VirtualInput.Active = Showing;
            if (!Showing) return;

            LayOutButtons();

            VirtualInput.LookDelta = Vector2.zero;
            VirtualInput.Throttle = 0f;

            ReadTouches();
            ApplyStick();
        }

        // There is deliberately no LateUpdate clearing presses here. Wiping them at the end of the
        // frame is what lost taps whose reader happened to run before this script - VirtualInput
        // expires them by frame number instead.

        // --------------------------------------------------------------------------- layout

        /// <summary>
        /// Where the buttons sit, worked out from the screen height so they stay thumb-sized on
        /// any phone. Which buttons exist depends on the scene - the landing flies itself, so it
        /// needs almost nothing.
        /// </summary>
        void LayOutButtons()
        {
            float h = Screen.height;
            float w = Screen.width;

            float b = h * 0.17f;
            float edge = h * 0.05f;
            float gap = h * 0.02f;

            buttonCount = 0;

            switch (mode)
            {
                case Mode.Walking:
                    Add(new Rect(w - edge - b, h - edge - b, b, b), "USE", Act.Use, false);
                    break;

                case Mode.Flying:
                    Add(new Rect(w - edge - b, h - edge - b, b, b), "THR\n+", Act.ThrottleUp, true);
                    Add(new Rect(w - edge - b * 2f - gap, h - edge - b, b, b), "THR\n−", Act.ThrottleDown, true);
                    Add(new Rect(w - edge - b, h - edge - b * 2f - gap, b, b), "VIEW", Act.View, false);
                    Add(new Rect(w - edge - b * 2f - gap, h - edge - b * 2f - gap, b, b), "GEAR", Act.Gear, false);

                    if (Aircraft.Instance != null && Aircraft.Instance.ReadyToLand)
                        Add(new Rect(w - edge - b * 2f - gap, h - edge - b * 3f - gap * 2f,
                                     b * 2f + gap, b * 0.7f), "LAND", Act.Land, false);
                    break;

                default:
                    Add(new Rect(w - edge - b, h - edge - b, b, b), "VIEW", Act.View, false);

                    if (EndCard.Instance != null && EndCard.Instance.Showing)
                        Add(new Rect(w * 0.5f - h * 0.2f, h * 0.72f, h * 0.4f, h * 0.11f),
                            "FLY AGAIN", Act.Restart, false);
                    break;
            }
        }

        void Add(Rect r, string label, Act act, bool hold)
        {
            buttons[buttonCount].rect = r;
            buttons[buttonCount].label = label;
            buttons[buttonCount].act = act;
            buttons[buttonCount].hold = hold;
            buttonCount++;
        }

        // --------------------------------------------------------------------------- touches

        /// <summary>
        /// Sorts every finger on the screen into one of three jobs: working a button, working the
        /// joystick, or looking around. Each finger is followed by its own id from the moment it
        /// lands until it lifts, so they never swap jobs halfway through a gesture.
        /// </summary>
        void ReadTouches()
        {
            int count = Input.touchCount;

            // In the editor there are no touches, so the mouse stands in as a single finger. That
            // is only so the layout can be checked on a PC; a phone never takes this path.
            if (count == 0 && alwaysShow && Input.GetMouseButton(0))
            {
                HandleMousePretendingToBeATouch();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Touch t = Input.GetTouch(i);

                // Touches are measured from the bottom left, GUI rectangles from the top left.
                Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y);

                if (t.phase == TouchPhase.Began)
                {
                    int hit = ButtonUnder(p);

                    if (hit >= 0)
                    {
                        if (!buttons[hit].hold) Fire(buttons[hit].act);
                        continue;
                    }

                    if (stickFinger < 0 && InStickZone(p))
                    {
                        stickFinger = t.fingerId;
                        stickOrigin = p;
                        stickNow = p;
                        continue;
                    }

                    if (lookFinger < 0) lookFinger = t.fingerId;
                    continue;
                }

                if (t.fingerId == stickFinger)
                {
                    stickNow = p;
                }
                else if (t.fingerId == lookFinger)
                {
                    if (t.phase == TouchPhase.Moved)
                        VirtualInput.LookDelta += t.deltaPosition * dragSensitivity;
                }
                else
                {
                    // A finger resting on a hold button keeps it firing.
                    int hit = ButtonUnder(p);
                    if (hit >= 0 && buttons[hit].hold) Fire(buttons[hit].act);
                }

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    if (t.fingerId == stickFinger) stickFinger = -1;
                    if (t.fingerId == lookFinger) lookFinger = -1;
                }
            }

            if (count == 0)
            {
                stickFinger = -1;
                lookFinger = -1;
            }
        }

        void HandleMousePretendingToBeATouch()
        {
            Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (Input.GetMouseButtonDown(0))
            {
                int hit = ButtonUnder(p);
                if (hit >= 0)
                {
                    if (!buttons[hit].hold) Fire(buttons[hit].act);
                    return;
                }

                if (InStickZone(p)) { stickFinger = 9999; stickOrigin = p; }
            }

            if (stickFinger == 9999) stickNow = p;

            int held = ButtonUnder(p);
            if (held >= 0 && buttons[held].hold) Fire(buttons[held].act);
        }

        /// <summary>
        /// The joystick lives in the lower left. Everywhere else is free for looking.
        ///
        /// Scene 4 has no joystick at all - it flies itself - so there the whole screen looks
        /// around, and a thumb in the corner is not quietly swallowed by a stick that does nothing.
        /// </summary>
        bool InStickZone(Vector2 guiPoint)
        {
            if (mode == Mode.Watching) return false;

            return guiPoint.x < Screen.width * 0.45f && guiPoint.y > Screen.height * 0.3f;
        }

        int ButtonUnder(Vector2 guiPoint)
        {
            for (int i = 0; i < buttonCount; i++)
                if (buttons[i].rect.Contains(guiPoint)) return i;

            return -1;
        }

        void Fire(Act act)
        {
            switch (act)
            {
                case Act.Use: VirtualInput.PressUse(); break;
                case Act.Gear: VirtualInput.PressGear(); break;
                case Act.View: VirtualInput.PressView(); break;
                case Act.Land: VirtualInput.PressLand(); break;
                case Act.Restart: VirtualInput.PressRestart(); break;
                case Act.ThrottleUp: VirtualInput.Throttle = 1f; break;
                case Act.ThrottleDown: VirtualInput.Throttle = -1f; break;
            }
        }

        /// <summary>
        /// Turns how far the joystick is pushed into the two movement numbers.
        ///
        /// The push is divided by the maximum reach and then clamped, so half a push really is
        /// half the input. That is the difference between a joystick and four arrow buttons, and
        /// it is what lets you hold a gentle bank instead of sawing left and right.
        /// </summary>
        void ApplyStick()
        {
            if (stickFinger < 0)
            {
                VirtualInput.Move = 0f;
                VirtualInput.Strafe = 0f;
                return;
            }

            float reach = Screen.height * stickRadius;
            Vector2 push = stickNow - stickOrigin;

            if (push.magnitude > reach) push = push.normalized * reach;

            VirtualInput.Strafe = Mathf.Clamp(push.x / reach, -1f, 1f);

            // Screen y grows downwards, so pushing the stick UP is a NEGATIVE y. Forward, and
            // nose-up, both want a positive number, hence the minus.
            VirtualInput.Move = Mathf.Clamp(-push.y / reach, -1f, 1f);
        }

        // --------------------------------------------------------------------------- drawing

        void OnGUI()
        {
            if (!Showing) return;

            EnsureStyle();

            for (int i = 0; i < buttonCount; i++)
            {
                Tint(new Color(0f, 0f, 0f, controlAlpha));
                GUI.DrawTexture(buttons[i].rect, box);
                Tint(Color.white);
                GUI.Label(buttons[i].rect, buttons[i].label, labelStyle);
            }

            if (mode == Mode.Watching) return;

            DrawStick();
        }

        /// <summary>
        /// The joystick, drawn only once a thumb is on it. Drawing it all the time would mean
        /// painting a ring in a spot the thumb may never go near.
        /// </summary>
        void DrawStick()
        {
            if (stickFinger < 0) return;

            float reach = Screen.height * stickRadius;

            Tint(new Color(1f, 1f, 1f, controlAlpha * 0.6f));
            GUI.DrawTexture(new Rect(stickOrigin.x - reach, stickOrigin.y - reach, reach * 2f, reach * 2f), dot);

            Vector2 push = stickNow - stickOrigin;
            if (push.magnitude > reach) push = push.normalized * reach;

            float knob = reach * 0.45f;
            Vector2 c = stickOrigin + push;

            Tint(new Color(1f, 1f, 1f, 0.75f));
            GUI.DrawTexture(new Rect(c.x - knob, c.y - knob, knob * 2f, knob * 2f), dot);
            Tint(Color.white);
        }

        static void Tint(Color c)
        {
            GUI.color = c;
        }

        void EnsureStyle()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.wordWrap = true;
                labelStyle.normal.textColor = Color.white;
            }

            labelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(Screen.height * 0.024f));
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>
        /// A filled circle, drawn into a texture once at startup. GUI.DrawTexture can only draw
        /// rectangles, so a round joystick needs a round picture to draw.
        /// </summary>
        static Texture2D Disc()
        {
            const int size = 96;
            var t = new Texture2D(size, size, TextureFormat.ARGB32, false);
            float r = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));

                    // Fade over the last pixel or so, otherwise the edge is a staircase.
                    float a = Mathf.Clamp01(r - d);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            t.Apply();
            return t;
        }
    }
}
