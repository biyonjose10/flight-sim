using UnityEngine;

namespace FlightSim.Build
{
    /// <summary>
    /// Pieces that appear in both scenes: the plane's wings and engines, the runway, the jet
    /// bridge, and their materials. Scene 1 sees them from the terminal window and scene 2 sees
    /// them from the cabin windows, so they're built by the same code and look the same in both.
    /// </summary>
    public static class SharedParts
    {
        // ------------------------------------------------------------------ materials

        public static Material PlaneWhite   { get { return Prim.Mat("Plane White", new Color(0.94f, 0.95f, 0.96f), 0.1f, 0.6f); } }
        public static Material PlaneBlue    { get { return Prim.Mat("Plane Blue", new Color(0.1f, 0.25f, 0.6f), 0.1f, 0.6f); } }
        public static Material PlaneWindows { get { return Prim.Mat("Plane Windows", new Color(0.08f, 0.1f, 0.14f), 0.2f, 0.85f); } }
        public static Material EngineCowl   { get { return Prim.Mat("Engine Cowl", new Color(0.8f, 0.82f, 0.85f), 0.5f, 0.6f); } }
        public static Material Intake       { get { return Prim.Mat("Engine Intake", new Color(0.05f, 0.05f, 0.06f)); } }
        public static Material GearMetal    { get { return Prim.Mat("Gear Metal", new Color(0.5f, 0.5f, 0.52f), 0.6f, 0.4f); } }
        public static Material Tyre         { get { return Prim.Mat("Tyre", new Color(0.07f, 0.07f, 0.07f), 0f, 0.1f); } }
        public static Material BeaconRed    { get { return Prim.Emissive("Beacon Red", new Color(1f, 0.15f, 0.1f), new Color(4f, 0.3f, 0.2f)); } }

        public static Material Grass      { get { return Prim.Mat("Grass", new Color(0.36f, 0.5f, 0.26f), 0f, 0.05f); } }
        public static Material Concrete   { get { return Prim.Mat("Apron Concrete", new Color(0.64f, 0.64f, 0.62f), 0f, 0.15f); } }
        public static Material Tarmac     { get { return Prim.Mat("Runway Tarmac", new Color(0.2f, 0.2f, 0.21f), 0f, 0.1f); } }
        public static Material LineWhite  { get { return Prim.Mat("Line White", new Color(0.95f, 0.95f, 0.95f)); } }
        public static Material LineYellow { get { return Prim.Mat("Line Yellow", new Color(0.95f, 0.78f, 0.15f)); } }
        public static Material EdgeLight  { get { return Prim.Emissive("Runway Edge Light", Color.white, new Color(1.4f, 1.25f, 0.9f)); } }
        public static Material Bridge     { get { return Prim.Mat("Jet Bridge", new Color(0.72f, 0.74f, 0.77f), 0.3f, 0.4f); } }
        public static Material Facade     { get { return Prim.Mat("Terminal Facade", new Color(0.62f, 0.64f, 0.66f), 0.1f, 0.3f); } }
        public static Material Roof       { get { return Prim.Mat("Terminal Roof", new Color(0.42f, 0.44f, 0.47f), 0.2f, 0.3f); } }

        // ------------------------------------------------------------------ the plane

        /// <summary>
        /// One swept wing (or tailplane). "side" is +1 for the wing on the +Z side, -1 for the
        /// other. The wing is a flat box turned by the sweep angle so its tip sits further back
        /// than its root, which is what makes it read as a jet rather than a propeller plane.
        /// </summary>
        public static void Wing(Transform parent, string name, Vector3 root, int side,
                                float inner, float span, float chord, float sweep, bool winglet)
        {
            float angle = -sweep * side;
            var turn = Quaternion.Euler(0f, angle, 0f);

            // The centre of the box is half a span out from the root, along the swept direction.
            Vector3 centre = root + turn * new Vector3(0f, 0f, side * (inner + span * 0.5f));
            Prim.Box(parent, name, centre, new Vector3(chord, 0.35f, span), new Vector3(0f, angle, 0f), PlaneWhite);

            if (winglet)
            {
                Vector3 tip = root + turn * new Vector3(0f, 0f, side * (inner + span));
                Prim.Box(parent, name + " Winglet", tip + new Vector3(-1.2f, 0.75f, 0f),
                         new Vector3(1.6f, 1.5f, 0.15f), new Vector3(0f, 0f, 25f), PlaneBlue);
            }
        }

