using UnityEngine;
using C = FlightSim.FlightLayout.Cabin;

namespace FlightSim.Build
{
    /// <summary>
    /// Builds scene 2, inside the plane (Assets/Scenes/02_Cabin.unity).
    ///
    /// From the back to the front: the rear wall with the toilets, 20 rows of 3+3 seats either
    /// side of the aisle, the boarding door and the galley, the bulkhead with the open cockpit
    /// door, and the cockpit itself (two pilot seats, glowing screens, the thrust levers and
    /// the windscreen). Outside the windows are the wings, the terminal, the jet bridge and,
    /// straight ahead of the nose, the runway.
    ///
    /// Every size and position comes from FlightLayout.Cabin.
    /// </summary>
    public static class CabinBuilder
    {
        static Material floorMat, aisleCarpet, wallPanel, ceilingMat, binMat, binLip, seatShell, seatFabric, headrestCover,
                        lightStrip, readingLight, bulkheadMat, galleyMat, worktop, glass, doorMat, exitSign,
                        cockpitGrey, cockpitFloor, panelBlack, glareshield, screenSky, screenGround, screenNav, screenEngine,
                        screenLine, magenta, amber, buttonGreen, pedestalMat, leverKnob, pilotSeatMat, stickMat;

        public static void Build()
        {
            var scene = SceneKit.NewScene();
            CreateMaterials();

            var cabin = new GameObject("Cabin").transform;
            Shell(cabin);
            SideWalls(cabin);
            Seats(cabin);
            FrontOfCabin(cabin);
            Lights(cabin);

            Cockpit(new GameObject("Cockpit").transform);
            Outside(new GameObject("Outside").transform);
            Passengers(new GameObject("Passengers").transform);

            SceneKit.Environment(new Vector3(40f, 60f, 0f), 1.1f, 300f, 1800f);
            SceneKit.Player(C.PlayerSpawn, C.PlayerSpawnYaw);
            SceneKit.Zone("Take-off Zone (Thrust Levers)", C.TakeoffZoneCentre, C.TakeoffZoneSize,
                          "Press E to take off", FlightLayout.TakeoffScene,
                          "Take-off is scene 3 - it hasn't been built yet.");
            SceneKit.GameSystems("On Board - Flight FS 204", "Walk the cabin, then head through the open door to the cockpit");

            SceneKit.Save(scene, FlightLayout.CabinScene);
        }

