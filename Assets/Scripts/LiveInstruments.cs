using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Makes the cockpit instruments tell the truth.
    ///
    /// The cockpit itself is only painted shapes: a blue box over a brown box is an artificial
    /// horizon, a green bar is an engine gauge, a piece of 3D text is a speed readout. This script
    /// is the part that makes them mean something. Every frame it asks the aeroplane four
    /// questions - which way is the nose pointing, how fast are we going, how high are we, and are
    /// the wheels down - and moves the shapes to match.
    ///
    /// The same cockpit is built into three scenes, and in scene 2 (the cabin, parked at the gate)
    /// there is no aeroplane at all. So the first thing this script does each frame is look for
    /// one, and if there isn't one it does nothing whatsoever - no movement and no log lines.
    /// That single check is what lets one cockpit serve all three scenes.
    ///
    /// The builder (CockpitParts) fills in every field below while it is placing the shapes, so
    /// nothing here ever has to go searching the scene for anything.
    /// </summary>
    [DisallowMultipleComponent]
    public class LiveInstruments : MonoBehaviour
    {
        // -------------------------------------------------------------------- fixed numbers

        /// <summary>
        /// How far the artificial horizon slides across its screen for each degree of pitch, in
        /// metres. The aeroplane can only reach 25 degrees nose up and 20 degrees nose down (see
        /// FlightLayout.Flight), so at 2 mm per degree the horizon travels at most 5 cm - about a
        /// third of the way to the edge of the screen. Big enough to read at a glance, small
        /// enough that the picture never runs off the glass.
        /// </summary>
        public const float MetresPerPitchDegree = 0.002f;

        /// <summary>
        /// The shortest an engine bar is allowed to get. A box scaled to zero disappears
        /// completely, and a gauge that vanishes at idle looks broken rather than idle.
        /// </summary>
        public const float MinGaugeHeight = 0.02f;

        /// <summary>Altitude is kept in metres and shown in feet, the way real instruments do it.</summary>
        public const float FeetPerMetre = 3.28084f;

        /// <summary>Climbing or descending faster than this, in metres per second, is worth saying.</summary>
        public const float TrendThreshold = 1f;

        /// <summary>The gear has to be this far through its travel before the light goes green.</summary>
        public const float GearLockedBlend = 0.99f;

        // ---------------------------------------------------------- what the builder gives us

        [Tooltip("The artificial horizons, one per pilot. Roll turns them, pitch slides them.")]
        public Transform[] horizonPivots;

        [Tooltip("The engine power bars. They grow upwards from their base as the throttle opens.")]
        public Transform[] engineGauges;

        [Tooltip("The airspeed readout, in knots.")]
        public TextMesh speedText;

        [Tooltip("The altitude readout, in feet above the ground.")]
        public TextMesh altitudeText;

        [Tooltip("The compass heading readout, in degrees.")]
        public TextMesh headingText;

        [Tooltip("The landing-gear lights: green when the wheels are down and locked.")]
        public Renderer[] gearLights;

        [Tooltip("Green: the wheels are down and locked.")]
        public Material gearDownMaterial;

        [Tooltip("Amber: the wheels are up, or still on their way.")]
        public Material gearUpMaterial;

        // --------------------------------------------------------------- remembered at start

        // Where each moving piece sits when the aeroplane is straight and level, with the engines
        // at full power. Every frame is worked out from these plus the aeroplane's numbers, never
        // from where the piece happened to be last frame, so a small error can never build up.
        Vector3[] horizonHome;
        float[] gaugeBaseY;
        float[] gaugeFullHeight;

        bool saidHello;
        bool saidGoodbye;

        /// <summary>
        /// Measures the panel as the builder left it. The engine bars are built at full size, so
        /// the height they were built at is what "full power" looks like, and the bottom edge of
        /// that bar is the line they grow up from.
        /// </summary>
        void Start()
        {
            if (horizonPivots != null)
            {
                horizonHome = new Vector3[horizonPivots.Length];
                for (int i = 0; i < horizonPivots.Length; i++)
                {
                    if (horizonPivots[i] != null) horizonHome[i] = horizonPivots[i].localPosition;
                }
            }

            if (engineGauges != null)
            {
                gaugeBaseY = new float[engineGauges.Length];
                gaugeFullHeight = new float[engineGauges.Length];

                for (int i = 0; i < engineGauges.Length; i++)
                {
                    if (engineGauges[i] == null) continue;

                    float full = engineGauges[i].localScale.y;
                    gaugeFullHeight[i] = full;
                    gaugeBaseY[i] = engineGauges[i].localPosition.y - full * 0.5f;
                }
            }
        }

        /// <summary>
        /// LateUpdate, not Update, so the aeroplane has already flown this frame by the time the
        /// panel is drawn. The instruments then show where it is now, not where it was a frame ago.
        /// </summary>
        void LateUpdate()
        {
            var plane = Aircraft.Instance;

            // Scene 2 has a cockpit but no aeroplane. Nothing to show, and nothing to say either.
            if (plane == null) return;

            if (!saidHello)
            {
                saidHello = true;
                Debug.Log("[PANEL] Instruments live - following the aeroplane.");
            }

            // After a crash the aeroplane stops updating itself, so the panel stops with it.
            if (plane.Crashed)
            {
                if (!saidGoodbye)
                {
                    saidGoodbye = true;
                    Debug.Log("[PANEL] The flight ended - the instruments are frozen where they stopped.");
                }
                return;
            }

            ShowAttitude(plane.Pitch, plane.Roll);
            ShowEnginePower(plane.throttle);
            ShowNumbers(plane);
            ShowGear(plane.GearDown, plane.GearBlend);
        }

        // ------------------------------------------------------------------------- the panel

        /// <summary>
        /// The artificial horizon: the one instrument a pilot really flies by.
        ///
        /// The sky box, the ground box and the white line between them are all children of a
        /// single pivot sitting in the middle of the screen, so moving that one pivot moves the
        /// whole picture.
        ///
        /// ROLL turns the pivot. The screens are mounted on the front face of the panel and look
        /// at the pilots along the panel's local -X, so the axis sticking straight out of the
        /// glass is the panel's local X - and turning about the axis that points at your eye is
        /// what reads as a tilt. Positive roll in Aircraft means the right wing is down; drop your
        /// right shoulder and a level horizon appears to swing anti-clockwise by that same angle,
        /// which is exactly what Quaternion.Euler(roll, 0, 0) produces on this screen.
        ///
        /// PITCH slides the pivot. Nose up means the real horizon drops out of the windscreen, so
        /// the drawn one drops too - hence the minus sign. The slide runs along the pivot's OWN up
        /// direction rather than straight down the screen, so in a banked turn the picture still
        /// moves at right angles to the horizon line, the way a real attitude indicator behaves.
        /// </summary>
        void ShowAttitude(float pitch, float roll)
        {
            if (horizonPivots == null || horizonHome == null) return;

            Quaternion tilt = Quaternion.Euler(roll, 0f, 0f);
            Vector3 slide = tilt * Vector3.up * (-pitch * MetresPerPitchDegree);

            for (int i = 0; i < horizonPivots.Length; i++)
            {
                if (horizonPivots[i] == null) continue;

                horizonPivots[i].localRotation = tilt;
                horizonPivots[i].localPosition = horizonHome[i] + slide;
            }
        }

        /// <summary>
        /// The engine bars. A bar has to look as though it grows out of its base, but a box in
        /// Unity grows in both directions at once - so the height changes and the centre moves up
        /// by half of the new height, which pins the bottom edge in place.
        /// </summary>
        void ShowEnginePower(float throttle)
        {
            if (engineGauges == null || gaugeBaseY == null || gaugeFullHeight == null) return;

            float power = Mathf.Clamp01(throttle);

            for (int i = 0; i < engineGauges.Length; i++)
            {
                if (engineGauges[i] == null) continue;

                float height = Mathf.Lerp(MinGaugeHeight, gaugeFullHeight[i], power);

                Vector3 scale = engineGauges[i].localScale;
                scale.y = height;
                engineGauges[i].localScale = scale;

                Vector3 pos = engineGauges[i].localPosition;
                pos.y = gaugeBaseY[i] + height * 0.5f;
                engineGauges[i].localPosition = pos;
            }
        }

        /// <summary>The three numbers a real crew reads out loud: speed, height and heading.</summary>
        void ShowNumbers(Aircraft plane)
        {
            if (speedText != null)
            {
                speedText.text = string.Format("IAS {0:0} KT", plane.Knots);
            }

            if (altitudeText != null)
            {
                if (plane.OnGround)
                {
                    altitudeText.text = "ALT GND";
                }
                else
                {
                    // A word for which way the height is going, so you can see a climb without
                    // having to watch the number change.
                    string trend = "";
                    if (plane.VerticalSpeed > TrendThreshold) trend = " CLB";
                    else if (plane.VerticalSpeed < -TrendThreshold) trend = " DES";

                    altitudeText.text = string.Format("ALT {0:0} FT{1}", plane.Altitude * FeetPerMetre, trend);
                }
            }

            if (headingText != null)
            {
                // {0:000} keeps it three digits, so 9 degrees reads as 009 like a real compass.
                headingText.text = string.Format("HDG {0:000}", plane.Heading);
            }
        }

        /// <summary>
        /// The landing-gear lights. Green only when the wheels are down AND have finished moving:
        /// the gear takes a few seconds to travel, and "nearly down" is not down.
        /// </summary>
        void ShowGear(bool down, float blend)
        {
            if (gearLights == null) return;

            bool downAndLocked = down && blend > GearLockedBlend;

            Material mat = downAndLocked ? gearDownMaterial : gearUpMaterial;
            if (mat == null) return;

            for (int i = 0; i < gearLights.Length; i++)
            {
                if (gearLights[i] != null) gearLights[i].sharedMaterial = mat;
            }
        }
    }
}
