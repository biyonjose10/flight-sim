using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// How long the whole journey took, from walking into the terminal to stopping on the runway.
    ///
    /// This is a plain static class rather than a MonoBehaviour on purpose. The four scenes each
    /// destroy everything in them when the next one loads, so an object holding the start time
    /// would not survive. A static value belongs to the program rather than to any scene, so it
    /// simply carries on. The alternative, DontDestroyOnLoad, keeps an object alive across scenes
    /// but then has to be stopped from piling up duplicates every time you go round again - which
    /// is a classic source of bugs, and this needs none of it.
    ///
    /// Time.time is the time since the game started and is NOT reset by loading a scene, which is
    /// exactly what is wanted here. (Time.timeSinceLevelLoad is the per-scene one.)
    /// </summary>
    public static class FlightClock
    {
        static float startedAt = -1f;
        static float stoppedAt = -1f;

        /// <summary>Starts the clock from zero. Called when scene 1 loads.</summary>
        public static void Restart()
        {
            startedAt = Time.time;
            stoppedAt = -1f;
            Debug.Log("[SCENE] Journey clock started");
        }

        /// <summary>Freezes the clock, so the arrival card does not keep counting.</summary>
        public static void Stop()
        {
            if (startedAt >= 0f && stoppedAt < 0f) stoppedAt = Time.time;
        }

        /// <summary>How long the journey has taken so far, in seconds.</summary>
        public static float Seconds
        {
            get
            {
                if (startedAt < 0f) return 0f;
                return (stoppedAt >= 0f ? stoppedAt : Time.time) - startedAt;
            }
        }

        /// <summary>The time as minutes and seconds, for the arrival card.</summary>
        public static string Formatted
        {
            get
            {
                int total = Mathf.Max(0, Mathf.RoundToInt(Seconds));
                return string.Format("{0}:{1:00}", total / 60, total % 60);
            }
        }
    }
}
