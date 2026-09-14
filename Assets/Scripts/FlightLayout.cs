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

        /// <summary>Scene 3. Not built yet, and the take-off button says so instead of crashing.</summary>
        public const string TakeoffScene = "03_Takeoff";

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
            public const float FuselageLength = 36f;
            public const float FuselageRadius = 2f;
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
    }
}
