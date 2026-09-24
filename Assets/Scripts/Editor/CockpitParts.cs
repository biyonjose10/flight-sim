using UnityEngine;
using C = FlightSim.FlightLayout.Cabin;

namespace FlightSim.Build
{
    /// <summary>
    /// The inside of the cockpit, in one place.
    ///
    /// You walk into this cockpit in scene 2, and you sit in it in scenes 3 and 4, so it cannot
    /// live inside any one scene's builder. Every builder calls Cockpit(...) and gets the same
    /// room: the floor and walls, the windscreen, the instrument panel, the thrust levers, the
    /// two pilot seats and the overhead switches.
    ///
    /// It is built in the cabin's own coordinates - the nose at x = 10.4, the floor at y = 0 - and
    /// scenes 3 and 4 park the whole thing inside the aeroplane's shell using
    /// FlightLayout.Plane.CockpitOffset, so what you see out of the windscreen lines up with the
    /// cockpit windows you can see from outside.
    ///
    /// The panel is not a painting. Cockpit(...) also attaches a LiveInstruments component and
    /// hands it the horizons, the engine bars, the readouts and the gear lights, so in scenes 3
    /// and 4 the instruments follow the aeroplane. In scene 2 there is no aeroplane, and
    /// LiveInstruments quietly does nothing.
    /// </summary>
    public static class CockpitParts
    {
        // ------------------------------------------------------------------ panel geometry

        // The panel screens are flat boxes lying on a pivot tilted 18 degrees back towards the
        // pilots. In that pivot's own coordinates the boxes are thin in X and their faces look
        // along local -X, towards the seats. So "further from the pilot" is a bigger (less
        // negative) local X, and "closer to the pilot" is a smaller (more negative) one. Every
        // number below is a local X on that pivot, in metres.

        /// <summary>The plane the flat screens themselves sit on, a centimetre proud of the panel.</summary>
        const float ScreenX = -0.07f;

        /// <summary>Markings drawn on top of a screen: the horizon line, the route, the bars.</summary>
        const float MarkingX = -0.076f;

        /// <summary>The bezel, the nearest thing to the pilot, so it hides what passes behind it.</summary>
        const float BezelX = -0.084f;

        /// <summary>
        /// Half the height and half the width of the sky/ground card that rolls behind the bezel.
        ///
        /// This is the fix for the obvious problem with a rotating square: turn a box on a flat
        /// panel and its corners swing out past the edge of the screen and float about on the
        /// black plastic. The card is therefore built much bigger than the hole you look through
        /// (see ApertureHalfHeight/Width), so that at the steepest bank the aeroplane can manage -
        /// 55 degrees, from FlightLayout.Flight.RollMax - plus the biggest pitch slide, the card
        /// still covers every corner of the hole. The far corner of the hole ends up about 0.24 m
        /// from the middle of the card, and the card reaches 0.25 m, so it always wins.
        /// </summary>
        const float CardHalf = 0.25f;

        /// <summary>Half the height of the hole you actually see the horizon through.</summary>
        const float ApertureHalfHeight = 0.13f;

        /// <summary>Half the width of that hole.</summary>
        const float ApertureHalfWidth = 0.16f;

        /// <summary>
        /// How far out the bezel reaches. A little wider than the card, so the card's corners are
        /// always behind plastic and never past the edge of it.
        /// </summary>
        const float BezelOuter = 0.26f;

        /// <summary>How thick the bezel bars are, front to back.</summary>
        const float BezelThickness = 0.006f;

        /// <summary>An engine bar at full power. LiveInstruments shrinks it from here.</summary>
        const float GaugeFullHeight = 0.24f;

        /// <summary>
        /// How the readouts on the panel are turned.
        ///
        /// Prim.Text3D writes text that faces -Z, which reads correctly to somebody looking
        /// towards +Z. A pilot in this cockpit looks towards +X, so the text is turned 90 degrees
        /// about Y: that swings its face round to point along the panel's local -X (at the seats)
        /// and leaves its "up" along the panel's local +Y (up the slope of the panel). The 18
        /// degree tilt of the panel comes along for free, because the text is a child of it, so
        /// the writing lies flat on the panel exactly like the screens do.
        /// </summary>
        static readonly Vector3 PanelTextFacing = new Vector3(0f, 90f, 0f);

        // --------------------------------------------------------------------- the materials

        static Material cockpitGrey, cockpitFloor, panelBlack, glareshield, screenSky, screenGround,
                        screenNav, screenEngine, screenLine, magenta, amber, buttonGreen, pedestalMat,
                        leverKnob, pilotSeatMat, stickMat, glass;

