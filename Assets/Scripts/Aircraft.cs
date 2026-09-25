using UnityEngine;
using F = FlightSim.FlightLayout.Flight;
using W = FlightSim.FlightLayout.World;

namespace FlightSim
{
    /// <summary>
    /// The aeroplane you fly in scene 3, and the one scene 4 lands for you.
    ///
    /// This is a deliberately simple flight model. It is NOT a physics simulation: there is no
    /// Rigidbody, no lift equation and no angle of attack. Instead it keeps four numbers - speed,
    /// pitch, roll and heading - nudges them towards what the keys are asking for, and then works
    /// out where the plane should be a moment later. Everything you can see follows from those
    /// four numbers.
    ///
    /// That choice is the whole point. A real flight model can stall, spin and drop out of the
    /// sky, which is miserable to demonstrate and hard to explain. This one:
    ///
    ///   * levels itself out the moment you stop pressing anything,
    ///   * will not let the nose go past the angles in FlightLayout.Flight,
    ///   * cannot stall, because lift is never calculated in the first place,
    ///   * and still turns using the real formula for a banked turn, so the flying feels honest.
    ///
    /// The one thing that can go wrong is hitting the ground, which ends the flight rather than
    /// pretending nothing happened.
    ///
    /// Controls: W/S pitch, A/D roll, Q/E throttle, Space gear, V camera (see FlightCamera).
    /// </summary>
    [DisallowMultipleComponent]
    public class Aircraft : MonoBehaviour
    {
        /// <summary>The aeroplane in the current scene, so the HUD and instruments can find it.</summary>
        public static Aircraft Instance { get; private set; }

        [Header("State")]
        [Tooltip("Switched off while the screen fades, and while a crash message is on screen.")]
        public bool inputEnabled = true;

        [Tooltip("Metres per second along the direction the nose is pointing.")]
        public float speed;

        [Tooltip("0 is closed, 1 is full power. Q and E move it.")]
        public float throttle;

        [Tooltip("The group of wheels to fold away. Set by the scene builder.")]
        public Transform landingGear;

        [Tooltip("True in scene 4, where a script flies the plane instead of the keyboard.")]
        public bool scriptedFlight;

        // The four numbers the whole model is built from.
        float pitch;        // nose up (+) or down (-), in degrees
        float roll;         // banked right (+) or left (-), in degrees
        float heading;      // compass direction the nose points, in degrees

        bool onGround = true;
        bool gearDown = true;
        float gearBlend = 1f;      // 1 is down and locked, 0 is fully tucked away
        bool crashed;
        float groundY;

        // The autopilot the playtest flies with. A person never switches this on.
        bool autopilot;
        float autopilotThrottle;
        float autopilotTargetPitch;

        // ------------------------------------------------------------------ what others read

        /// <summary>Height of the wheels above the ground, in metres.</summary>
        public float Altitude { get { return transform.position.y - groundY - FlightLayout.Plane.WheelDrop; } }

        /// <summary>Compass heading in degrees, 0 to 360.</summary>
        public float Heading { get { return Mathf.Repeat(heading, 360f); } }

        public float Pitch { get { return pitch; } }
        public float Roll { get { return roll; } }
        public bool OnGround { get { return onGround; } }
        public bool GearDown { get { return gearDown; } }

        /// <summary>How far through its travel the gear is: 1 down, 0 up.</summary>
        public float GearBlend { get { return gearBlend; } }

        /// <summary>True once the flight has ended in the ground. Nothing responds after this.</summary>
        public bool Crashed { get { return crashed; } }

        /// <summary>Climbing (+) or descending (-), in metres per second.</summary>
        public float VerticalSpeed { get { return speed * Mathf.Sin(pitch * Mathf.Deg2Rad); } }

        /// <summary>Speed in knots, which is what the instruments show.</summary>
        public float Knots { get { return speed * 1.94384f; } }

        /// <summary>
        /// True when pressing L should start the approach - which is very nearly always.
        ///
        /// This used to insist you were airborne, above 300 m and with the wheels up, which read
        /// as the landing key being broken: you press L, nothing happens, and nothing tells you
        /// why. Being allowed to ask for the approach whenever you like is worth far more than
        /// the realism of refusing, so the only thing that stops it now is having crashed.
        /// </summary>
        public bool ReadyToLand
        {
            get { return !crashed; }
        }

