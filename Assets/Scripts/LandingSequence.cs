using UnityEngine;
using L = FlightSim.FlightLayout.Landing;

namespace FlightSim
{
    /// <summary>
    /// Scene 4: the approach and touchdown, which flies itself while you watch.
    ///
    /// You steer none of this. It is a timeline: one clock, four stages, and at every moment the
    /// script works out exactly where the aeroplane should be and puts it there. Nothing is left
    /// to chance, so the landing looks right every single time it is demonstrated.
    ///
    ///     descent    a straight glide from PlaneStart down towards the runway
    ///     flare      the nose comes up and the sink rate washes off, just above the tarmac
    ///     roll-out   wheels down, reverse thrust and brakes, slowing to a stop
    ///     arrived    the end card
    ///
    /// It moves the SAME Aircraft component the player flies in scene 3, through
    /// Aircraft.PlaceForScript. That is the reason the instruments, the engine sound and both
    /// camera views keep working here without a single special case: as far as everything else is
    /// concerned, this is just an aeroplane flying.
    ///
    /// Every duration and height comes from FlightLayout.Landing.
    /// </summary>
    [DisallowMultipleComponent]
    public class LandingSequence : MonoBehaviour
    {
        /// <summary>The sequence in the current scene, so the playtest can ask how far it has got.</summary>
        public static LandingSequence Instance { get; private set; }

        public enum Stage { Descent, Flare, RollOut, Arrived }

        [Header("Wired up by the scene builder")]
        public Aircraft plane;
        public AircraftAudio sound;
        public EndCard endCard;

        Stage stage = Stage.Descent;
        float clock;
        float touchdownAt;     // the moment the wheels met the tarmac
        float rollOutSpeed;

        /// <summary>Which part of the landing is happening now.</summary>
        public Stage Current { get { return stage; } }

        /// <summary>True once the plane has stopped and the arrival card is up.</summary>
        public bool Finished { get { return stage == Stage.Arrived; } }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (plane == null) plane = Aircraft.Instance;

            if (plane == null)
            {
                Debug.LogError("[LANDING] There is no aeroplane in this scene, so the landing cannot run");
                enabled = false;
                return;
            }

            // The player's keys do nothing here. Only this script moves the plane, and telling
            // Aircraft so stops its own take-off logic (auto-retracting gear, spotting a landing)
            // from interfering with the timeline.
            plane.inputEnabled = false;
            plane.scriptedFlight = true;
            plane.SetGear(true);
            plane.SetOnGround(false);
            plane.throttle = 0.25f;

            Debug.Log(string.Format("[LANDING] Starting the approach {0:0} m out at {1:0} m",
                                    FlightLayout.Landing.TouchdownX - FlightLayout.Landing.PlaneStart.x,
                                    FlightLayout.Landing.StartAltitude));
        }

        void Update()
        {
            if (plane == null) return;

            clock += Time.deltaTime;

            switch (stage)
            {
                case Stage.Descent: Descend(); break;
                case Stage.Flare: Flare(); break;
                case Stage.RollOut: RollOut(); break;
            }
        }

        /// <summary>
        /// The glide down. The plane travels forward at a steady speed and loses height evenly,
        /// which is what a real approach looks like from outside: a straight line to the runway.
        /// </summary>
        void Descend()
        {

            float t = Mathf.Clamp01(clock / L.DescentSeconds);

            Vector3 from = L.PlaneStart;
            Vector3 to = new Vector3(FlareStartX(), FlightLayout.Plane.WheelDrop + FlareHeight, 0f);

            plane.PlaceForScript(Vector3.Lerp(from, to, t), L.ApproachPitch, 0f, 90f, L.ApproachSpeed);

            if (clock >= L.DescentSeconds)
            {
                stage = Stage.Flare;
                Debug.Log(string.Format("[LANDING] Over the threshold at {0:0} m, flaring", plane.Altitude));
            }
        }

