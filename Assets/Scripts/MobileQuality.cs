using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Turns the expensive graphics settings down on a phone.
    ///
    /// The desktop settings are deliberately generous: eight per-pixel lights, soft real-time
    /// shadows out to ninety metres, and a world of a few thousand objects. A PC shrugs at that.
    /// A phone does not - it has a fraction of the power and it is pushing a lot of pixels - and
    /// the result is a game that runs so slowly it feels broken rather than slow.
    ///
    /// None of this changes what is in the scene. It changes how hard the renderer is asked to
    /// work on each frame, which is where almost all of the cost is:
    ///
    ///   * **per-pixel lights** - each one makes the renderer draw affected objects again. Eight
    ///     is generous on a desktop and ruinous on a phone.
    ///   * **real-time shadows** - the most expensive single setting here, because every shadow
    ///     casting light re-renders the scene from the light's point of view.
    ///   * **anti-aliasing** - smooths edges by rendering bigger and shrinking, so it costs real
    ///     fill rate on a screen that is already dense enough to hide the jaggies.
    ///
    /// It only ever runs on a mobile build, so the PC version is untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public class MobileQuality : MonoBehaviour
    {
        [Tooltip("Apply the cut-down settings on a PC too, to see what a phone gets.")]
        public bool forceOnDesktop = false;

        void Awake()
        {
            if (!Application.isMobilePlatform && !forceOnDesktop) return;

            QualitySettings.pixelLightCount = 2;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.antiAliasing = 0;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;

            // Ask for a steady 60 rather than letting it swing about, and take vsync out of the
            // way so the frame rate request is the one that counts.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // The screen can go to sleep mid-flight otherwise, because holding a joystick does not
            // count as touching the screen for the sleep timer on every device.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log("[QUALITY] Phone settings: shadows off, 2 pixel lights, no anti-aliasing, 60 fps target");
        }
    }
}
