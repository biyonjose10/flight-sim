using UnityEngine;
using T = FlightSim.FlightLayout.Terminal;

namespace FlightSim.Build
{
    /// <summary>
    /// Builds scene 1, the departure hall at Gate 7 (Assets/Scenes/01_Terminal.unity).
    ///
    /// Inside: floor, walls and ceiling, benches facing a long glass wall, a departures board,
    /// the gate door with its desk, and a few passengers. Outside, 4 m below the window: the
    /// apron, the jet bridge, your plane parked side-on, and the runway beyond it.
    ///
    /// Every size and position comes from FlightLayout.Terminal.
    /// </summary>
    public static class TerminalBuilder
    {
        static Material floorTile, carpet, wall, ceiling, frame, glass, door, seatFrame, seatCushion,
                        lightPanel, boardCasing, boardScreen, gateSign, deskBody, deskTop, pillar, pot, leaves;

        public static void Build()
        {
            var scene = SceneKit.NewScene();
            CreateMaterials();

            var hall = new GameObject("Hall").transform;
            Shell(hall);
            WindowWall(hall);
            Seating(hall);
            Gate(hall);
            DeparturesBoard(hall);
            CeilingLights(hall);
            Plants(hall);

            var outside = new GameObject("Outside").transform;
            Ground(outside);
            SharedParts.JetBridge(outside, T.GateX, T.WindowZ + 0.6f, T.PlaneCentre.z - 1.35f, 0f, T.GroundY);
            SharedParts.Runway(outside, new Vector3(0f, T.GroundY, T.RunwayCentreZ), T.RunwayLength, T.RunwayWidth);
            SharedParts.Airliner(outside, T.PlaneCentre);

            People(new GameObject("People").transform);
            Sound(new GameObject("Sound").transform);

            // Sun high in the south, so it lights the side of the plane that faces the window.
            SceneKit.Environment(new Vector3(42f, 25f, 0f), 1.15f, 250f, 1400f);
            SceneKit.Player(T.PlayerSpawn, T.PlayerSpawnYaw);
            SceneKit.Zone("Boarding Zone (Gate 7)", T.BoardingZoneCentre, T.BoardingZoneSize,
                          "Press E to board the plane", FlightLayout.CabinScene, null);
            SceneKit.GameSystems("Gate 7 - Departures",
                                 "Have a look out of the window, then walk to the gate door to board");

            SceneKit.Save(scene, FlightLayout.TerminalScene);
        }

        /// <summary>
        /// What the departure hall sounds like: a low hum that never stops, and a boarding call
        /// every half-minute or so. An empty room with no sound at all reads as a fault rather
        /// than as quiet.
        /// </summary>
        static void Sound(Transform t)
        {
            var ambience = t.gameObject.AddComponent<Ambience>();
            ambience.bed = SceneKit.Loop(t, "Hall Hum", AudioBank.HallHum, 0.6f);
            ambience.occasional = SceneKit.OneShotSource(t, "Announcements");
            ambience.occasionalClip = AudioBank.Load(AudioBank.PaChime);
            ambience.occasionalVolume = 0.45f;
            ambience.minGap = 22f;
            ambience.maxGap = 45f;
        }