        /// <summary>A jet engine lying along X, with its intake facing forward (+X).</summary>
        public static void Engine(Transform parent, string name, Vector3 centre, float radius, float length)
        {
            var e = Prim.Empty(parent, name, centre).transform;
            Prim.Cyl(e, "Cowl", Vector3.zero, radius, length, Prim.AxisX, EngineCowl);
            Prim.Cyl(e, "Intake", new Vector3(length * 0.5f + 0.01f, 0f, 0f), radius * 0.85f, 0.04f, Prim.AxisX, Intake);
            Prim.Cyl(e, "Exhaust", new Vector3(-length * 0.5f - 0.4f, 0f, 0f), radius * 0.55f, 0.8f, Prim.AxisX, GearMetal);
            Prim.Box(e, "Pylon", new Vector3(-0.3f, radius + 0.3f, 0f), new Vector3(length * 0.7f, 0.7f, 0.22f), PlaneWhite);
        }

        /// <summary>
        /// The whole A320-style aeroplane, nose towards +X, built around the middle of the
        /// fuselage. The fuselage is one long cylinder with a squashed sphere at each end.
        ///
        /// Scene 1 parks it outside the terminal window, and scenes 3 and 4 fly it, so it lives
        /// here rather than in any one scene's builder.
        ///
        /// Returns the plane's root, and puts every wheel under a child called "Landing Gear" so
        /// the flying scenes can fold it away without hunting through the hierarchy.
        /// </summary>
        public static Transform Airliner(Transform parent, Vector3 centre)
        {
            var p = Prim.Empty(parent, "Airliner (your plane)", centre).transform;
            float L = FlightLayout.Plane.FuselageLength;
            float R = FlightLayout.Plane.FuselageRadius;
            float half = FlightLayout.Plane.HalfLength;

            Prim.Cyl(p, "Fuselage", Vector3.zero, R, L, Prim.AxisX, PlaneWhite);
            Prim.Sphere(p, "Nose", new Vector3(half, 0f, 0f), new Vector3(6f, R * 2f, R * 2f), PlaneWhite);
            Prim.Sphere(p, "Tail Cone", new Vector3(-half, 0.15f, 0f), new Vector3(9f, R * 1.9f, R * 1.9f), PlaneWhite);

            // The markings are thin plates stuck to each SIDE of the fuselage, not boxes running
            // through it.
            //
            // A single box spanning the full width is simpler and looks identical from outside -
            // which is how scene 1 always saw it. But scenes 3 and 4 put you INSIDE this shell,
            // and from the pilot's seat those boxes cut straight through the cockpit: the blue
            // cheatline becomes a solid floor half a metre below your eyes.
            foreach (int side in new[] { -1, 1 })
            {
                float skin = side * (R - 0.01f);

                Prim.Box(p, "Cheatline", new Vector3(0f, -0.35f, skin), new Vector3(L * 0.96f, 0.26f, 0.06f), PlaneBlue);

                for (float x = -13f; x <= 12.5f; x += 0.95f)
                    Prim.Box(p, "Window", new Vector3(x, 0.45f, skin), new Vector3(0.3f, 0.36f, 0.06f), PlaneWindows);

                Prim.Box(p, "Cockpit Windows", new Vector3(half + 1.5f, 0.7f, side * 1.6f),
                         new Vector3(1.1f, 0.42f, 0.08f), new Vector3(0f, 0f, -25f), PlaneWindows);
            }

            Prim.Box(p, "Rear Door", new Vector3(-12f, -0.25f, -R - 0.01f), new Vector3(0.9f, 1.8f, 0.06f), EngineCowl);
            Prim.Text3D(p, "Airline Name", new Vector3(-2f, 1.1f, -R - 0.03f), Vector3.zero, "FLIGHT SIM AIR", 0.09f, new Color(0.1f, 0.25f, 0.6f));

            // Tail.
            // Narrow and only slightly tilted, so it reads as a swept fin rather than a diamond.
            Prim.Box(p, "Fin", new Vector3(-half - 2.4f, R + 2.2f, 0f), new Vector3(3.6f, 5.4f, 0.35f), new Vector3(0f, 0f, 18f), PlaneBlue);

            foreach (int side in new[] { -1, 1 })
            {
                Wing(p, "Wing", new Vector3(1.5f, -1.3f, 0f), side, 1.2f, 17f, 5.5f, 22f, true);
                Wing(p, "Tailplane", new Vector3(-half - 1.5f, 0.5f, 0f), side, 0.6f, 6f, 2.8f, 30f, false);
                Engine(p, "Engine", new Vector3(2.8f, -2.45f, side * 6f), 0.95f, 4f);
            }

            LandingGear(p, half);

            // Red anti-collision beacons, top and bottom, flashing out of step.
            SceneKit.Blinker(p, "Beacon (top)", new Vector3(1f, R + 0.15f, 0f), 0.3f, BeaconRed, new Color(1f, 0.1f, 0.1f), 8f, 0.12f, 1.1f, 0f);
            SceneKit.Blinker(p, "Beacon (belly)", new Vector3(1f, -R - 0.15f, 0f), 0.3f, BeaconRed, new Color(1f, 0.1f, 0.1f), 8f, 0.12f, 1.1f, 0.6f);

            return p;
        }

