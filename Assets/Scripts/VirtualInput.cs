using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// What the on-screen buttons are currently asking for.
    ///
    /// On a phone there is no keyboard and no mouse, so something has to stand in for them. This
    /// is that stand-in: TouchControls writes into it when you press an on-screen button, and the
    /// ordinary game scripts read it ALONGSIDE the keyboard rather than instead of it. That is why
    /// the same build works with a keyboard on a PC and with your thumbs on a phone, and why none
    /// of the game scripts had to be rewritten - each one gained a single "or the screen button".
    ///
    /// It is a plain static class rather than a component because there is only ever one set of
    /// controls, and every scene destroys its own objects when the next one loads. A static value
    /// belongs to the program, so it simply carries on.
    ///
    /// The held values (Move, Strafe, Throttle) are rewritten every frame by TouchControls, so
    /// they clear themselves the moment you lift your thumb. The one-shot presses are taken with
    /// Consume(), which reads the press and clears it, so a single tap can never fire twice.
    /// </summary>
    public static class VirtualInput
    {
        /// <summary>True once on-screen controls are being drawn. Off on a PC unless forced on.</summary>
        public static bool Active;

        // Held: -1 to 1. Walking, these are the two movement axes; flying, pitch and roll.
        public static float Move;
        public static float Strafe;
        public static float Throttle;

        /// <summary>How far the look-drag moved this frame, in pixels.</summary>
        public static Vector2 LookDelta;

        static bool use, gear, view, land, restart;

        public static void PressUse() { use = true; }
        public static void PressGear() { gear = true; }
        public static void PressView() { view = true; }
        public static void PressLand() { land = true; }
        public static void PressRestart() { restart = true; }

        public static bool ConsumeUse() { return Take(ref use); }
        public static bool ConsumeGear() { return Take(ref gear); }
        public static bool ConsumeView() { return Take(ref view); }
        public static bool ConsumeLand() { return Take(ref land); }
        public static bool ConsumeRestart() { return Take(ref restart); }

        static bool Take(ref bool flag)
        {
            if (!flag) return false;
            flag = false;
            return true;
        }

        /// <summary>
        /// Drops any press nobody acted on. Without this, tapping "use" where there is nothing to
        /// use would leave the press sitting there, and it would go off later in a completely
        /// different place.
        /// </summary>
        public static void EndFrame()
        {
            use = gear = view = land = restart = false;
        }

        /// <summary>Clears everything. Called when a scene starts, so nothing carries across.</summary>
        public static void Reset()
        {
            Move = Strafe = Throttle = 0f;
            LookDelta = Vector2.zero;
            EndFrame();
        }
    }
}