        // ---------------------------------------------------------------------- setting up

        void Awake()
        {
            Instance = this;
            groundY = FlightLayout.World.GroundY;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // Read the starting attitude off the object the builder placed, so the scene file and
            // this script can never disagree about which way the plane is facing.
            Vector3 euler = transform.eulerAngles;
            heading = euler.y;
            pitch = 0f;
            roll = 0f;

            onGround = Altitude <= 0.05f;
            gearDown = true;
            gearBlend = 1f;

            Debug.Log(string.Format("[FLIGHT] Aircraft ready at {0}, heading {1:0}, {2}",
                                    transform.position, Heading, onGround ? "on the ground" : "airborne"));
        }

        // ------------------------------------------------------------------------ each frame

        void Update()
        {
            if (crashed) return;

            float dt = Time.deltaTime;

            // While a script is flying (scene 4), the flight model stands down completely. If it
            // did not, it would add its own forward step on top of the one the timeline just set,
            // and the plane would travel at roughly double speed - or not, depending on which
            // script Unity happened to update first, which is worse.
            //
            // The gear still animates, because that is a moving part rather than a decision.
            if (scriptedFlight)
            {
                MoveGear(dt);
                return;
            }

            ReadControls(dt);
            MoveEngines(dt);
            MoveGear(dt);
            Fly(dt);
            CheckTheGround();
        }

        /// <summary>
        /// Turns key presses into the numbers above. Holding a key moves a value; letting go lets
        /// pitch and roll drift back to level, which is what makes the plane forgiving.
        /// </summary>
        void ReadControls(float dt)
        {

            float pitchInput = 0f, rollInput = 0f, throttleInput = 0f;

            if (autopilot)
            {
                // The playtest's autopilot: hold a throttle setting and aim for one pitch angle.
                // It obeys the same rule a person does - the nose stays down until the plane is
                // fast enough to fly - so an automated take-off looks like a real one.
                float wanted = (onGround && speed < F.RotateSpeed) ? 0f : autopilotTargetPitch;

                throttle = Mathf.MoveTowards(throttle, autopilotThrottle, F.ThrottleRate * dt);
                pitch = Mathf.MoveTowards(pitch, wanted, F.PitchRate * dt);
                roll = Mathf.MoveTowards(roll, 0f, F.LevelRate * dt);
                return;
            }

            if (inputEnabled)
            {
                // S pulls the nose up, W pushes it down, the way a control column works.
                if (Input.GetKey(KeyCode.S)) pitchInput += 1f;
                if (Input.GetKey(KeyCode.W)) pitchInput -= 1f;
                if (Input.GetKey(KeyCode.D)) rollInput += 1f;
                if (Input.GetKey(KeyCode.A)) rollInput -= 1f;
                if (Input.GetKey(KeyCode.E)) throttleInput += 1f;
                if (Input.GetKey(KeyCode.Q)) throttleInput -= 1f;

                if (Input.GetKeyDown(KeyCode.Space)) ToggleGear();

                // The same four controls again, from the on-screen buttons on a phone. Adding
                // them to the keyboard's numbers means there is only ever one flight model.
                pitchInput += VirtualInput.Move;
                rollInput += VirtualInput.Strafe;
                throttleInput += VirtualInput.Throttle;

                if (VirtualInput.ConsumeGear()) ToggleGear();

                pitchInput = Mathf.Clamp(pitchInput, -1f, 1f);
                rollInput = Mathf.Clamp(rollInput, -1f, 1f);
                throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
            }

            throttle = Mathf.Clamp01(throttle + throttleInput * F.ThrottleRate * dt);

            // On the ground the nose stays on the tarmac until the wheels are ready to leave it,
            // however hard you pull. That is what stops you hauling the plane into the air at
            // walking pace, without needing to model a stall.
            bool canRotate = !onGround || speed >= F.RotateSpeed;

            if (Mathf.Abs(pitchInput) > 0.01f && canRotate)
                pitch += pitchInput * F.PitchRate * dt;
            else
                pitch = Mathf.MoveTowards(pitch, 0f, F.LevelRate * dt);

            if (Mathf.Abs(rollInput) > 0.01f && !onGround)
                roll += rollInput * F.RollRate * dt;
            else
                roll = Mathf.MoveTowards(roll, 0f, F.LevelRate * dt);

            // On the ground, A and D steer the nosewheel instead of banking the aeroplane.
            if (onGround && Mathf.Abs(rollInput) > 0.01f)
                heading += rollInput * F.GroundSteerRate * dt;

            // The clamps are why it cannot flip over or point straight up.
            pitch = Mathf.Clamp(pitch, F.PitchMin, F.PitchMax);
            roll = Mathf.Clamp(roll, -F.RollMax, F.RollMax);
        }