        static void CreateMaterials()
        {
            floorMat      = Prim.Mat("Cabin Floor", new Color(0.3f, 0.3f, 0.32f));
            aisleCarpet   = Prim.Mat("Cabin Carpet", new Color(0.25f, 0.2f, 0.32f), 0f, 0.05f);
            wallPanel     = Prim.Mat("Cabin Wall", new Color(0.9f, 0.9f, 0.88f), 0f, 0.35f);
            ceilingMat    = Prim.Mat("Cabin Ceiling", new Color(0.93f, 0.93f, 0.92f));
            binMat        = Prim.Mat("Overhead Bin", new Color(0.88f, 0.88f, 0.86f), 0f, 0.4f);
            binLip        = Prim.Mat("Overhead Bin Lip", new Color(0.55f, 0.56f, 0.58f), 0.3f, 0.4f);
            seatShell     = Prim.Mat("Cabin Seat Shell", new Color(0.35f, 0.36f, 0.4f), 0.3f, 0.4f);
            seatFabric    = Prim.Mat("Cabin Seat Fabric", new Color(0.13f, 0.22f, 0.42f), 0f, 0.1f);
            headrestCover = Prim.Mat("Headrest Cover", new Color(0.85f, 0.85f, 0.82f), 0f, 0.1f);
            lightStrip    = Prim.Emissive("Cabin Light Strip", Color.white, new Color(1.3f, 1.25f, 1.1f));
            readingLight  = Prim.Emissive("Reading Light Panel", new Color(0.8f, 0.8f, 0.78f), new Color(0.25f, 0.25f, 0.22f));
            bulkheadMat   = Prim.Mat("Bulkhead", new Color(0.7f, 0.72f, 0.75f));
            galleyMat     = Prim.Mat("Galley Steel", new Color(0.72f, 0.74f, 0.76f), 0.7f, 0.55f);
            worktop       = Prim.Mat("Galley Worktop", new Color(0.9f, 0.9f, 0.9f), 0.2f, 0.6f);
            glass         = Prim.Glass("Cabin Window Glass", new Color(0.8f, 0.9f, 1f, 0.12f));
            doorMat       = Prim.Mat("Cabin Door", new Color(0.78f, 0.79f, 0.8f), 0.3f, 0.4f);
            exitSign      = Prim.Emissive("Exit Sign", new Color(0.1f, 0.5f, 0.2f), new Color(0.1f, 0.9f, 0.3f));
            cockpitGrey   = Prim.Mat("Cockpit Grey", new Color(0.32f, 0.34f, 0.37f), 0.1f, 0.3f);
            cockpitFloor  = Prim.Mat("Cockpit Floor", new Color(0.15f, 0.15f, 0.16f));
            panelBlack    = Prim.Mat("Panel Black", new Color(0.06f, 0.06f, 0.07f), 0.2f, 0.4f);
            glareshield   = Prim.Mat("Glareshield", new Color(0.1f, 0.1f, 0.11f));
            screenSky     = Prim.Emissive("Screen Sky", new Color(0.15f, 0.4f, 0.9f), new Color(0.2f, 0.55f, 1.3f));
            screenGround  = Prim.Emissive("Screen Ground", new Color(0.5f, 0.3f, 0.12f), new Color(0.6f, 0.35f, 0.12f));
            screenNav     = Prim.Emissive("Screen Nav", new Color(0.02f, 0.05f, 0.04f), new Color(0.02f, 0.12f, 0.06f));
            screenEngine  = Prim.Emissive("Screen Engine", new Color(0.03f, 0.04f, 0.08f), new Color(0.05f, 0.1f, 0.25f));
            screenLine    = Prim.Emissive("Screen Line", Color.white, new Color(1.2f, 1.2f, 1.2f));
            magenta       = Prim.Emissive("Screen Magenta", new Color(1f, 0.2f, 0.9f), new Color(1.2f, 0.2f, 1f));
            amber         = Prim.Emissive("Caution Amber", new Color(1f, 0.6f, 0.1f), new Color(2f, 1.1f, 0.1f));
            buttonGreen   = Prim.Emissive("Button Green", new Color(0.2f, 1f, 0.4f), new Color(0.2f, 1f, 0.35f));
            pedestalMat   = Prim.Mat("Pedestal", new Color(0.22f, 0.23f, 0.25f), 0.2f, 0.35f);
            leverKnob     = Prim.Mat("Thrust Lever Knob", new Color(0.85f, 0.85f, 0.85f), 0.3f, 0.6f);
            pilotSeatMat  = Prim.Mat("Pilot Seat", new Color(0.12f, 0.12f, 0.13f), 0f, 0.2f);
            stickMat      = Prim.Mat("Sidestick", new Color(0.1f, 0.1f, 0.1f), 0.2f, 0.5f);
        }

        // ----------------------------------------------------------------------- cabin

        static void Shell(Transform t)
        {
            float rear = C.RearWallX, front = C.BulkheadX, hw = C.HalfWidth, ceil = C.CeilingY;
            float length = front - rear, mid = (front + rear) * 0.5f;

            Prim.Box(t, "Floor", new Vector3(mid, -0.05f, 0f), new Vector3(length, 0.1f, hw * 2f), floorMat, true);
            Prim.Box(t, "Aisle Carpet", new Vector3(mid, 0.005f, 0f), new Vector3(length, 0.01f, C.AisleHalfWidth * 2f + 0.1f), aisleCarpet);
            Prim.Box(t, "Ceiling", new Vector3(mid, ceil + 0.05f, 0f), new Vector3(length, 0.1f, hw * 2f), ceilingMat, true);

            Prim.Box(t, "Rear Wall", new Vector3(rear - 0.05f, ceil * 0.5f, 0f), new Vector3(0.1f, ceil, hw * 2f), wallPanel, true);
            foreach (int side in new[] { -1, 1 })
                Prim.Box(t, "Lavatory Door", new Vector3(rear + 0.01f, 1f, side * 0.9f), new Vector3(0.04f, 2f, 0.8f), doorMat);

            // Read by someone walking towards the back (looking along -X).
            Prim.Text3D(t, "Lavatory Sign", new Vector3(rear + 0.04f, 2.1f, 0f), new Vector3(0f, -90f, 0f), "LAVATORY", 0.012f, new Color(0.2f, 0.2f, 0.25f));
        }