        static void CreateMaterials()
        {
            floorTile   = Prim.Mat("Terminal Floor", new Color(0.78f, 0.77f, 0.74f), 0f, 0.55f);
            carpet      = Prim.Mat("Terminal Carpet", new Color(0.2f, 0.27f, 0.38f), 0f, 0.05f);
            wall        = Prim.Mat("Terminal Wall", new Color(0.86f, 0.85f, 0.82f));
            ceiling     = Prim.Mat("Terminal Ceiling", new Color(0.9f, 0.9f, 0.9f));
            frame       = Prim.Mat("Window Frame", new Color(0.25f, 0.27f, 0.3f), 0.6f, 0.5f);
            glass       = Prim.Glass("Window Glass", new Color(0.7f, 0.85f, 0.95f, 0.15f));
            door        = Prim.Mat("Gate Door", new Color(0.32f, 0.36f, 0.42f), 0.4f, 0.4f);
            seatFrame   = Prim.Mat("Seat Frame", new Color(0.55f, 0.57f, 0.6f), 0.7f, 0.5f);
            seatCushion = Prim.Mat("Terminal Seat", new Color(0.12f, 0.3f, 0.5f));
            lightPanel  = Prim.Emissive("Ceiling Light", Color.white, new Color(1.6f, 1.55f, 1.4f));
            boardCasing = Prim.Mat("Board Casing", new Color(0.08f, 0.08f, 0.1f));
            boardScreen = Prim.Emissive("Board Screen", new Color(0.02f, 0.03f, 0.06f), new Color(0.02f, 0.04f, 0.1f));
            gateSign    = Prim.Emissive("Gate Sign", new Color(0.1f, 0.25f, 0.6f), new Color(0.08f, 0.2f, 0.55f));
            deskBody    = Prim.Mat("Gate Desk", new Color(0.3f, 0.33f, 0.38f), 0.2f, 0.5f);
            deskTop     = Prim.Mat("Gate Desk Top", new Color(0.85f, 0.85f, 0.83f), 0f, 0.6f);
            pillar      = Prim.Mat("Pillar", new Color(0.92f, 0.92f, 0.92f), 0.1f, 0.4f);
            pot         = Prim.Mat("Plant Pot", new Color(0.3f, 0.3f, 0.32f), 0.2f, 0.4f);
            leaves      = Prim.Mat("Plant Leaves", new Color(0.2f, 0.45f, 0.22f), 0f, 0.1f);
        }

        // ---------------------------------------------------------------------- inside

        static void Shell(Transform t)
        {
            float hw = T.HalfWidth, ceil = T.CeilingY;
            float depth = T.WindowZ - T.BackWallZ;
            float midZ = (T.WindowZ + T.BackWallZ) * 0.5f;

            Prim.Box(t, "Floor", new Vector3(0f, -0.05f, midZ), new Vector3(hw * 2f, 0.1f, depth), floorTile, true);
            Prim.Box(t, "Ceiling", new Vector3(0f, ceil + 0.05f, midZ), new Vector3(hw * 2f, 0.1f, depth), ceiling, true);

            // Carpet under the two seating areas.
            foreach (int side in new[] { -1, 1 })
                Prim.Box(t, "Carpet", new Vector3(side * 9.25f, 0.005f, -2f), new Vector3(11.5f, 0.01f, 5.2f), carpet);

            Prim.Box(t, "Back Wall", new Vector3(0f, ceil * 0.5f, T.BackWallZ - 0.1f), new Vector3(hw * 2f, ceil, 0.2f), wall, true);
            Prim.Box(t, "West Wall", new Vector3(-hw - 0.1f, ceil * 0.5f, midZ), new Vector3(0.2f, ceil, depth), wall, true);
            Prim.Box(t, "East Wall", new Vector3(hw + 0.1f, ceil * 0.5f, midZ), new Vector3(0.2f, ceil, depth), wall, true);

            foreach (float x in new[] { -18f, -10f, 10f, 18f })
                Prim.Cyl(t, "Pillar", new Vector3(x, ceil * 0.5f, T.WindowZ - 0.7f), 0.3f, ceil, Prim.AxisY, pillar, true);
        }