        /// <summary>
        /// The three legs, grouped under one object. Scene 1's plane is parked, so its gear just
        /// sits there; in the air, LandingGearAnimator swings this whole group up into the belly.
        /// </summary>
        static void LandingGear(Transform plane, float half)
        {
            var gear = Prim.Empty(plane, "Landing Gear", Vector3.zero).transform;

            foreach (int side in new[] { -1, 1 })
            {
                var leg = Prim.Empty(gear, "Main Gear", new Vector3(-1f, -1.7f, side * 2.8f)).transform;
                Prim.Cyl(leg, "Strut", new Vector3(0f, -0.9f, 0f), 0.16f, 1.8f, Prim.AxisY, GearMetal);
                Prim.Cyl(leg, "Wheel", new Vector3(0f, -1.7f, 0f), 0.45f, 0.7f, Prim.AxisZ, Tyre);
            }

            var nose = Prim.Empty(gear, "Nose Gear", new Vector3(half - 2f, -1.9f, 0f)).transform;
            Prim.Cyl(nose, "Strut", new Vector3(0f, -0.8f, 0f), 0.12f, 1.6f, Prim.AxisY, GearMetal);
            Prim.Cyl(nose, "Wheel", new Vector3(0f, -1.5f, 0f), 0.35f, 0.5f, Prim.AxisZ, Tyre);
        }

        // ------------------------------------------------------------------ the airport

        /// <summary>A runway lying along X, centred on the given point, with markings and edge lights.</summary>
        public static void Runway(Transform parent, Vector3 centre, float length, float width)
        {
            var r = Prim.Empty(parent, "Runway", centre).transform;

            Prim.Box(r, "Tarmac", new Vector3(0f, 0.02f, 0f), new Vector3(length, 0.02f, width), Tarmac);

            // Dashed centre line.
            for (float x = -length * 0.5f + 60f; x < length * 0.5f - 20f; x += 40f)
                Prim.Box(r, "Centre Line", new Vector3(x, 0.035f, 0f), new Vector3(20f, 0.01f, 0.9f), LineWhite);

            // "Piano keys" at the west end.
            for (int i = 0; i < 8; i++)
            {
                float z = -width * 0.5f + 4f + i * (width - 8f) / 7f;
                Prim.Box(r, "Threshold Stripe", new Vector3(-length * 0.5f + 18f, 0.035f, z),
                         new Vector3(28f, 0.01f, 1.8f), LineWhite);
            }

            foreach (int side in new[] { -1, 1 })
            {
                Prim.Box(r, "Edge Line", new Vector3(0f, 0.035f, side * (width * 0.5f - 1.5f)),
                         new Vector3(length, 0.01f, 0.7f), LineWhite);

                for (float x = -length * 0.5f + 10f; x < length * 0.5f; x += 30f)
                    Prim.NoShadow(Prim.Sphere(r, "Edge Light", new Vector3(x, 0.25f, side * (width * 0.5f + 1f)),
                                              Vector3.one * 0.35f, EdgeLight));
            }
        }

        /// <summary>
        /// The covered walkway from the terminal to the plane door, running along Z at the given
        /// X. Its floor is level with the building's floor, and it stands on legs down to the apron.
        /// </summary>
        public static void JetBridge(Transform parent, float x, float zFrom, float zTo, float floorY, float groundY)
        {
            float length = Mathf.Abs(zTo - zFrom);
            var b = Prim.Empty(parent, "Jet Bridge", new Vector3(x, floorY, (zFrom + zTo) * 0.5f)).transform;

            Prim.Box(b, "Tunnel", new Vector3(0f, 1.35f, 0f), new Vector3(2.6f, 2.7f, length), Bridge);
            Prim.Box(b, "Roof Rib", new Vector3(0f, 2.75f, 0f), new Vector3(2.8f, 0.1f, length), GearMetal);

            foreach (int side in new[] { -1, 1 })
                Prim.Box(b, "Window Strip", new Vector3(side * 1.31f, 1.7f, 0f),
                         new Vector3(0.02f, 0.45f, length - 1.5f), PlaneWindows);

            // Legs a fifth of the way from the plane end, where a real bridge's wheels are.
            float legZ = Mathf.Sign(zTo - zFrom) * length * 0.3f;
            float legHeight = floorY - groundY;

            foreach (int side in new[] { -1, 1 })
                Prim.Cyl(b, "Support Leg", new Vector3(side * 0.8f, -legHeight * 0.5f, legZ), 0.15f, legHeight, Prim.AxisY, GearMetal);

            Prim.Box(b, "Wheel Bogie", new Vector3(0f, -legHeight + 0.35f, legZ), new Vector3(2.2f, 0.7f, 0.8f), Tyre);
        }
    }
}
