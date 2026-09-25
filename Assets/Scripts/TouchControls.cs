using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// The on-screen buttons you play with on a phone.
    ///
    /// It draws a pad on the left for movement and a column of buttons on the right for everything
    /// else, and it writes what you are pressing into VirtualInput. The rest of the game reads
    /// VirtualInput next to the keyboard, so nothing else had to change to make the game playable
    /// with thumbs.
    ///
    /// Looking around is a DRAG rather than a button. Anywhere on the screen that is not a button
    /// works, the way it does in most phone games - buttons for the things that are on or off, and
    /// dragging for the thing that needs to be smooth.
    ///
    /// Which buttons appear depends on the scene: walking needs a direction pad and "use", flying
    /// needs pitch, roll, throttle, gear and the camera, and the landing needs almost nothing
    /// because it flies itself.
    ///
    /// Everything is drawn with OnGUI, the same as the rest of the display in this project, so the
    /// whole thing is one file you can read top to bottom instead of a tree of Canvas objects.
    /// Sizes are fractions of the screen height, so the buttons stay thumb-sized on any phone.
    /// </summary>
    [DisallowMultipleComponent]
    public class TouchControls : MonoBehaviour
    {
        public enum Mode { Walking, Flying, Watching }

        [Header("What this scene needs")]
        public Mode mode = Mode.Walking;

        [Tooltip("Show the buttons on a PC too. Handy for checking the layout without a phone.")]
        public bool alwaysShow = false;

        [Header("Feel")]
        [Tooltip("How far the view turns per pixel of drag.")]
        public float dragSensitivity = 0.12f;
        [Range(0f, 1f)] public float buttonAlpha = 0.28f;

        // Worked out once per frame from the screen size, and used both for drawing the buttons
        // and for deciding whether a touch counts as a drag or as a button press.
        Rect padUp, padDown, padLeft, padRight;
        Rect actionA, actionB, actionC, actionD;
        string labelA, labelB, labelC, labelD;

        GUIStyle buttonStyle;
        Texture2D white;
        int dragFinger = -1;

        /// <summary>True when the on-screen controls should be drawn at all.</summary>
        public bool Showing { get { return alwaysShow || Application.isMobilePlatform; } }

        void Awake()
        {
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();

            VirtualInput.Reset();
        }

        void OnDestroy()
        {
            if (white != null) Destroy(white);
            VirtualInput.Reset();
        }

        void Update()
        {
            VirtualInput.Active = Showing;
            if (!Showing) return;

            Layout();
            ReadDrag();
        }

        /// <summary>Anything nobody acted on is dropped at the end of the frame.</summary>
        void LateUpdate()
        {
            VirtualInput.EndFrame();
        }

        /// <summary>
        /// Where every button sits. Worked out from the screen height so the buttons are the same
        /// physical size whatever the phone, and kept in one place because the drag code has to
        /// agree with the drawing code about what counts as a button.
        /// </summary>
        void Layout()
        {
            float h = Screen.height;
            float w = Screen.width;

            float b = h * 0.16f;          // button size - about a thumb
            float edge = h * 0.04f;       // margin from the screen edge
            float gap = h * 0.012f;

            // Left: a four-way pad.
            float padCx = edge + b * 1.5f + gap;
            float padCy = h - edge - b * 1.5f - gap;

            padUp = new Rect(padCx - b * 0.5f, padCy - b * 1.5f - gap, b, b);
            padDown = new Rect(padCx - b * 0.5f, padCy + b * 0.5f + gap, b, b);
            padLeft = new Rect(padCx - b * 1.5f - gap, padCy - b * 0.5f, b, b);
            padRight = new Rect(padCx + b * 0.5f + gap, padCy - b * 0.5f, b, b);

            // Right: a column of the buttons this scene actually uses.
            float rx = w - edge - b;
            float ry = h - edge - b;

            actionA = new Rect(rx, ry, b, b);
            actionB = new Rect(rx - b - gap, ry, b, b);
            actionC = new Rect(rx, ry - b - gap, b, b);
            actionD = new Rect(rx - b - gap, ry - b - gap, b, b);

            switch (mode)
            {
                case Mode.Walking:
                    labelA = "USE"; labelB = null; labelC = null; labelD = null;
                    break;

                case Mode.Flying:
                    labelA = "THR +"; labelB = "THR -"; labelC = "VIEW"; labelD = "GEAR";
                    break;

                default:
                    labelA = null; labelB = null; labelC = "VIEW"; labelD = null;
                    break;
            }
        }

        /// <summary>
        /// Turns a finger dragged across the screen into a look. Any touch that did not start on a
        /// button counts, and only one finger at a time drives the view - otherwise resting a
        /// second thumb on the screen would fight with the first.
        /// </summary>
        void ReadDrag()
        {
            VirtualInput.LookDelta = Vector2.zero;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                // Touch positions come from the bottom left, GUI rectangles from the top left.
                Vector2 gui = new Vector2(t.position.x, Screen.height - t.position.y);

                if (t.phase == TouchPhase.Began && dragFinger < 0 && !OnAButton(gui))
                    dragFinger = t.fingerId;

                if (t.fingerId != dragFinger) continue;

                if (t.phase == TouchPhase.Moved)
                    VirtualInput.LookDelta = t.deltaPosition * dragSensitivity;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    dragFinger = -1;
            }

            if (Input.touchCount == 0) dragFinger = -1;
        }

        bool OnAButton(Vector2 guiPoint)
        {
            return padUp.Contains(guiPoint) || padDown.Contains(guiPoint) ||
                   padLeft.Contains(guiPoint) || padRight.Contains(guiPoint) ||
                   (labelA != null && actionA.Contains(guiPoint)) ||
                   (labelB != null && actionB.Contains(guiPoint)) ||
                   (labelC != null && actionC.Contains(guiPoint)) ||
                   (labelD != null && actionD.Contains(guiPoint));
        }

        void OnGUI()
        {
            if (!Showing) return;

            EnsureStyle();

            // The held axes are rebuilt from scratch every frame, so letting go clears them.
            float move = 0f, strafe = 0f, throttle = 0f;

            if (mode != Mode.Watching)
            {
                // Flying, "up" raises the nose. A real control column works the other way round -
                // you pull BACK to climb - but on a phone an up arrow that makes you go down is
                // simply confusing, and Aircraft treats a positive pitch input as nose-up anyway.
                if (Held(padUp, mode == Mode.Flying ? "NOSE\nUP" : "FWD")) move += 1f;
                if (Held(padDown, mode == Mode.Flying ? "NOSE\nDOWN" : "BACK")) move -= 1f;
                if (Held(padLeft, mode == Mode.Flying ? "ROLL\nL" : "LEFT")) strafe -= 1f;
                if (Held(padRight, mode == Mode.Flying ? "ROLL\nR" : "RIGHT")) strafe += 1f;
            }

            switch (mode)
            {
                case Mode.Walking:
                    if (Tapped(actionA, labelA)) VirtualInput.PressUse();
                    break;

                case Mode.Flying:
                    if (Held(actionA, labelA)) throttle += 1f;
                    if (Held(actionB, labelB)) throttle -= 1f;
                    if (Tapped(actionC, labelC)) VirtualInput.PressView();
                    if (Tapped(actionD, labelD)) VirtualInput.PressGear();

                    // The landing button only appears when the plane is actually ready for it, so
                    // the screen is not cluttered with something that would do nothing.
                    if (Aircraft.Instance != null && Aircraft.Instance.ReadyToLand)
                    {
                        var landRect = new Rect(actionA.x - actionA.width * 0.5f,
                                                actionC.y - actionA.height - Screen.height * 0.012f,
                                                actionA.width * 1.5f, actionA.height * 0.75f);
                        if (Tapped(landRect, "LAND")) VirtualInput.PressLand();
                    }
                    break;

                default:
                    if (Tapped(actionC, labelC)) VirtualInput.PressView();

                    if (EndCard.Instance != null && EndCard.Instance.Showing)
                    {
                        var again = new Rect(Screen.width * 0.5f - Screen.height * 0.16f,
                                             Screen.height * 0.72f,
                                             Screen.height * 0.32f, Screen.height * 0.1f);
                        if (Tapped(again, "FLY AGAIN")) VirtualInput.PressRestart();
                    }
                    break;
            }

            VirtualInput.Move = move;
            VirtualInput.Strafe = strafe;
            VirtualInput.Throttle = throttle;
        }

        /// <summary>A button that does something for as long as you hold it down.</summary>
        bool Held(Rect r, string label)
        {
            if (label == null) return false;

            Background(r);
            return GUI.RepeatButton(r, label, buttonStyle);
        }

        /// <summary>A button that does something once, when you tap it.</summary>
        bool Tapped(Rect r, string label)
        {
            if (label == null) return false;

            Background(r);
            return GUI.Button(r, label, buttonStyle);
        }

        void Background(Rect r)
        {
            GUI.color = new Color(0f, 0f, 0f, buttonAlpha);
            GUI.DrawTexture(r, white);
            GUI.color = Color.white;
        }

        void EnsureStyle()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.wordWrap = true;
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.active.textColor = new Color(0.7f, 0.9f, 1f);

                // Transparent so the dark box drawn underneath shows through.
                buttonStyle.normal.background = null;
                buttonStyle.active.background = null;
            }

            buttonStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(Screen.height * 0.022f));
        }
    }
}