        static void WindowWall(Transform hall)
        {
            var w = Prim.Empty(hall, "Window Wall", Vector3.zero).transform;

            float hw = T.HalfWidth, z = T.WindowZ, sill = T.WindowSillY, head = T.WindowHeadY;
            float doorL = T.GateX - T.GateDoorWidth * 0.5f;
            float doorR = T.GateX + T.GateDoorWidth * 0.5f;

            Prim.Box(w, "Sill", new Vector3(0f, sill * 0.5f, z), new Vector3(hw * 2f, sill, 0.3f), frame, true);
            Prim.Box(w, "Header", new Vector3(0f, (head + T.CeilingY) * 0.5f, z), new Vector3(hw * 2f, T.CeilingY - head, 0.3f), frame, true);

            // Glass either side of the gate door, and above it. Solid, so you can't walk out.
            Pane(w, "Glass West", -hw, doorL, sill, head, z);
            Pane(w, "Glass East", doorR, hw, sill, head, z);
            Pane(w, "Glass Above Door", doorL, doorR, T.GateDoorHeight, head, z);

            Prim.Box(w, "Gate Door", new Vector3(T.GateX, T.GateDoorHeight * 0.5f, z),
                     new Vector3(T.GateDoorWidth, T.GateDoorHeight, 0.12f), door, true);
            Prim.Box(w, "Gate Door Push Bar", new Vector3(T.GateX, 1.05f, z - 0.08f), new Vector3(1.4f, 0.05f, 0.05f), frame);

            // Vertical frames every 4 m, skipping the door, plus one each side of it.
            float glassHeight = head - sill, glassMid = (sill + head) * 0.5f;
            for (float x = -hw + 2f; x < hw - 0.01f; x += 4f)
            {
                if (x > doorL - 0.2f && x < doorR + 0.2f) continue;
                Prim.Box(w, "Mullion", new Vector3(x, glassMid, z), new Vector3(0.1f, glassHeight, 0.16f), frame);
            }

            Prim.Box(w, "Door Frame West", new Vector3(doorL, glassMid, z), new Vector3(0.12f, glassHeight, 0.18f), frame);
            Prim.Box(w, "Door Frame East", new Vector3(doorR, glassMid, z), new Vector3(0.12f, glassHeight, 0.18f), frame);
        }

        static void Pane(Transform parent, string name, float xFrom, float xTo, float yFrom, float yTo, float z)
        {
            Prim.NoShadow(Prim.Box(parent, name,
                new Vector3((xFrom + xTo) * 0.5f, (yFrom + yTo) * 0.5f, z),
                new Vector3(xTo - xFrom, yTo - yFrom, 0.04f), glass, true));
        }

        /// <summary>Benches of joined seats, all facing the window (+Z). Each bench gets one invisible collider.</summary>
        static void Seating(Transform hall)
        {
            float length = T.SeatsPerBench * T.SeatWidth;

            foreach (float z in T.BenchRowsZ)
            {
                foreach (float cx in T.BenchCentresX)
                {
                    var bench = Prim.Empty(hall, "Bench", new Vector3(cx, 0f, z)).transform;

                    Prim.Box(bench, "Beam", new Vector3(0f, 0.36f, -0.05f), new Vector3(length, 0.06f, 0.08f), seatFrame);
                    Prim.Box(bench, "Leg", new Vector3(-length * 0.5f + 0.3f, 0.18f, -0.05f), new Vector3(0.08f, 0.36f, 0.4f), seatFrame);
                    Prim.Box(bench, "Leg", new Vector3(length * 0.5f - 0.3f, 0.18f, -0.05f), new Vector3(0.08f, 0.36f, 0.4f), seatFrame);

                    for (int i = 0; i < T.SeatsPerBench; i++)
                    {
                        float x = -length * 0.5f + T.SeatWidth * (i + 0.5f);
                        Prim.Box(bench, "Seat", new Vector3(x, 0.44f, 0f), new Vector3(T.SeatWidth - 0.05f, 0.07f, 0.5f), seatCushion);
                        Prim.Box(bench, "Backrest", new Vector3(x, 0.72f, -0.25f), new Vector3(T.SeatWidth - 0.05f, 0.5f, 0.06f),
                                 new Vector3(-8f, 0f, 0f), seatCushion);

                        if (i < T.SeatsPerBench - 1)
                            Prim.Box(bench, "Armrest", new Vector3(x + T.SeatWidth * 0.5f, 0.6f, -0.02f), new Vector3(0.04f, 0.04f, 0.42f), seatFrame);
                    }

                    Prim.Blocker(bench, "Bench Collider", new Vector3(0f, 0.5f, -0.02f), new Vector3(length, 1f, 0.6f));
                }
            }
        }