        /// <summary>
        /// Speed: thrust pushes, drag holds back, and drag grows with the square of speed. That
        /// squared term is what gives the plane a top speed without anything clamping it.
        /// </summary>
        void MoveEngines(float dt)
        {

            float thrust = throttle * F.MaxThrustAccel;
            float drag = F.DragFactor * speed * speed;

            speed += (thrust - drag) * dt;

            // Wheel brakes: closing the throttle on the ground pulls you up short.
            if (onGround && throttle < 0.05f)
                speed -= F.BrakeDecel * dt;

            // Climbing costs speed and diving gains it, which is what stops you holding the nose
            // up for ever at full power.
            speed -= Mathf.Sin(pitch * Mathf.Deg2Rad) * 9.81f * dt;

            speed = Mathf.Max(0f, speed);
        }

        /// <summary>Folds the wheels away, and puts them down again, over a couple of seconds.</summary>
        void MoveGear(float dt)
        {

            // A real aeroplane's gear comes up on its own once it is safely off the ground.
            //
            // The "climbing" test matters more than it looks. Without it, scene 4 would start the
            // approach 180 m up with the wheels down, decide that was too high, and fold them away
            // on short final - which is exactly the wrong moment. Wheels come up on the way up.
            if (gearDown && !onGround && !scriptedFlight &&
                Altitude > F.GearAutoRetractAltitude && VerticalSpeed > 0f)
                ToggleGear();

            float target = gearDown ? 1f : 0f;
            gearBlend = Mathf.MoveTowards(gearBlend, target, dt / Mathf.Max(0.01f, F.GearMoveSeconds));

            if (landingGear != null)
            {
                // Swing the legs up into the belly, and hide them once they are inside it.
                landingGear.localRotation = Quaternion.Euler(0f, 0f, (1f - gearBlend) * 85f);
                landingGear.gameObject.SetActive(gearBlend > 0.02f);
            }
        }

        void ToggleGear()
        {
            if (onGround && !gearDown) return;   // never fold the legs while standing on them

            gearDown = !gearDown;
            Debug.Log("[FLIGHT] Landing gear " + (gearDown ? "down" : "up"));
        }

        /// <summary>
        /// Where the plane ends up. The nose direction comes from heading and pitch, and a banked
        /// aeroplane turns: the rate is g x tan(bank) / speed, which is the real formula for a
        /// balanced turn. Steeper bank or slower speed means a tighter turn, exactly as it should.
        /// </summary>
        void Fly(float dt)
        {
            if (!onGround && speed > 1f)
            {
                float turnRate = 9.81f * Mathf.Tan(roll * Mathf.Deg2Rad) / speed;   // radians per second
                heading += turnRate * Mathf.Rad2Deg * dt;
            }

            // On the ground the plane rolls flat along the tarmac whatever the nose is doing.
            float travelPitch = onGround ? 0f : pitch;

            transform.rotation = Quaternion.Euler(-pitch, heading, -roll);

            Vector3 forward = Quaternion.Euler(-travelPitch, heading, 0f) * Vector3.forward;
            transform.position += forward * speed * dt;

            if (onGround)
            {
                // Glued to the tarmac until the wheels actually leave it.
                Vector3 p = transform.position;
                p.y = groundY + FlightLayout.Plane.WheelDrop;
                transform.position = p;

                if (speed >= FlightLayout.Flight.RotateSpeed && pitch > 1f)
                {
                    onGround = false;
                    Debug.Log(string.Format("[FLIGHT] Airborne at {0:0} kt", Knots));
                }
            }
        }