        /// <summary>
        /// The cockpit's own palette. The names matter more than the colours: Prim looks materials
        /// up by name and updates the one asset it finds, so "Panel Black" here and "Panel Black"
        /// in CabinBuilder are the same material file, shared by every scene.
        /// </summary>
        static void CreateMaterials()
        {
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
            glass         = Prim.Glass("Cabin Window Glass", new Color(0.8f, 0.9f, 1f, 0.12f));
        }

        // ----------------------------------------------------------------------- the cockpit

        /// <summary>
        /// Builds the whole cockpit under the transform you hand it, and wires up the instruments.
        ///
        /// Pass it a fresh empty object. In scene 2 that object sits at the origin, because the
        /// cabin is already in these coordinates; in scenes 3 and 4 it is parked inside the
        /// aeroplane by FlightLayout.Plane.CockpitOffset.
        /// </summary>
        public static void Cockpit(Transform k)
        {
            CreateMaterials();

            // The component that will drive the panel. It is created first so the builders below
            // can hand it each piece as they make it, and it lives on the cockpit's own root, so
            // it goes wherever the cockpit goes.
            var live = k.gameObject.AddComponent<LiveInstruments>();
            live.gearDownMaterial = buttonGreen;
            live.gearUpMaterial = amber;

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

            InstrumentPanel(k, live);
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
        ///
        /// While it builds, it hands each moving piece to LiveInstruments: the two artificial
        /// horizons, the two engine bars, the three readouts and the gear lights.
        /// </summary>
        static void InstrumentPanel(Transform k, LiveInstruments live)
        {
            Prim.Box(k, "Glareshield", new Vector3(C.PanelX + 0.15f, 1.33f, 0f), new Vector3(0.5f, 0.08f, 2.8f), glareshield);

            var panel = Prim.Empty(k, "Instrument Panel", new Vector3(C.PanelX, 0.98f, 0f), new Vector3(0f, 0f, 18f)).transform;
            Prim.Box(panel, "Panel Face", Vector3.zero, new Vector3(0.12f, 0.62f, 2.9f), panelBlack);

            var horizons = new Transform[2];
            int slot = 0;

            foreach (int side in new[] { -1, 1 })
            {
                // Primary flight display: the artificial horizon, blue sky over brown ground.
                float pfd = side * 1.05f;
                horizons[slot] = ArtificialHorizon(panel, pfd);
                slot++;

                // Navigation display: dark screen with the planned route in magenta.
                float nd = side * 0.42f;
                Prim.NoShadow(Prim.Box(panel, "Nav Display", new Vector3(ScreenX, 0f, nd), new Vector3(0.01f, 0.32f, 0.42f), screenNav));
                Prim.NoShadow(Prim.Box(panel, "Nav Route", new Vector3(MarkingX, 0.03f, nd), new Vector3(0.005f, 0.2f, 0.012f), magenta));
                Prim.NoShadow(Prim.Box(panel, "Nav Aircraft", new Vector3(MarkingX, -0.1f, nd), new Vector3(0.005f, 0.02f, 0.06f), amber));
            }

            live.horizonPivots = horizons;
            live.engineGauges = EngineDisplay(panel);
            live.gearLights = GearLights(panel);

            live.speedText = Readout(panel, "Speed Readout", -0.55f, "IAS --- KT");
            live.altitudeText = Readout(panel, "Altitude Readout", 0f, "ALT ----- FT");
            live.headingText = Readout(panel, "Heading Readout", 0.55f, "HDG ---");

            // Master caution light, flashing.
            SceneKit.Blinker(panel, "Master Caution", new Vector3(-0.08f, 0.26f, -0.75f), 0.035f, amber, Color.black, 0f, 0.5f, 0.5f, 0f);
        }

        /// <summary>
        /// One pilot's artificial horizon, and the frame that keeps it tidy.
        ///
        /// Everything that moves - sky, ground and the white horizon line - hangs off a single
        /// empty pivot in the middle of the screen, which is the transform LiveInstruments turns
        /// and slides. The sky and ground are deliberately far bigger than the screen you look at,
        /// so that no matter how far the picture rolls or slides, there is still card underneath
        /// every part of the hole.
        ///
        /// That leaves the opposite problem: the card is now much too big and its corners would
        /// hang out over the black plastic. So the last thing built here is a bezel - four thin
        /// bars of the same panel black, sitting a few millimetres nearer the pilot than the card.
        /// They frame the screen and hide everything outside the frame. A real aeroplane's display
        /// is set into the panel in exactly this way, which is why it looks right rather than like
        /// a patch.
        /// </summary>
        static Transform ArtificialHorizon(Transform panel, float z)
        {
            var pivot = Prim.Empty(panel, "Artificial Horizon", new Vector3(ScreenX, 0f, z)).transform;

            // Sky above the line, ground below it, each half the card tall and the full card wide.
            Prim.NoShadow(Prim.Box(pivot, "PFD Sky", new Vector3(0f, CardHalf * 0.5f, 0f),
                                   new Vector3(0.01f, CardHalf, CardHalf * 2f), screenSky));
            Prim.NoShadow(Prim.Box(pivot, "PFD Ground", new Vector3(0f, -CardHalf * 0.5f, 0f),
                                   new Vector3(0.01f, CardHalf, CardHalf * 2f), screenGround));

            // The white line where they meet, a shade nearer the pilot so it is never hidden by
            // the card. Its own X is measured from the pivot, which sits at ScreenX.
            Prim.NoShadow(Prim.Box(pivot, "PFD Horizon", new Vector3(MarkingX - ScreenX, 0f, 0f),
                                   new Vector3(0.005f, 0.012f, CardHalf * 1.84f), screenLine));

            // The bezel is a child of the PANEL, not the pivot, because it must stay still while
            // everything behind it moves.
            float barHeight = BezelOuter - ApertureHalfHeight;
            float barY = (BezelOuter + ApertureHalfHeight) * 0.5f;
            Prim.Box(panel, "PFD Bezel Top", new Vector3(BezelX, barY, z),
                     new Vector3(BezelThickness, barHeight, BezelOuter * 2f), panelBlack);
            Prim.Box(panel, "PFD Bezel Bottom", new Vector3(BezelX, -barY, z),
                     new Vector3(BezelThickness, barHeight, BezelOuter * 2f), panelBlack);

            // The two side bars only have to reach the top and bottom bars, which already cover
            // the corners between them.
            float barWidth = BezelOuter - ApertureHalfWidth;
            float barZ = (BezelOuter + ApertureHalfWidth) * 0.5f;
            foreach (int side in new[] { -1, 1 })
                Prim.Box(panel, "PFD Bezel Side", new Vector3(BezelX, 0f, z + side * barZ),
                         new Vector3(BezelThickness, ApertureHalfHeight * 2f, barWidth), panelBlack);

            return pivot;
        }

        /// <summary>
        /// The engine display in the middle of the panel: one bar per engine, built at full power
        /// so LiveInstruments can measure what full power looks like and shrink from there.
        /// </summary>
        static Transform[] EngineDisplay(Transform panel)
        {
            Prim.NoShadow(Prim.Box(panel, "Engine Display", new Vector3(ScreenX, 0f, 0f), new Vector3(0.01f, 0.32f, 0.3f), screenEngine));

            var bars = new Transform[2];
            int slot = 0;

            foreach (float z in new[] { -0.07f, 0.07f })
            {
                var bar = Prim.NoShadow(Prim.Box(panel, "Engine Gauge", new Vector3(MarkingX, 0f, z),
                                                 new Vector3(0.005f, GaugeFullHeight, 0.06f), buttonGreen));
                bars[slot] = bar.transform;
                slot++;
            }

            return bars;
        }

        /// <summary>
        /// Three landing-gear lights along the top of the panel, one per leg, plus a label so a
        /// visitor knows what they are. LiveInstruments swaps them between green and amber.
        /// </summary>
        static Renderer[] GearLights(Transform panel)
        {
            var lights = new Renderer[3];

            for (int i = 0; i < lights.Length; i++)
            {
                var bulb = Prim.NoShadow(Prim.Box(panel, "Gear Light", new Vector3(MarkingX, 0.235f, (i - 1) * 0.06f),
                                                  new Vector3(0.005f, 0.03f, 0.03f), buttonGreen));
                lights[i] = bulb.GetComponent<Renderer>();
            }

            Prim.Text3D(panel, "Gear Label", new Vector3(-0.072f, 0.185f, 0f), PanelTextFacing,
                        "GEAR", 0.004f, new Color(0.7f, 0.72f, 0.7f));

            return lights;
        }

        /// <summary>
        /// One numeric readout along the bottom of the panel. It starts showing dashes, which is
        /// what you see in scene 2, where there is no aeroplane for LiveInstruments to follow.
        /// </summary>
        static TextMesh Readout(Transform panel, string name, float z, string placeholder)
        {
            var go = Prim.Text3D(panel, name, new Vector3(-0.072f, -0.235f, z), PanelTextFacing,
                                 placeholder, 0.005f, new Color(0.85f, 0.9f, 0.85f));

            return go == null ? null : go.GetComponent<TextMesh>();
        }

        // ------------------------------------------------------- the rest of the flight deck

        /// <summary>The throttle pedestal between the two seats: the levers you push for take-off.</summary>
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

        /// <summary>One pilot's seat: a pedestal, a cushion, a reclined back and two armrests.</summary>
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
    }
}