        static void Gate(Transform t)
        {
            float z = T.WindowZ;

            Prim.NoShadow(Prim.Box(t, "Gate Sign", new Vector3(T.GateX, 3.45f, z - 0.3f), new Vector3(3.4f, 0.95f, 0.12f), gateSign));
            Prim.Text3D(t, "Gate Sign Number", new Vector3(T.GateX, 3.62f, z - 0.37f), Vector3.zero, "GATE 7", 0.05f, Color.white);
            Prim.Text3D(t, "Gate Sign Flight", new Vector3(T.GateX, 3.18f, z - 0.37f), Vector3.zero,
                        "FS 204   LONDON   NOW BOARDING", 0.02f, new Color(1f, 0.85f, 0.3f));

            Vector3 d = T.GateDeskCentre;
            Prim.Box(t, "Gate Desk", d + new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.1f, 0.8f), deskBody, true);
            Prim.Box(t, "Gate Desk Top", d + new Vector3(0f, 1.125f, 0f), new Vector3(2.35f, 0.05f, 0.95f), deskTop);
            Prim.Box(t, "Gate Desk Monitor", d + new Vector3(0.4f, 1.38f, 0.15f), new Vector3(0.5f, 0.35f, 0.04f), boardCasing);
            Prim.Text3D(t, "Gate Desk Label", d + new Vector3(0f, 0.7f, -0.41f), Vector3.zero, "GATE 7", 0.03f, Color.white);

            // Queue posts with belts between them, marking the boarding line.
            for (int i = 0; i < 4; i++)
            {
                float qz = 1.2f + i * 1.2f;
                Prim.Cyl(t, "Queue Post", new Vector3(15.1f, 0.5f, qz), 0.04f, 1f, Prim.AxisY, seatFrame);
                if (i < 3)
                    Prim.Box(t, "Queue Belt", new Vector3(15.1f, 0.93f, qz + 0.6f), new Vector3(0.03f, 0.05f, 1.2f), gateSign);
            }
        }

        /// <summary>The flight board on the back wall, read by someone facing away from the window (hence the 180-degree turn).</summary>
        static void DeparturesBoard(Transform t)
        {
            float z = T.BackWallZ + 0.1f;
            var facing = new Vector3(0f, 180f, 0f);

            Prim.Box(t, "Departures Board", new Vector3(0f, 3.5f, z), new Vector3(8f, 3f, 0.15f), boardCasing);
            Prim.NoShadow(Prim.Box(t, "Board Screen", new Vector3(0f, 3.45f, z + 0.08f), new Vector3(7.6f, 2.6f, 0.02f), boardScreen));
            Prim.Text3D(t, "Board Title", new Vector3(0f, 4.5f, z + 0.1f), facing, "DEPARTURES", 0.04f, new Color(1f, 0.8f, 0.25f));

            string[,] flights =
            {
                { "FS 204", "LONDON",    "GATE 7",  "BOARDING" },
                { "FS 118", "DUBAI",     "GATE 3",  "ON TIME"  },
                { "FS 330", "SINGAPORE", "GATE 12", "ON TIME"  },
                { "FS 087", "NEW YORK",  "GATE 9",  "DELAYED"  },
                { "FS 451", "TOKYO",     "GATE 5",  "ON TIME"  }
            };

            // Seen from the front, the text runs towards -X, so each column starts further along -X.
            float[] columnX = { 3.5f, 2.1f, -0.3f, -2.1f };

            for (int row = 0; row < flights.GetLength(0); row++)
            {
                float y = 4.0f - row * 0.42f;
                for (int col = 0; col < 4; col++)
                {
                    string text = flights[row, col];
                    Color c = col < 3 ? new Color(0.95f, 0.95f, 0.9f)
                            : text == "BOARDING" ? new Color(0.35f, 1f, 0.45f)
                            : text == "DELAYED" ? new Color(1f, 0.35f, 0.3f)
                            : new Color(1f, 0.8f, 0.25f);

                    Prim.Text3D(t, "Board " + text, new Vector3(columnX[col], y, z + 0.1f), facing, text, 0.026f, c, TextAnchor.MiddleLeft);
                }
            }
        }

