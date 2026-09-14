using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Flashes a light on and off: the red beacon on top of the plane and the cockpit's
    /// warning lights.
    ///
    /// It needs no timer variable. Mathf.Repeat wraps the game clock into one on-plus-off cycle,
    /// and the light is on for the first part of each cycle.
    /// </summary>
    public class BlinkingLight : MonoBehaviour
    {
        [Tooltip("The glowing mesh to show and hide.")]
        public Renderer bulb;
        [Tooltip("Optional real light that flashes with it.")]
        public Light glow;

        public float onSeconds = 0.12f;
        public float offSeconds = 1.0f;
        [Tooltip("Shifts this light's timing, so a row of lights doesn't flash in perfect unison.")]
        public float offset = 0f;

        void Update()
        {
            float cycle = onSeconds + offSeconds;
            bool on = Mathf.Repeat(Time.time + offset, cycle) < onSeconds;

            if (bulb != null) bulb.enabled = on;
            if (glow != null) glow.enabled = on;
        }
    }
}