        /// <summary>
        /// Each side wall is a solid strip below the windows, a solid strip above, and posts
        /// between the windows. Every row of seats gets its own window, and each gap is filled
        /// with a pane of glass.
        /// </summary>
        static void SideWalls(Transform t)
        {
            float rear = C.RearWallX, front = C.BulkheadX, ceil = C.CeilingY;
            float length = front - rear, mid = (front + rear) * 0.5f;
            float winHeight = C.WindowTopY - C.WindowBottomY, winY = (C.WindowTopY + C.WindowBottomY) * 0.5f;

            foreach (int side in new[] { -1, 1 })
            {
                float z = side * (C.HalfWidth + 0.05f);

                Prim.Box(t, "Wall Below Windows", new Vector3(mid, C.WindowBottomY * 0.5f, z), new Vector3(length, C.WindowBottomY, 0.1f), wallPanel, true);
                Prim.Box(t, "Wall Above Windows", new Vector3(mid, (C.WindowTopY + ceil) * 0.5f, z), new Vector3(length, ceil - C.WindowTopY, 0.1f), wallPanel, true);

                float postFrom = rear;
                for (int row = C.Rows - 1; row >= 0; row--)
                {
                    float wx = C.FirstRowX - row * C.RowPitch + 0.1f;
                    float left = wx - C.WindowWidth * 0.5f;

                    WallPost(t, postFrom, left, winY, winHeight, z);
                    Prim.NoShadow(Prim.Box(t, "Window", new Vector3(wx, winY, side * (C.HalfWidth + 0.02f)),
                                           new Vector3(C.WindowWidth, winHeight, 0.02f), glass));
                    postFrom = wx + C.WindowWidth * 0.5f;
                }
                WallPost(t, postFrom, front, winY, winHeight, z);
            }

            // Overhead bins over both seat blocks.
            float binFrom = C.FirstRowX - (C.Rows - 1) * C.RowPitch - 0.4f, binTo = C.FirstRowX + 0.4f;
            foreach (int side in new[] { -1, 1 })
            {
                Prim.Box(t, "Overhead Bins", new Vector3((binFrom + binTo) * 0.5f, 1.95f, side * 1.55f), new Vector3(binTo - binFrom, 0.42f, 0.8f), binMat);
                Prim.Box(t, "Overhead Bin Lip", new Vector3((binFrom + binTo) * 0.5f, 1.75f, side * 1.16f), new Vector3(binTo - binFrom, 0.03f, 0.04f), binLip);
            }
        }

        static void WallPost(Transform t, float xFrom, float xTo, float y, float height, float z)
        {
            if (xTo - xFrom < 0.001f) return;
            Prim.Box(t, "Wall Between Windows", new Vector3((xFrom + xTo) * 0.5f, y, z), new Vector3(xTo - xFrom, height, 0.1f), wallPanel, true);
        }

        /// <summary>
        /// Twenty rows of 3+3 seats, all facing the nose (+X). The seats themselves have no
        /// colliders. One invisible block over each side's seats keeps you in the aisle.
        /// </summary>
        static void Seats(Transform t)
        {
            for (int row = 0; row < C.Rows; row++)
            {
                float x = C.FirstRowX - row * C.RowPitch;
                foreach (float z in C.SeatZ) Seat(t, new Vector3(x, 0f, z));

                // Reading-light panel under the bins for each row.
                foreach (int side in new[] { -1, 1 })
                    Prim.NoShadow(Prim.Box(t, "Reading Lights", new Vector3(x - 0.1f, 1.73f, side * 1.4f), new Vector3(0.25f, 0.02f, 0.45f), readingLight));
            }

            float from = C.FirstRowX - (C.Rows - 1) * C.RowPitch - 0.4f, to = C.FirstRowX + 0.4f;
            float blockWidth = (C.HalfWidth - 0.16f) - C.AisleHalfWidth;
            foreach (int side in new[] { -1, 1 })
                Prim.Blocker(t, "Seat Block Collider", new Vector3((from + to) * 0.5f, 0.55f, side * (C.AisleHalfWidth + blockWidth * 0.5f)),
                             new Vector3(to - from, 1.1f, blockWidth));
        }