        static void CeilingLights(Transform t)
        {
            foreach (float x in new[] { -15f, -5f, 5f, 15f })
            {
                foreach (float z in new[] { -4f, 0f, 4f })
                    Prim.NoShadow(Prim.Box(t, "Ceiling Light Panel", new Vector3(x, T.CeilingY - 0.03f, z), new Vector3(3f, 0.05f, 0.8f), lightPanel));

                foreach (float z in new[] { -3.5f, 2.5f })
                    SceneKit.PointLight(t, "Ceiling Light", new Vector3(x, T.CeilingY - 0.6f, z), new Color(1f, 0.95f, 0.85f), 1.4f, 13f);
            }
        }

        static void Plants(Transform t)
        {
            foreach (float x in new[] { -19f, 19f })
            {
                Prim.Cyl(t, "Plant Pot", new Vector3(x, 0.3f, T.BackWallZ + 0.8f), 0.35f, 0.6f, Prim.AxisY, pot, true);
                Prim.Sphere(t, "Plant", new Vector3(x, 1.05f, T.BackWallZ + 0.8f), new Vector3(1f, 1.2f, 1f), leaves);
            }
        }

        static void People(Transform t)
        {
            // (bench index, row index, seat index) for the people sitting down.
            int[,] seated = { { 0, 0, 2 }, { 0, 1, 5 }, { 1, 0, 6 }, { 2, 1, 1 }, { 3, 0, 3 }, { 3, 1, 7 }, { 2, 0, 4 } };
            float length = T.SeatsPerBench * T.SeatWidth;

            for (int i = 0; i < seated.GetLength(0); i++)
            {
                float cx = T.BenchCentresX[seated[i, 0]];
                float z = T.BenchRowsZ[seated[i, 1]];
                float x = cx - length * 0.5f + T.SeatWidth * (seated[i, 2] + 0.5f);
                Figures.Seated(t, "Passenger (sitting)", new Vector3(x, 0f, z + 0.02f), 0f, i);
            }

            Figures.Standing(t, "Passenger (at the window)", new Vector3(-7f, 0f, 5.5f), 0f, 3);
            Figures.Standing(t, "Passenger (at the window)", new Vector3(-6.2f, 0f, 5.3f), -25f, 9);
            Figures.Standing(t, "Passenger (queueing)", new Vector3(15.6f, 0f, 2.9f), -10f, 10);
            Figures.Standing(t, "Passenger (queueing)", new Vector3(15.7f, 0f, 1.9f), 5f, 12);
            Figures.Standing(t, "Gate Agent", T.GateDeskCentre + new Vector3(0f, 0f, 0.85f), 180f, 5);
        }

        // --------------------------------------------------------------------- outside

        static void Ground(Transform o)
        {
            float g = T.GroundY;
            float midZ = (T.WindowZ + T.BackWallZ) * 0.5f;
            float width = T.HalfWidth * 2f + 1f;

            Prim.Box(o, "Grass", new Vector3(0f, g - 0.1f, 300f), new Vector3(3000f, 0.2f, 3000f), SharedParts.Grass);
            Prim.Box(o, "Apron", new Vector3(0f, g + 0.01f, 34f), new Vector3(170f, 0.02f, 54f), SharedParts.Concrete);
            Prim.Box(o, "Taxi Line to the Plane", new Vector3(0f, g + 0.025f, T.PlaneCentre.z), new Vector3(70f, 0.01f, 0.3f), SharedParts.LineYellow);
            Prim.Box(o, "Taxi Line Across the Apron", new Vector3(0f, g + 0.025f, 52f), new Vector3(170f, 0.01f, 0.3f), SharedParts.LineYellow);

            // The outside of the building: the wall under the window, the storey below the hall, and the roof.
            Prim.Box(o, "Terminal Facade", new Vector3(0f, g * 0.5f, T.WindowZ + 0.35f), new Vector3(width, -g, 0.6f), SharedParts.Facade);
            Prim.Box(o, "Terminal Lower Floor", new Vector3(0f, (g - 0.1f) * 0.5f, midZ), new Vector3(width, -g - 0.1f, T.WindowZ - T.BackWallZ), SharedParts.Facade);
            Prim.Box(o, "Terminal Roof", new Vector3(0f, T.CeilingY + 0.35f, midZ + 0.4f),
                     new Vector3(width + 0.2f, 0.6f, T.WindowZ - T.BackWallZ + 1.4f), SharedParts.Roof);
        }
    }
}