        /// <summary>
        /// The last few seconds. The nose comes up and the descent flattens out, so the wheels
        /// arrive gently instead of driving into the tarmac at the approach angle.
        /// </summary>
        void Flare()
        {

            float t = Mathf.Clamp01((clock - L.DescentSeconds) / L.FlareSeconds);

            Vector3 from = new Vector3(FlareStartX(), FlightLayout.Plane.WheelDrop + FlareHeight, 0f);
            Vector3 to = new Vector3(L.TouchdownX, FlightLayout.Plane.WheelDrop, 0f);

            // Ease the height out rather than dropping in a straight line: that curve IS the flare.
            Vector3 p = Vector3.Lerp(from, to, t);
            p.y = Mathf.Lerp(from.y, to.y, t * t);

            float pitch = Mathf.Lerp(L.ApproachPitch, L.FlarePitch, t);
            float speed = Mathf.Lerp(L.ApproachSpeed, L.TouchdownSpeed, t);

            plane.PlaceForScript(p, pitch, 0f, 90f, speed);

            if (t >= 1f) Touchdown();
        }

        void Touchdown()
        {

            stage = Stage.RollOut;
            touchdownAt = clock;
            rollOutSpeed = L.TouchdownSpeed;

            plane.SetOnGround(true);

            // The touchdown thump is NOT played here. AircraftAudio already fires it the moment
            // the wheels meet the ground, and playing it here too would sound like two aeroplanes
            // landing at once.
            if (sound != null) sound.reverseThrust = true;

            Debug.Log(string.Format("[LANDING] Touchdown at {0:0} kt", plane.Knots));
        }

        /// <summary>
        /// Wheels on the ground, slowing to a stop. The speed comes down smoothly and the nose
        /// lowers onto the nosewheel, the way it does after a real landing.
        /// </summary>
        void RollOut()
        {

            float elapsed = clock - touchdownAt;
            float t = Mathf.Clamp01(elapsed / L.RollOutSeconds);

            // Slow down hardest at the start, which is when reverse thrust and the brakes bite.
            rollOutSpeed = Mathf.Lerp(L.TouchdownSpeed, 0f, t * t);

            Vector3 p = plane.transform.position;
            p.x += rollOutSpeed * Time.deltaTime;
            p.y = FlightLayout.Plane.WheelDrop;

            float pitch = Mathf.Lerp(L.FlarePitch, L.StopPitch, Mathf.Clamp01(elapsed / 2f));
            plane.PlaceForScript(p, pitch, 0f, 90f, rollOutSpeed);

            plane.throttle = Mathf.Lerp(0.4f, 0f, t);

            // Reverse thrust is only used in the first part of the roll-out.
            if (sound != null && elapsed > L.RollOutSeconds * 0.55f) sound.reverseThrust = false;

            if (t >= 1f) Arrive();
        }

        void Arrive()
        {
            stage = Stage.Arrived;
            plane.PlaceForScript(plane.transform.position, FlightLayout.Landing.StopPitch, 0f, 90f, 0f);
            plane.throttle = 0f;

            if (sound != null) sound.reverseThrust = false;

            // Stop the journey clock here, so the arrival card shows how long the flight took
            // rather than carrying on counting while you read it.
            FlightClock.Stop();

            Debug.Log("[LANDING] Stopped on the runway");

            if (endCard != null) endCard.Show(FlightLayout.Landing.EndCardDelay);
        }

        /// <summary>Where the flare begins: one flare's worth of travel before the touchdown point.</summary>
        static float FlareStartX()
        {
            return FlightLayout.Landing.TouchdownX - FlightLayout.Landing.ApproachSpeed * FlightLayout.Landing.FlareSeconds;
        }

        /// <summary>
        /// How high the wheels are when the flare starts. It follows from the glide itself: the
        /// approach loses height at a steady rate, and this is what is left at that point.
        /// </summary>
        static float FlareHeight
        {
            get
            {
                float total = L.DescentSeconds + L.FlareSeconds;
                return L.StartAltitude * (L.FlareSeconds / total);
            }
        }
    }
}