        static void Seat(Transform t, Vector3 pos)
        {
            var s = Prim.Empty(t, "Seat", pos).transform;
            float w = C.SeatWidth;

            Prim.Box(s, "Base", new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 0.4f, 0.06f), seatShell);
            Prim.Box(s, "Cushion", new Vector3(0.02f, 0.45f, 0f), new Vector3(0.46f, 0.1f, w - 0.03f), seatFabric);
            // Tilting around Z leans the backrest back towards -X, like a slightly reclined seat.
            Prim.Box(s, "Backrest", new Vector3(-0.24f, 0.8f, 0f), new Vector3(0.1f, 0.72f, w - 0.03f), new Vector3(0f, 0f, 8f), seatFabric);
            Prim.Box(s, "Headrest Cover", new Vector3(-0.28f, 1.1f, 0f), new Vector3(0.11f, 0.18f, w - 0.06f), new Vector3(0f, 0f, 8f), headrestCover);
            Prim.Box(s, "Armrest", new Vector3(-0.02f, 0.62f, w * 0.5f), new Vector3(0.4f, 0.04f, 0.04f), seatShell);
        }

        /// <summary>The space between row 1 and the cockpit: boarding door on the left, galley on the right, and the bulkhead.</summary>
        static void FrontOfCabin(Transform t)
        {
            float hw = C.HalfWidth, ceil = C.CeilingY;

            // Boarding door, on the inside of the left wall.
            Prim.Box(t, "Boarding Door", new Vector3(C.BoardingDoorX, 1f, -hw + 0.01f), new Vector3(1f, 2f, 0.04f), doorMat);
            Prim.Box(t, "Boarding Door Handle", new Vector3(C.BoardingDoorX, 1.05f, -hw + 0.05f), new Vector3(0.5f, 0.05f, 0.05f), galleyMat);
            Prim.NoShadow(Prim.Box(t, "Exit Sign", new Vector3(C.BoardingDoorX, 2.1f, -hw + 0.04f), new Vector3(0.45f, 0.14f, 0.02f), exitSign));
            Prim.Text3D(t, "Exit Sign Text", new Vector3(C.BoardingDoorX, 2.1f, -hw + 0.06f), new Vector3(0f, 180f, 0f), "EXIT", 0.012f, Color.white);

            // Galley, on the right.
            Prim.Box(t, "Galley Counter", new Vector3(5.2f, 0.5f, 1.5f), new Vector3(2.6f, 1f, 0.9f), galleyMat, true);
            Prim.Box(t, "Galley Worktop", new Vector3(5.2f, 1.02f, 1.45f), new Vector3(2.6f, 0.04f, 1f), worktop);
            Prim.Box(t, "Galley Cabinets", new Vector3(5.2f, 1.85f, 1.6f), new Vector3(2.6f, 0.7f, 0.7f), galleyMat);
            Prim.Box(t, "Galley Oven", new Vector3(4.6f, 1.85f, 1.24f), new Vector3(0.5f, 0.4f, 0.02f), panelBlack);
            Prim.Box(t, "Galley Coffee Maker", new Vector3(5.6f, 1.2f, 1.6f), new Vector3(0.35f, 0.32f, 0.35f), panelBlack);

            // Bulkhead: two wall sections either side of the cockpit doorway, and a lintel over it.
            float bx = C.BulkheadX, dh = C.CockpitDoorHalfWidth, sideWidth = hw - dh;
            Prim.Box(t, "Bulkhead Left", new Vector3(bx, ceil * 0.5f, -(dh + sideWidth * 0.5f)), new Vector3(0.1f, ceil, sideWidth), bulkheadMat, true);
            Prim.Box(t, "Bulkhead Right", new Vector3(bx, ceil * 0.5f, dh + sideWidth * 0.5f), new Vector3(0.1f, ceil, sideWidth), bulkheadMat, true);
            Prim.Box(t, "Bulkhead Lintel", new Vector3(bx, (2f + ceil) * 0.5f, 0f), new Vector3(0.1f, ceil - 2f, dh * 2f), bulkheadMat, true);

            // The cockpit door, swung open against the left side of the doorway.
            Prim.Box(t, "Cockpit Door (open)", new Vector3(bx + 0.42f, 1f, -dh - 0.03f), new Vector3(0.8f, 2f, 0.05f), doorMat);
            Prim.Text3D(t, "Flight Deck Sign", new Vector3(bx - 0.07f, 2.12f, 0f), new Vector3(0f, 90f, 0f), "FLIGHT DECK", 0.01f, new Color(0.2f, 0.2f, 0.25f));
        }

