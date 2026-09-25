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

        // A press remembers WHICH FRAME it happened on rather than just that it happened.
        //
        // This matters, and getting it wrong made the LAND button do nothing at all. Unity does
        // not promise any particular order between two components' Update methods. TouchControls
        // records the press in its Update; FlightHUD reads it in its Update. If the HUD happened
        // to run first, it saw nothing - and if the press was then wiped at the end of the frame,
        // the tap was lost for good. Which of the two ran first was pure luck, so the button
        // looked broken rather than flaky.
        //
        // Keeping the frame number means a press is readable both later in the same frame and
        // early in the next one, so either order works, and it still expires straight afterwards
        // so one tap can never fire twice.
        static int useFrame = -10, gearFrame = -10, viewFrame = -10, landFrame = -10, restartFrame = -10;

        public static void PressUse() { useFrame = Time.frameCount; }
        public static void PressGear() { gearFrame = Time.frameCount; }
        public static void PressView() { viewFrame = Time.frameCount; }
        public static void PressLand() { landFrame = Time.frameCount; }
        public static void PressRestart() { restartFrame = Time.frameCount; }

        public static bool ConsumeUse() { return Take(ref useFrame); }
        public static bool ConsumeGear() { return Take(ref gearFrame); }
        public static bool ConsumeView() { return Take(ref viewFrame); }
        public static bool ConsumeLand() { return Take(ref landFrame); }
        public static bool ConsumeRestart() { return Take(ref restartFrame); }

        static bool Take(ref int frame)
        {
            if (frame < 0 || Time.frameCount - frame > 1) return false;

            frame = -1;
            return true;
        }

        /// <summary>Clears everything. Called when a scene starts, so nothing carries across.</summary>
        public static void Reset()
        {
            Move = Strafe = Throttle = 0f;
            LookDelta = Vector2.zero;
            useFrame = gearFrame = viewFrame = landFrame = restartFrame = -10;
        }
    }
}
