using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Every number that decides how big something is and where it goes.
    ///
    /// The two scenes are not hand-made. The editor scripts in Scripts/Editor build them, and
    /// they read every size and position from this file. To move something, change the number
    /// here and run Tools > Flight Sim > Build Scenes again. Dragging an object around in the
    /// editor only lasts until the next rebuild, which puts it back.
    ///
    /// Units are metres. The floor you walk on is always y = 0.
    /// </summary>
    public static class FlightLayout
    {
        // ------------------------------------------------------------------------- scenes

        public const string SceneFolder = "Assets/Scenes";
        public const string TerminalScene = "01_Terminal";
        public const string CabinScene = "02_Cabin";

        /// <summary>Scene 3: the take-off, and the flight you steer yourself.</summary>
        public const string TakeoffScene = "03_Takeoff";

        /// <summary>Scene 4: the approach and landing, which plays itself while you watch.</summary>
        public const string LandingScene = "04_Landing";

        // ------------------------------------------------------------------------- player

        public const float PlayerHeight = 1.8f;
        public const float PlayerRadius = 0.25f;   // narrow enough to fit down the cabin aisle
        public const float EyeHeight = 1.65f;

        // ----------------------------------------------------------------- scene 1: terminal

        /// <summary>
        /// The departure hall. +Z points out of the big window towards the plane and the runway.
        /// </summary>
        public static class Terminal
        {
            // The hall is a box: x from -20 to +20, z from the back wall to the window.
            public const float HalfWidth = 20f;
            public const float BackWallZ = -7f;      // the departures board hangs here
            public const float WindowZ = 7f;         // the glass wall
            public const float CeilingY = 6f;
            public const float WindowSillY = 0.4f;
            public const float WindowHeadY = 5.6f;

            /// <summary>The apron sits 4 m below the hall, so you look down onto the plane.</summary>
            public const float GroundY = -4f;

            // Where you start: in the middle of the hall, facing the window.
            public static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, -5f);
            public const float PlayerSpawnYaw = 0f;

            // Seating: four benches of eight seats, in two rows, all facing the window.
            // The gap between x = -4.3 and x = +4.3 is left clear as the walkway.
            public static readonly float[] BenchCentresX = { -12f, -6.5f, 6.5f, 12f };
            public static readonly float[] BenchRowsZ = { -3.5f, -0.5f };
            public const int SeatsPerBench = 8;
            public const float SeatWidth = 0.55f;

            // The gate: a door in the glass wall leading onto the jet bridge.
            public const float GateX = 14f;
            public const float GateDoorWidth = 2f;
            public const float GateDoorHeight = 2.6f;
            public static readonly Vector3 GateDeskCentre = new Vector3(17f, 0f, 4.5f);

            /// <summary>Stand inside this box and "Press E to board" appears.</summary>
            public static readonly Vector3 BoardingZoneCentre = new Vector3(14f, 1.2f, 5.2f);
            public static readonly Vector3 BoardingZoneSize = new Vector3(3f, 2.4f, 2.6f);

            // The plane parked outside, side-on to the window, nose pointing east (+X).
            public static readonly Vector3 PlaneCentre = new Vector3(0f, -0.2f, 30f);
            public const float PlaneDoorX = 14f;      // lines up with the gate, so the jet bridge is straight

            // The runway, beyond the plane, running east-west.
            public const float RunwayCentreZ = 85f;
            public const float RunwayWidth = 45f;
            public const float RunwayLength = 1200f;

            /// <summary>The automated playtest walks this path: up the walkway, along the window, to the gate.</summary>
            public static readonly Vector3[] PlaytestRoute =
            {
                new Vector3(0f, 0f, 3.5f),
                new Vector3(14f, 0f, 3.5f),
                new Vector3(14f, 0f, 5.2f)
            };
        }

        // -------------------------------------------------------------------- scene 2: cabin

        /// <summary>
        /// Inside the plane. +X points to the nose (the cockpit), so the aisle runs along X.
        /// Negative Z is the left-hand side, where the boarding door and the terminal are.
        /// </summary>
        public static class Cabin
        {
            public const float HalfWidth = 1.95f;     // wall to wall is 3.9 m, like an A320
            public const float CeilingY = 2.25f;
            public const float RearWallX = -12.6f;
            public const float BulkheadX = 6.8f;      // the wall between the cabin and the cockpit
            public const float NoseX = 10.4f;         // the windscreen
            public const float CockpitHalfWidth = 1.6f;
            public const float CockpitDoorHalfWidth = 0.45f;
            public const float CockpitCeilingY = 2.1f;

            /// <summary>The ground outside, 3.6 m below the cabin floor.</summary>
            public const float GroundY = -3.6f;

            // Seats: 20 rows, three each side of the aisle ("3+3"), 0.8 m apart.
            public const int Rows = 20;
            public const float FirstRowX = 3.2f;
            public const float RowPitch = 0.8f;
            public static readonly float[] SeatZ = { -1.55f, -1.05f, -0.55f, 0.55f, 1.05f, 1.55f };
            public const float SeatWidth = 0.48f;

            /// <summary>Half the aisle width. The seat blocks start here, which is what keeps you in the aisle.</summary>
            public const float AisleHalfWidth = 0.31f;

            // The window strip in each side wall.
            public const float WindowBottomY = 0.95f;
            public const float WindowTopY = 1.4f;
            public const float WindowWidth = 0.35f;

            // The front of the cabin: the boarding door on the left, the galley on the right.
            public const float BoardingDoorX = 5f;

            // Where you start: just inside the boarding door, looking towards the cockpit door.
            public static readonly Vector3 PlayerSpawn = new Vector3(4.6f, 0f, -0.9f);
            public const float PlayerSpawnYaw = 68f;

            // The cockpit.
            public const float PilotSeatX = 8.2f;
            public const float PilotSeatZ = 0.7f;
            public const float PanelX = 9.6f;
            public static readonly Vector3 ThrottleCentre = new Vector3(8.9f, 0f, 0f);

            /// <summary>Stand inside this box (between the two pilot seats) and "Press E to take off" appears.</summary>
            public static readonly Vector3 TakeoffZoneCentre = new Vector3(8.2f, 1f, 0f);
            public static readonly Vector3 TakeoffZoneSize = new Vector3(1.8f, 2f, 1.0f);

            /// <summary>The automated playtest walks this path: down the aisle, back, then into the cockpit.</summary>
            public static readonly Vector3[] PlaytestRoute =
            {
                new Vector3(5f, 0f, 0f),
                new Vector3(-8f, 0f, 0f),
                new Vector3(5f, 0f, 0f),
                new Vector3(8.1f, 0f, 0f)
            };
        }

        // ---------------------------------------------------------------------- the plane

        /// <summary>
        /// The aeroplane itself. Scene 1 parks it outside the window, scenes 3 and 4 fly it, and
        /// all three build it from SharedParts.Airliner, so it is the same aircraft every time.
        ///
        /// Its origin is the middle of the fuselage, and the nose points towards +X.
        /// </summary>
        public static class Plane
        {
            public const float FuselageLength = 36f;
            public const float FuselageRadius = 2f;

            /// <summary>Half the fuselage: where the nose sphere and the tail cone sit.</summary>
            public const float HalfLength = FuselageLength * 0.5f;

            /// <summary>
            /// How far the bottom of the wheels is below the middle of the fuselage. Park the
            /// plane at this height and the tyres touch the ground exactly.
            /// </summary>
            public const float WheelDrop = 3.85f;

            /// <summary>
            /// Where the cockpit interior sits inside the exterior shell.
            ///
            /// The cockpit you walk into in scene 2 is built in the cabin's own coordinates (nose
            /// at x = 10.4, floor at y = 0). The exterior shell has its nose at x = 18 and its
            /// floor about 1.1 m below the middle. Shifting the interior by this much lines the
            /// two up, so the windscreen you look through sits behind the cockpit windows you can
            /// see from outside.
            /// </summary>
            public static readonly Vector3 CockpitOffset = new Vector3(7.6f, -1.1f, 0f);

            /// <summary>The pilot's eye, relative to the middle of the fuselage.</summary>
            public static readonly Vector3 EyeLocal = new Vector3(15.8f, 0.15f, 0f);

            /// <summary>
            /// How far the model has to be turned inside the aeroplane to point where it is going.
            ///
            /// The shell and the cockpit are both built with the **nose along +X**, because that
            /// is how scene 1 and scene 2 are laid out. Unity's idea of "forward", and therefore
            /// the direction Aircraft flies in, is **+Z**. Without this quarter turn the plane
            /// travels sideways down the runway: the fuselage sits across it, and the cockpit
            /// ends up 7.6 m off to one side of the pilot's head.
            ///
            /// Turning the model rather than rewriting the geometry keeps scenes 1 and 2 working
            /// exactly as they did.
            /// </summary>
            public const float ModelYaw = -90f;

            /// <summary>Turns a measurement taken in the model's nose-along-X frame into the aeroplane's own frame.</summary>
            public static Vector3 ModelToPlane(Vector3 inModelFrame)
            {
                return Quaternion.Euler(0f, ModelYaw, 0f) * inModelFrame;
            }
        }

        // --------------------------------------------------------------------- the airfield

        /// <summary>
        /// The world scenes 3 and 4 share: one runway along X, centred on the origin, with land
        /// to the west and the sea to the east. Both scenes build it from WorldParts, so the
        /// airport you take off from is the airport you land at.
        /// </summary>
        public static class World
        {
            public const float GroundY = 0f;
            public const float RunwayLength = 2600f;
            public const float RunwayWidth = 45f;

            /// <summary>The west end of the runway, where a take-off starts.</summary>
            public const float ThresholdX = -RunwayLength * 0.5f;

            /// <summary>The east end, where a landing finishes.</summary>
            public const float FarEndX = RunwayLength * 0.5f;

            /// <summary>Past this point the land stops and the sea begins.</summary>
            public const float CoastX = 6000f;
            public const float SeaY = -4f;

            public const float CloudLowY = 800f;
            public const float CloudHighY = 1500f;
            public const int CloudCount = 90;

            /// <summary>Haze, so the horizon fades out instead of ending in a hard line.</summary>
            public const float FogStart = 900f;
            public const float FogEnd = 14000f;
        }

        // ------------------------------------------------------------------- scene 3: flight

        /// <summary>
        /// Take-off and free flight. You fly this one yourself, and the numbers below are what
        /// make it forgiving: the plane levels itself when you let go of the keys, and it can
        /// neither stall nor flip over.
        /// </summary>
        public static class Flight
        {
            /// <summary>Lined up on the runway threshold, wheels on the ground, nose towards +X.</summary>
            public static readonly Vector3 PlaneStart = new Vector3(World.ThresholdX + 120f, Plane.WheelDrop, 0f);

            // ------------------------------------------------------------- engines and speed

            /// <summary>How fast the throttle lever itself moves, from 0 to 1, per second.</summary>
            public const float ThrottleRate = 0.5f;

            /// <summary>Acceleration at full throttle, in metres per second, per second.</summary>
            public const float MaxThrustAccel = 4.5f;

            /// <summary>
            /// Drag grows with the square of speed, which is what stops the plane accelerating
            /// for ever. Full thrust balances this drag at about 260 m/s.
            /// </summary>
            public const float DragFactor = 0.000066f;

            /// <summary>Wheel brakes, used when the throttle is closed on the ground.</summary>
            public const float BrakeDecel = 3.5f;

            /// <summary>
            /// Rotation speed. Below this the wheels stay down however hard you pull; above it the
            /// plane flies. A real A320 rotates at about 150 knots, which is roughly this.
            /// </summary>
            public const float RotateSpeed = 75f;

            // --------------------------------------------------------------------- controls

            public const float PitchRate = 22f;      // degrees per second while W or S is held
            public const float PitchMin = -20f;      // nose down
            public const float PitchMax = 25f;       // nose up
            public const float RollRate = 50f;
            public const float RollMax = 55f;

            /// <summary>
            /// How fast pitch and roll fall back to level when you are not touching the keys.
            /// This is the "assisted" part: let go and the plane sorts itself out.
            /// </summary>
            public const float LevelRate = 18f;

            /// <summary>Nosewheel steering on the ground, in degrees per second.</summary>
            public const float GroundSteerRate = 12f;

            // ------------------------------------------------------------------------- gear

            /// <summary>Above this height the gear folds away by itself, as a real one would.</summary>
            public const float GearAutoRetractAltitude = 20f;
            public const float GearMoveSeconds = 3f;

            // ------------------------------------------------------------------- the ending

            /// <summary>Gear up and higher than this, and "Press L to begin landing" appears.</summary>
            public const float LandingPromptAltitude = 300f;
            public const KeyCode LandingKey = KeyCode.L;

            /// <summary>
            /// Touching the ground away from the runway ends the flight. The scene reloads after
            /// this long, so a bad flight is never a dead end.
            /// </summary>
            public const float CrashRestartSeconds = 3.5f;
        }

        // ------------------------------------------------------------------- the two cameras

        /// <summary>
        /// The two views, in both scene 3 and scene 4. V swaps between them. In the pilot's seat
        /// the mouse looks around the cockpit; outside it swings the camera around the plane, and
        /// the scroll wheel moves it closer or further away.
        /// </summary>
        public static class FlightCam
        {
            public const KeyCode ToggleKey = KeyCode.V;

            public const float ChaseDistance = 55f;
            public const float ChaseMinDistance = 25f;
            public const float ChaseMaxDistance = 150f;
            public const float ChaseHeight = 10f;
            public const float ZoomRate = 40f;

            /// <summary>How quickly the chase camera catches up. Lower is lazier and smoother.</summary>
            public const float FollowSharpness = 4f;

            public const float StartOrbitYaw = -18f;
            public const float StartOrbitPitch = 12f;
            public const float MinOrbitPitch = -70f;
            public const float MaxOrbitPitch = 80f;

            /// <summary>How far you can turn your head in the pilot's seat, in degrees.</summary>
            public const float CockpitYawLimit = 120f;
            public const float CockpitPitchLimit = 70f;
        }

        // ------------------------------------------------------------------ scene 4: landing

        /// <summary>
        /// The approach and touchdown. You fly none of this: the whole thing is a timeline, and
        /// these are its milestones in seconds from the moment the scene starts. You can still
        /// look around and swap cameras the whole way down.
        /// </summary>
        public static class Landing
        {
            public const float ApproachSpeed = 72f;
            public const float TouchdownSpeed = 66f;

            /// <summary>Where the wheels are meant to meet the tarmac.</summary>
            public const float TouchdownX = World.ThresholdX + 330f;

            /// <summary>How high the approach begins, above the wheels' resting height.</summary>
            public const float StartAltitude = 180f;

            // The timeline.
            public const float DescentSeconds = 36f;   // a steady glide down to the flare
            public const float FlareSeconds = 3f;      // nose comes up, the sink rate washes off
            public const float RollOutSeconds = 13f;   // reverse thrust and brakes, down to a stop
            public const float EndCardDelay = 1.5f;    // a breath after stopping, then the card

            /// <summary>
            /// Where the approach starts, worked out from the numbers above rather than typed in.
            ///
            /// This matters: if the distance and the speed disagreed, the plane would have to
            /// cheat to arrive on time and the speed on the instruments would be a lie. Flying at
            /// ApproachSpeed for the whole descent and flare covers exactly this far, so the
            /// readout you see is the speed it is really travelling.
            /// </summary>
            public static readonly Vector3 PlaneStart = new Vector3(
                TouchdownX - ApproachSpeed * (DescentSeconds + FlareSeconds),
                Plane.WheelDrop + StartAltitude,
                0f);

            public const float ApproachPitch = -3f;    // nose slightly down on the glideslope
            public const float FlarePitch = 6f;        // nose up over the threshold
            public const float StopPitch = 0f;

            /// <summary>Press this on the arrival card to fly the whole thing again.</summary>
            public const KeyCode RestartKey = KeyCode.R;
        }
    }
}