        static void Lights(Transform t)
        {
            float from = C.RearWallX, to = C.BulkheadX;

            foreach (int side in new[] { -1, 1 })
                Prim.NoShadow(Prim.Box(t, "Ceiling Light Strip", new Vector3((from + to) * 0.5f, C.CeilingY - 0.02f, side * 0.55f),
                                       new Vector3(to - from - 0.4f, 0.03f, 0.12f), lightStrip));

            foreach (float x in new[] { -10f, -6f, -2f, 2f, 5.5f })
                SceneKit.PointLight(t, "Cabin Light", new Vector3(x, 2.05f, 0f), new Color(1f, 0.93f, 0.82f), 1f, 5f);
        }

        // --------------------------------------------------------------------- cockpit

        static void Cockpit(Transform k)
        {
            float x0 = C.BulkheadX, x1 = C.NoseX, hw = C.CockpitHalfWidth, ceil = C.CockpitCeilingY;
            float length = x1 - x0, mid = (x0 + x1) * 0.5f;

            Prim.Box(k, "Floor", new Vector3(mid, -0.05f, 0f), new Vector3(length, 0.1f, hw * 2f), cockpitFloor, true);
            Prim.Box(k, "Ceiling", new Vector3(mid, ceil + 0.05f, 0f), new Vector3(length, 0.1f, hw * 2f), cockpitGrey, true);

            // Side walls, with a side window near the front.
            const float sideWindowFrom = 9f;
            foreach (int side in new[] { -1, 1 })
            {
                float z = side * (hw + 0.05f);
                Prim.Box(k, "Side Wall Lower", new Vector3(mid, 0.55f, z), new Vector3(length, 1.1f, 0.1f), cockpitGrey, true);
                Prim.Box(k, "Side Wall Upper", new Vector3(mid, (1.8f + ceil) * 0.5f, z), new Vector3(length, ceil - 1.8f, 0.1f), cockpitGrey, true);
                Prim.Box(k, "Side Wall Middle", new Vector3((x0 + sideWindowFrom) * 0.5f, 1.45f, z), new Vector3(sideWindowFrom - x0, 0.7f, 0.1f), cockpitGrey, true);
                Prim.NoShadow(Prim.Box(k, "Side Window", new Vector3((sideWindowFrom + x1) * 0.5f, 1.45f, z), new Vector3(x1 - sideWindowFrom, 0.7f, 0.02f), glass));
            }

            // The nose: solid below and above the windscreen.
            Prim.Box(k, "Nose Below Windscreen", new Vector3(x1 + 0.05f, 0.525f, 0f), new Vector3(0.1f, 1.05f, hw * 2f), cockpitGrey, true);
            Prim.Box(k, "Nose Above Windscreen", new Vector3(x1 + 0.05f, (1.85f + ceil) * 0.5f, 0f), new Vector3(0.1f, ceil - 1.85f, hw * 2f), cockpitGrey, true);
            Prim.NoShadow(Prim.Box(k, "Windscreen", new Vector3(x1 + 0.05f, 1.45f, 0f), new Vector3(0.02f, 0.8f, hw * 2f), glass));
            Prim.Box(k, "Windscreen Centre Post", new Vector3(x1 + 0.03f, 1.45f, 0f), new Vector3(0.08f, 0.8f, 0.08f), cockpitGrey);

            InstrumentPanel(k);
            ThrustLevers(k);

            foreach (int side in new[] { -1, 1 })
            {
                PilotSeat(k, new Vector3(C.PilotSeatX, 0f, side * C.PilotSeatZ));

                // A320s have a sidestick on the outer console instead of a yoke in front of the pilot.
                Prim.Box(k, "Side Console", new Vector3(8.4f, 0.35f, side * 1.35f), new Vector3(1.2f, 0.7f, 0.4f), pedestalMat);
                Prim.Cyl(k, "Sidestick", new Vector3(8.6f, 0.8f, side * 1.35f), 0.025f, 0.2f, new Vector3(0f, 0f, -10f), stickMat);
            }

            // Overhead switch panel with a few lit buttons.
            Prim.Box(k, "Overhead Panel", new Vector3(9.2f, ceil - 0.05f, 0f), new Vector3(1.6f, 0.08f, 1.2f), panelBlack);
            for (int i = 0; i < 6; i++)
                Prim.NoShadow(Prim.Box(k, "Overhead Button", new Vector3(8.7f + i * 0.2f, ceil - 0.1f, (i % 2 == 0 ? -0.3f : 0.3f)),
                                       new Vector3(0.05f, 0.02f, 0.05f), i == 4 ? amber : buttonGreen));

            SceneKit.PointLight(k, "Cockpit Light", new Vector3(8.4f, 1.9f, 0f), new Color(1f, 0.93f, 0.82f), 0.6f, 3f);
        }