        /// <summary>
        /// The one way a flight can go wrong. Touching the ground while flying ends it, unless the
        /// wheels are down and you are over the runway, which is a landing rather than a crash.
        /// </summary>
        void CheckTheGround()
        {
            // Scene 4 flies the plane onto the runway itself and announces its own touchdown, so
            // this check would only fight with it - and fire the landing sound twice.
            if (scriptedFlight || onGround || Altitude > 0f) return;

            // A plane that is going UP cannot be hitting the ground.
            //
            // This test is not fussiness, it is the whole take-off. At the moment the wheels
            // leave the tarmac the altitude is exactly zero, so without it the very next frame
            // reads as a touchdown: the plane lands, lifts off, lands again, and sits on the
            // runway accelerating instead of climbing away.
            if (VerticalSpeed >= 0f) return;

            bool overRunway = Mathf.Abs(transform.position.z) < W.RunwayWidth * 0.5f &&
                              transform.position.x > W.ThresholdX && transform.position.x < W.FarEndX;

            if (overRunway && gearDown && VerticalSpeed > -8f)
            {
                onGround = true;
                pitch = 0f;
                roll = 0f;

                Vector3 p = transform.position;
                p.y = groundY + FlightLayout.Plane.WheelDrop;
                transform.position = p;

                Debug.Log(string.Format("[FLIGHT] Touched down at {0:0} kt", Knots));
                return;
            }

            Crash(gearDown ? "You hit the ground short of the runway." : "You flew into the ground.");
        }

        /// <summary>Ends the flight with an explanation. FlightHUD shows it and restarts the scene.</summary>
        public void Crash(string why)
        {
            if (crashed) return;

            crashed = true;
            speed = 0f;
            inputEnabled = false;

            Debug.Log("[FLIGHT] Crashed: " + why);

            if (FlightHUD.Instance != null) FlightHUD.Instance.ShowCrash(why);
        }

        // ------------------------------------------------------------------ for the playtest

        /// <summary>
        /// Flies the plane without a keyboard, so the automated playtest can prove the whole
        /// flight works. It holds a throttle setting and aims the nose at one angle, which is all
        /// a take-off and a climb actually need. A player never triggers this.
        /// </summary>
        public void Autopilot(float wantedThrottle, float wantedPitch)
        {
            autopilot = true;
            autopilotThrottle = Mathf.Clamp01(wantedThrottle);
            autopilotTargetPitch = Mathf.Clamp(wantedPitch, FlightLayout.Flight.PitchMin, FlightLayout.Flight.PitchMax);

            Debug.Log(string.Format("[FLIGHT] Autopilot: throttle {0:0.00}, aiming for {1:0} degrees",
                                    autopilotThrottle, autopilotTargetPitch));
        }

        /// <summary>Hands control back to the keyboard.</summary>
        public void AutopilotOff()
        {
            autopilot = false;
        }

        /// <summary>
        /// Puts the plane exactly where a script wants it. Scene 4's landing sequence uses this to
        /// fly the approach, so the instruments, the sound and both cameras carry on working with
        /// no special cases anywhere else.
        /// </summary>
        public void PlaceForScript(Vector3 position, float wantedPitch, float wantedRoll, float wantedHeading, float wantedSpeed)
        {
            transform.position = position;
            pitch = wantedPitch;
            roll = wantedRoll;
            heading = wantedHeading;
            speed = wantedSpeed;

            transform.rotation = Quaternion.Euler(-pitch, heading, -roll);
        }

        /// <summary>Lets the landing sequence say whether the wheels are on the tarmac.</summary>
        public void SetOnGround(bool value)
        {
            onGround = value;
        }

        /// <summary>Lets the landing sequence put the wheels down without waiting for Space.</summary>
        public void SetGear(bool down)
        {
            gearDown = down;
        }
    }
}