        /// <summary>
        /// The main panel, tilted back towards the pilots. The screens are children of a tilted
        /// pivot, so they sit flat on the panel without each needing its own rotation. Screen
        /// faces point along local -X, towards the seats.
        /// </summary>
        static void InstrumentPanel(Transform k)
        {
            Prim.Box(k, "Glareshield", new Vector3(C.PanelX + 0.15f, 1.33f, 0f), new Vector3(0.5f, 0.08f, 2.8f), glareshield);

            var panel = Prim.Empty(k, "Instrument Panel", new Vector3(C.PanelX, 0.98f, 0f), new Vector3(0f, 0f, 18f)).transform;
            Prim.Box(panel, "Panel Face", Vector3.zero, new Vector3(0.12f, 0.62f, 2.9f), panelBlack);

            foreach (int side in new[] { -1, 1 })
            {
                // Primary flight display: the artificial horizon, blue sky over brown ground.
                float pfd = side * 1.05f;
                Prim.NoShadow(Prim.Box(panel, "PFD Sky", new Vector3(-0.07f, 0.08f, pfd), new Vector3(0.01f, 0.16f, 0.42f), screenSky));
                Prim.NoShadow(Prim.Box(panel, "PFD Ground", new Vector3(-0.07f, -0.08f, pfd), new Vector3(0.01f, 0.16f, 0.42f), screenGround));
                Prim.NoShadow(Prim.Box(panel, "PFD Horizon", new Vector3(-0.076f, 0f, pfd), new Vector3(0.005f, 0.012f, 0.3f), screenLine));

                // Navigation display: dark screen with the planned route in magenta.
                float nd = side * 0.42f;
                Prim.NoShadow(Prim.Box(panel, "Nav Display", new Vector3(-0.07f, 0f, nd), new Vector3(0.01f, 0.32f, 0.42f), screenNav));
                Prim.NoShadow(Prim.Box(panel, "Nav Route", new Vector3(-0.076f, 0.03f, nd), new Vector3(0.005f, 0.2f, 0.012f), magenta));
                Prim.NoShadow(Prim.Box(panel, "Nav Aircraft", new Vector3(-0.076f, -0.1f, nd), new Vector3(0.005f, 0.02f, 0.06f), amber));
            }

            // Engine display in the middle.
            Prim.NoShadow(Prim.Box(panel, "Engine Display", new Vector3(-0.07f, 0f, 0f), new Vector3(0.01f, 0.32f, 0.3f), screenEngine));
            foreach (float z in new[] { -0.07f, 0.07f })
                Prim.NoShadow(Prim.Box(panel, "Engine Gauge", new Vector3(-0.076f, 0.07f, z), new Vector3(0.005f, 0.08f, 0.08f), buttonGreen));

            // Master caution light, flashing.
            SceneKit.Blinker(panel, "Master Caution", new Vector3(-0.08f, 0.26f, -0.75f), 0.035f, amber, Color.black, 0f, 0.5f, 0.5f, 0f);
        }

        static void ThrustLevers(Transform k)
        {
            Vector3 c = C.ThrottleCentre;

            Prim.Box(k, "Pedestal", c + new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.36f), pedestalMat, true);
            Prim.Box(k, "Pedestal Top", c + new Vector3(0f, 0.82f, 0f), new Vector3(0.8f, 0.05f, 0.36f), new Vector3(0f, 0f, -8f), panelBlack);

            foreach (float z in new[] { -0.07f, 0.07f })
            {
                // Tilting around -Z leans the lever forward, towards the nose.
                Prim.Box(k, "Thrust Lever", c + new Vector3(-0.05f, 0.97f, z), new Vector3(0.04f, 0.26f, 0.035f), new Vector3(0f, 0f, -20f), pedestalMat);
                Prim.Box(k, "Thrust Lever Knob", c + new Vector3(0f, 1.09f, z), new Vector3(0.07f, 0.05f, 0.06f), leverKnob);
            }

            Prim.Text3D(k, "Thrust Label", c + new Vector3(-0.41f, 0.62f, 0f), new Vector3(0f, 90f, 0f), "THRUST", 0.008f, Color.white);
        }

        static void PilotSeat(Transform k, Vector3 pos)
        {
            var s = Prim.Empty(k, "Pilot Seat", pos).transform;
            Prim.Cyl(s, "Column", new Vector3(0f, 0.22f, 0f), 0.12f, 0.44f, Prim.AxisY, pedestalMat);
            Prim.Box(s, "Cushion", new Vector3(0.05f, 0.48f, 0f), new Vector3(0.5f, 0.1f, 0.5f), pilotSeatMat);
            Prim.Box(s, "Backrest", new Vector3(-0.24f, 0.9f, 0f), new Vector3(0.1f, 0.8f, 0.5f), new Vector3(0f, 0f, 10f), pilotSeatMat);
            Prim.Box(s, "Headrest", new Vector3(-0.31f, 1.37f, 0f), new Vector3(0.1f, 0.22f, 0.36f), new Vector3(0f, 0f, 10f), pilotSeatMat);
            foreach (int side in new[] { -1, 1 })
                Prim.Box(s, "Armrest", new Vector3(-0.05f, 0.7f, side * 0.27f), new Vector3(0.32f, 0.04f, 0.06f), pedestalMat);
        }

        // --------------------------------------------------------------------- outside

        static void Outside(Transform o)
        {
            float g = C.GroundY;

            Prim.Box(o, "Grass", new Vector3(0f, g - 0.1f, 0f), new Vector3(3000f, 0.2f, 3000f), SharedParts.Grass);
            Prim.Box(o, "Apron", new Vector3(-20f, g + 0.01f, -10f), new Vector3(160f, 0.02f, 70f), SharedParts.Concrete);
            Prim.Box(o, "Taxi Line", new Vector3(0f, g + 0.025f, 0f), new Vector3(80f, 0.01f, 0.3f), SharedParts.LineYellow);

            // The runway starts just ahead of the nose, so the windscreen looks straight down it.
            SharedParts.Runway(o, new Vector3(700f, g, 0f), 1300f, 45f);

            // The terminal on the left, with the jet bridge running out to the boarding door.
            Prim.Box(o, "Terminal Building", new Vector3(-20f, g + 5f, -40f), new Vector3(90f, 10f, 24f), SharedParts.Facade);
            Prim.Box(o, "Terminal Windows", new Vector3(-20f, g + 6.2f, -27.95f), new Vector3(88f, 3.6f, 0.1f), SharedParts.PlaneWindows);
            Prim.Box(o, "Terminal Roof", new Vector3(-20f, g + 10.3f, -39.5f), new Vector3(92f, 0.6f, 26f), SharedParts.Roof);
            SharedParts.JetBridge(o, C.BoardingDoorX, -27.9f, -C.HalfWidth - 0.1f, 0f, g);

            // This plane's own wings and engines, which you can see from the windows.
            foreach (int side in new[] { -1, 1 })
            {
                SharedParts.Wing(o, "Wing", new Vector3(1f, -1.1f, 0f), side, 1.9f, 17f, 5.5f, 22f, true);
                SharedParts.Engine(o, "Engine", new Vector3(2.4f, -2f, side * 5.5f), 0.95f, 4f);
            }
        }

        static void Passengers(Transform t)
        {
            // Fixed seed, so the same seats are filled every time the scene is rebuilt.
            var random = new System.Random(204);
            int look = 0;

            for (int row = 0; row < C.Rows; row++)
            {
                foreach (float z in C.SeatZ)
                {
                    if (random.NextDouble() > 0.14) continue;
                    Figures.Seated(t, "Passenger", new Vector3(C.FirstRowX - row * C.RowPitch, 0f, z), 90f, look++);
                }
            }

            Figures.Standing(t, "Flight Attendant", new Vector3(3.9f, 0f, 0.85f), -110f, 5);
        }
    }
}
