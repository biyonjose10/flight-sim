using UnityEngine;
using W = FlightSim.FlightLayout.World;

namespace FlightSim.Build
{
    /// <summary>
    /// The world that scene 3 (take-off) and scene 4 (landing) both fly over.
    ///
    /// One runway lies along X, centred on the origin, and the plane always travels towards +X.
    /// West of the runway (-X) is farmland with roads, villages and a cluster of tower blocks;
    /// east of it, past the coast, is open sea. Hills sit far to the north and south, and cloud
    /// slabs float overhead so you can see how fast you are going.
    ///
    /// Both scenes call Build() and Sky(), so the airport you take off from is exactly the
    /// airport you land at, lit by exactly the same sun. Every number describing the shape of
    /// the world lives in FlightLayout.World; the numbers in this file are cosmetic detail.
    ///
    /// Everything here is decoration seen from hundreds of metres up, so nothing gets a
    /// collider. The flight code decides when you have touched down by comparing your height
    /// with W.GroundY, and it never asks the physics engine. A world made of thousands of
    /// colliders you can never bump into would just be work the computer does for nothing.
    /// </summary>
    public static class WorldParts
    {
        // --------------------------------------------------------------- tuning numbers
        // These are "how it looks" numbers. Anything describing the shape of the world proper
        // (runway size, where the coast is, how high the clouds are) lives in FlightLayout.World.

        /// <summary>The grass slab is square and this big on a side. Its east edge is the coast.</summary>
        const float GroundSize = 30000f;

        /// <summary>The parallel strip of concrete that planes taxi along, and how wide it is.</summary>
        const float TaxiwayZ = -110f;
        const float TaxiwayWidth = 25f;

        // The terminal and its apron, both on the same side of the runway as the taxiway.
        const float ApronZ = -185f;
        const float ApronLength = 700f;
        const float ApronDepth = 130f;
        const float TerminalZ = -272f;
        const float TerminalLength = 260f;
        const float TerminalDepth = 50f;
        const float TerminalHeight = 22f;
        const float TowerX = 190f;          // the control tower, off the end of the terminal

        /// <summary>
        /// How big the painted runway numbers are. A TextMesh character ends up about
        /// fontSize / 10 * size metres tall, and Prim uses fontSize 72, so 2.8 gives digits
        /// roughly 20 m tall - the same as the real thing, and readable from the air.
        /// </summary>
        const float RunwayNumberSize = 2.8f;

        // Countryside. Kept deliberately modest: a few hundred flat boxes read as farmland from
        // altitude, and anything more is detail nobody is ever close enough to see.
        const int FieldCount = 200;
        const int RoadCount = 24;
        const int HamletCount = 10;
        const int CountrysideSeed = 20260925;

        /// <summary>Nothing scattered may land inside this box, so no field pokes up through the runway.</summary>
        const float AirfieldKeepOutX = 2400f;
        const float AirfieldKeepOutZ = 1000f;

        // The tower blocks: a small town centre with real height, near enough to judge altitude by.
        const int TowerCount = 14;
        const float TownX = -2800f;
        const float TownZ = 2000f;
        const float TownSpread = 420f;
        const int TownSeed = 771;

        // The coast and the sea.
        const float BeachWidth = 240f;
        const float SeaReach = 40000f;      // how far east the water goes: past the haze, so it has no far edge
        const float SeaSpan = 40000f;       // and how far north and south

        // The hills on the horizon.
        const int HillsPerRidge = 18;
        const float RidgeZ = 11500f;
        const int HillSeed = 4242;

        const int CloudSeed = 9001;

        // ------------------------------------------------------------------- materials

        static Material fieldGreen, fieldOlive, fieldBrown, road, hamletWall, hamletRoof,
                        towerGreyA, towerGreyB, towerWindow, sand, sea, hillHaze, cloud,
                        meadow, heath, trunk, leafDark, leafLight, freshWater, hillNear;

        // ----------------------------------------------------------------- entry points

        /// <summary>Builds the whole shared world under one parent. Both flying scenes call this.</summary>
        public static void Build(Transform parent)
        {
            CreateMaterials();

            Ground(parent);

            // Landscape first, then everything that sits on top of it. The order matters only
            // because each layer is drawn a fraction higher than the last, so nothing flickers.
            GroundPatches(parent);
            RollingHills(parent);
            Water(parent);
            Woods(parent);

            Airfield(parent);
            Countryside(parent);
            Towers(parent);
            Coast(parent);
            Hills(parent);
            Clouds(parent);

            int pieces = parent == null ? 0 : parent.GetComponentsInChildren<Transform>(true).Length;
            Debug.Log(string.Format("[WORLD] Built the shared world: {0} objects under '{1}'.",
                                    pieces, parent == null ? "(no parent)" : parent.name));
        }

        /// <summary>The sky, sun and haze both flying scenes use, so they match.</summary>
        public static UnityEngine.Light Sky()
        {
            // The sun is 40 degrees up and shines roughly the way you fly, so it sits behind you
            // on take-off instead of glaring through the windscreen, and it lights the side of
            // the plane the chase camera looks at. The haze distances come from FlightLayout, so
            // the horizon fades out at the same range in both scenes.
            return SceneKit.Environment(new Vector3(40f, 25f, 0f), 1.2f, W.FogStart, W.FogEnd);
        }

        /// <summary>
        /// One palette for the whole world. Prim keeps materials as assets and updates them in
        /// place, so building scene 3 and then scene 4 reuses the same materials rather than
        /// replacing them underneath the scene that was built first.
        /// </summary>
        static void CreateMaterials()
        {
            fieldGreen  = Prim.Mat("Field Green", new Color(0.33f, 0.47f, 0.22f), 0f, 0.05f);
            fieldOlive  = Prim.Mat("Field Olive", new Color(0.46f, 0.5f, 0.24f), 0f, 0.05f);
            fieldBrown  = Prim.Mat("Field Ploughed", new Color(0.42f, 0.34f, 0.23f), 0f, 0.05f);
            road        = Prim.Mat("Country Road", new Color(0.24f, 0.24f, 0.25f), 0f, 0.1f);
            hamletWall  = Prim.Mat("Hamlet Wall", new Color(0.82f, 0.79f, 0.72f), 0f, 0.2f);
            hamletRoof  = Prim.Mat("Hamlet Roof", new Color(0.45f, 0.24f, 0.18f), 0f, 0.15f);
            towerGreyA  = Prim.Mat("Tower Block Light", new Color(0.66f, 0.67f, 0.68f), 0.1f, 0.35f);
            towerGreyB  = Prim.Mat("Tower Block Dark", new Color(0.46f, 0.47f, 0.5f), 0.1f, 0.35f);
            towerWindow = Prim.Emissive("Tower Block Windows", new Color(0.1f, 0.12f, 0.16f),
                                        new Color(0.55f, 0.52f, 0.35f));
            sand        = Prim.Mat("Beach Sand", new Color(0.85f, 0.79f, 0.6f), 0f, 0.1f);
            sea         = Prim.Mat("Sea", new Color(0.12f, 0.28f, 0.45f), 0.1f, 0.8f);
            hillHaze    = Prim.Mat("Distant Hills", new Color(0.42f, 0.5f, 0.45f), 0f, 0.05f);
            cloud       = Prim.Mat("Cloud", new Color(0.97f, 0.97f, 0.98f), 0f, 0.05f);

            meadow      = Prim.Mat("Meadow", new Color(0.4f, 0.53f, 0.25f), 0f, 0.05f);
            heath       = Prim.Mat("Heath", new Color(0.3f, 0.36f, 0.2f), 0f, 0.05f);
            trunk       = Prim.Mat("Tree Trunk", new Color(0.3f, 0.22f, 0.15f), 0f, 0.1f);
            leafDark    = Prim.Mat("Tree Leaves Dark", new Color(0.16f, 0.32f, 0.15f), 0f, 0.08f);
            leafLight   = Prim.Mat("Tree Leaves Light", new Color(0.24f, 0.42f, 0.18f), 0f, 0.08f);
            freshWater  = Prim.Mat("Fresh Water", new Color(0.16f, 0.34f, 0.44f), 0.1f, 0.85f);
            hillNear    = Prim.Mat("Rolling Hills", new Color(0.34f, 0.45f, 0.26f), 0f, 0.05f);
        }

        // ---------------------------------------------------------------------- 1. ground

        /// <summary>
        /// The floor of the world: one enormous flat grass slab, plus the concrete apron the
        /// terminal stands on. The slab is pushed west so its east edge lands exactly on the
        /// coast - if the grass carried on east it would cover the sea, which sits four metres
        /// lower down, and you would never see any water.
        /// </summary>
        static void Ground(Transform parent)
        {
            var g = Prim.Empty(parent, "Ground", Vector3.zero).transform;

            float grassCentreX = W.CoastX - GroundSize * 0.5f;

            Prim.Box(g, "Grass (the land)", new Vector3(grassCentreX, W.GroundY - 0.1f, 0f),
                     new Vector3(GroundSize, 0.2f, GroundSize), SharedParts.Grass);

            Prim.Box(g, "Airport Apron", new Vector3(0f, W.GroundY + 0.02f, ApronZ),
                     new Vector3(ApronLength, 0.04f, ApronDepth), SharedParts.Concrete);
        }

        // -------------------------------------------------------------------- 2. airfield

        /// <summary>
        /// The airport itself: the runway you use, the taxiway beside it, the terminal you walked
        /// through in scenes 1 and 2, a control tower, and the big painted numbers that tell a
        /// pilot which runway this is.
        /// </summary>
        static void Airfield(Transform parent)
        {
            var a = Prim.Empty(parent, "Airfield", Vector3.zero).transform;

            // SharedParts draws the tarmac, the dashed centre line, the threshold stripes and the
            // edge lights. Scene 1's runway comes from the same call, so the two match exactly.
            SharedParts.Runway(a, new Vector3(0f, W.GroundY, 0f), W.RunwayLength, W.RunwayWidth);

            Taxiways(a);
            TerminalBlock(a);
            ControlTower(a);
            RunwayNumbers(a);
        }

        /// <summary>
        /// The strip of concrete planes wait on, running parallel to the runway, and three short
        /// links joining the two. It is only scenery, but without it the runway looks like a road
        /// dropped in a field rather than part of an airport.
        /// </summary>
        static void Taxiways(Transform a)
        {
            float y = W.GroundY;

            Prim.Box(a, "Taxiway", new Vector3(0f, y + 0.03f, TaxiwayZ),
                     new Vector3(W.RunwayLength * 0.9f, 0.04f, TaxiwayWidth), SharedParts.Concrete);
            Prim.Box(a, "Taxiway Centre Line", new Vector3(0f, y + 0.06f, TaxiwayZ),
                     new Vector3(W.RunwayLength * 0.9f, 0.01f, 0.8f), SharedParts.LineYellow);

            float fromZ = -W.RunwayWidth * 0.5f;
            float midZ = (fromZ + TaxiwayZ) * 0.5f;
            float linkLength = Mathf.Abs(TaxiwayZ - fromZ);

            foreach (float x in new[] { -900f, 0f, 900f })
            {
                Prim.Box(a, "Link Taxiway", new Vector3(x, y + 0.03f, midZ),
                         new Vector3(TaxiwayWidth, 0.04f, linkLength), SharedParts.Concrete);
                Prim.Box(a, "Link Taxiway Line", new Vector3(x, y + 0.06f, midZ),
                         new Vector3(0.8f, 0.01f, linkLength), SharedParts.LineYellow);
            }
        }

        /// <summary>
        /// The terminal building, seen from outside this time. It uses the same facade, roof and
        /// window materials as the building you walked around inside in scene 1, so it is clearly
        /// meant to be the same place.
        /// </summary>
        static void TerminalBlock(Transform a)
        {
            var t = Prim.Empty(a, "Terminal Building", new Vector3(0f, W.GroundY, TerminalZ)).transform;

            Prim.Box(t, "Facade", new Vector3(0f, TerminalHeight * 0.5f, 0f),
                     new Vector3(TerminalLength, TerminalHeight, TerminalDepth), SharedParts.Facade);
            Prim.Box(t, "Roof", new Vector3(0f, TerminalHeight + 0.6f, 0f),
                     new Vector3(TerminalLength + 4f, 1.2f, TerminalDepth + 4f), SharedParts.Roof);

            // The long strip of glass facing the apron, a whisker proud of the wall so the two
            // surfaces do not fight over the same pixels.
            Prim.Box(t, "Window Strip", new Vector3(0f, TerminalHeight * 0.62f, TerminalDepth * 0.5f + 0.2f),
                     new Vector3(TerminalLength - 20f, 6f, 0.4f), SharedParts.PlaneWindows);

            // Jet-bridge stubs poking out towards the apron, so the gates read as gates.
            for (int i = -2; i <= 2; i++)
            {
                Prim.Box(t, "Gate Pier", new Vector3(i * 52f, 5f, TerminalDepth * 0.5f + 14f),
                         new Vector3(3.2f, 3.2f, 28f), SharedParts.Bridge);
            }
        }

        /// <summary>
        /// The control tower: a plain concrete column with a glazed box on top, the way every real
        /// one looks. It is the tallest thing on the airfield, which makes it the easiest landmark
        /// to aim at when you turn back towards the runway.
        /// </summary>
        static void ControlTower(Transform a)
        {
            var c = Prim.Empty(a, "Control Tower", new Vector3(TowerX, W.GroundY, TerminalZ + 30f)).transform;

            const float shaft = 46f;

            Prim.Cyl(c, "Shaft", new Vector3(0f, shaft * 0.5f, 0f), 5.5f, shaft, Prim.AxisY, SharedParts.Facade);
            Prim.Box(c, "Cab Floor", new Vector3(0f, shaft + 0.4f, 0f), new Vector3(17f, 0.8f, 17f), SharedParts.Roof);
            Prim.Box(c, "Cab Glass", new Vector3(0f, shaft + 3.6f, 0f), new Vector3(15f, 5.6f, 15f),
                     SharedParts.PlaneWindows);
            Prim.Box(c, "Cab Roof", new Vector3(0f, shaft + 7f, 0f), new Vector3(19f, 1.2f, 19f), SharedParts.Roof);
            Prim.NoShadow(Prim.Sphere(c, "Tower Beacon", new Vector3(0f, shaft + 8.4f, 0f),
                                      Vector3.one * 1.6f, SharedParts.BeaconRed));
        }

        /// <summary>
        /// The huge painted numbers at each end of the runway. A runway is named after the compass
        /// heading you fly along it, divided by ten: this one runs east-west, so it is "09" used
        /// eastbound and "27" used westbound. The 09 is painted at the west end, because the west
        /// end is where you arrive when you are flying east.
        ///
        /// The rotation is the fiddly part. A TextMesh faces -Z and reads left to right along its
        /// own +X, so with no rotation at all it stands upright and reads correctly to a viewer
        /// looking towards +Z (this is the note in CLAUDE.md). Turning it 90 degrees about X lays
        /// it flat on the tarmac with its face pointing straight up. The extra 90 degrees about Y
        /// then spins it flat on the ground until the top of the digits points along +X - the way
        /// you are travelling - which is what makes the number read the right way up through the
        /// windscreen. The number at the far end uses -90 instead, because it is read by somebody
        /// coming the other way.
        /// </summary>
        static void RunwayNumbers(Transform a)
        {
            float y = W.GroundY + 0.06f;   // just above the tarmac and the paint already on it

            // Rx(90) lays the text face-up; the Ry then spins it on the ground until the tops of
            // the digits point the way you are travelling, which is how real runway numbers are
            // painted.
            //
            // Only check this from a camera looking straight down the runway (snapshot
            // "3a_on_the_runway"). From an angle off to one side the digits look rotated whatever
            // you do, and "09" seen obliquely is easily mistaken for "60".
            Prim.Text3D(a, "Runway Number 09", new Vector3(W.ThresholdX + 90f, y, 0f),
                        new Vector3(90f, 90f, 0f), "09", RunwayNumberSize, Color.white);

            Prim.Text3D(a, "Runway Number 27", new Vector3(W.FarEndX - 90f, y, 0f),
                        new Vector3(90f, -90f, 0f), "27", RunwayNumberSize, Color.white);
        }

        // ----------------------------------------------------------------- 3. countryside

        /// <summary>
        /// Farmland: flat coloured rectangles for fields, long thin dark boxes for roads, and a
        /// few clusters of little houses. It is all scattered by a random number generator given a
        /// fixed starting number (a "seed"), so it produces exactly the same countryside every
        /// time the scenes are built - random-looking, but never actually different from one build
        /// to the next, which is what stops the world changing behind your back.
        ///
        /// Everything keeps clear of a box around the airfield, so no field ever pokes up through
        /// the runway, and stops short of the coast so no field floats out to sea.
        /// </summary>
        static void Countryside(Transform parent)
        {
            var c = Prim.Empty(parent, "Countryside", Vector3.zero).transform;
            var rng = new System.Random(CountrysideSeed);

            var fields = Prim.Empty(c, "Fields", Vector3.zero).transform;
            var shades = new[] { fieldGreen, fieldOlive, fieldBrown };

            for (int i = 0; i < FieldCount; i++)
            {
                float x = Range(rng, -19000f, W.CoastX - 700f);
                float z = Range(rng, -13000f, 13000f);
                if (NearAirfield(x, z)) continue;

                float wide = Range(rng, 350f, 1100f);
                float deep = Range(rng, 300f, 900f);
                float yaw = Range(rng, -12f, 12f);   // a little skew, so it is not a chessboard

                Prim.Box(fields, "Field", new Vector3(x, W.GroundY + 0.05f, z),
                         new Vector3(wide, 0.06f, deep), new Vector3(0f, yaw, 0f),
                         shades[rng.Next(shades.Length)]);
            }

            var roads = Prim.Empty(c, "Roads", Vector3.zero).transform;

            for (int i = 0; i < RoadCount; i++)
            {
                if ((i % 2) == 0)
                {
                    // Runs along X. Held well north or south of the runway so it never crosses it.
                    float z = Range(rng, 1400f, 13000f) * (rng.Next(2) == 0 ? -1f : 1f);
                    Prim.Box(roads, "Road (east-west)", new Vector3(-7000f, W.GroundY + 0.09f, z),
                             new Vector3(25000f, 0.04f, 14f), road);
                }
                else
                {
                    // Runs along Z, so it only has to dodge the airfield's own length.
                    float x = Range(rng, AirfieldKeepOutX, 18000f) * (rng.Next(2) == 0 ? -1f : 1f);
                    if (x > W.CoastX - 400f) x = W.CoastX - 400f;
                    Prim.Box(roads, "Road (north-south)", new Vector3(x, W.GroundY + 0.09f, 0f),
                             new Vector3(14f, 0.04f, 26000f), road);
                }
            }

            var hamlets = Prim.Empty(c, "Hamlets", Vector3.zero).transform;

            for (int i = 0; i < HamletCount; i++)
            {
                float hx = Range(rng, -16000f, W.CoastX - 1200f);
                float hz = Range(rng, -11000f, 11000f);
                if (NearAirfield(hx, hz)) continue;

                var h = Prim.Empty(hamlets, "Hamlet", new Vector3(hx, W.GroundY, hz)).transform;
                int houses = 6 + rng.Next(4);

                for (int n = 0; n < houses; n++)
                {
                    float ox = Range(rng, -160f, 160f);
                    float oz = Range(rng, -160f, 160f);
                    float wide = Range(rng, 14f, 26f);
                    float tall = Range(rng, 7f, 13f);
                    float deep = Range(rng, 12f, 22f);

                    Prim.Box(h, "House", new Vector3(ox, tall * 0.5f, oz),
                             new Vector3(wide, tall, deep), hamletWall);
                    Prim.Box(h, "House Roof", new Vector3(ox, tall + 1.2f, oz),
                             new Vector3(wide + 2f, 2.4f, deep + 2f), hamletRoof);
                }
            }
        }

        /// <summary>True if this spot is close enough to the airfield that scenery would be in the way.</summary>
        static bool NearAirfield(float x, float z)
        {
            return Mathf.Abs(x) < AirfieldKeepOutX && Mathf.Abs(z) < AirfieldKeepOutZ;
        }

        // ------------------------------------------------------- extra landscape detail

        // These exist because from a few hundred metres up the old world read as a flat green
        // table. Height, water and trees are what actually make ground look like ground.
        const int GroundPatchCount = 150;
        const int WoodCount = 90;           // clumps of trees, not single trees
        const int TreesPerWood = 9;
        const int RollingHillCount = 70;
        const int LakeCount = 7;
        const int RiverLinks = 46;
        const int DetailSeed = 71741;

        /// <summary>
        /// Big soft patches of slightly different greens and browns laid over the grass.
        ///
        /// One flat colour over thirty kilometres is what made the land look like a table. Real
        /// ground is never one colour, and at altitude the patchwork is most of what tells you you
        /// are moving. They are stacked at slightly different heights so no two ever fight over
        /// the same pixels.
        /// </summary>
        static void GroundPatches(Transform parent)
        {
            var g = Prim.Empty(parent, "Ground Patches", Vector3.zero).transform;
            var rng = new System.Random(DetailSeed);
            var shades = new[] { meadow, heath, fieldGreen, fieldOlive };

            for (int i = 0; i < GroundPatchCount; i++)
            {
                float x = Range(rng, -18000f, W.CoastX - 400f);
                float z = Range(rng, -14000f, 14000f);
                if (NearAirfield(x, z)) continue;

                float wide = Range(rng, 900f, 3400f);
                float deep = Range(rng, 800f, 2800f);

                Prim.Box(g, "Ground Patch", new Vector3(x, W.GroundY + 0.03f, z),
                         new Vector3(wide, 0.04f, deep), new Vector3(0f, Range(rng, -20f, 20f), 0f),
                         shades[rng.Next(shades.Length)]);
            }
        }

        /// <summary>
        /// Woods: clumps of simple trees, a trunk with a blob of leaves on top.
        ///
        /// Trees are the cheapest thing that makes ground look inhabited from the air, because
        /// they give the surface texture and a sense of scale. They are grouped into woods rather
        /// than sprinkled evenly, which is how trees actually grow and reads far better than a
        /// uniform dusting.
        /// </summary>
        static void Woods(Transform parent)
        {
            var w = Prim.Empty(parent, "Woods", Vector3.zero).transform;
            var rng = new System.Random(DetailSeed + 7);

            for (int i = 0; i < WoodCount; i++)
            {
                float cx = Range(rng, -16000f, W.CoastX - 600f);
                float cz = Range(rng, -13000f, 13000f);
                if (NearAirfield(cx, cz)) continue;

                float spread = Range(rng, 90f, 320f);
                var wood = Prim.Empty(w, "Wood", new Vector3(cx, W.GroundY, cz)).transform;

                for (int t = 0; t < TreesPerWood; t++)
                {
                    float tx = Range(rng, -spread, spread);
                    float tz = Range(rng, -spread, spread);
                    float height = Range(rng, 14f, 26f);

                    Prim.Cyl(wood, "Trunk", new Vector3(tx, height * 0.3f, tz),
                             height * 0.07f, height * 0.6f, Prim.AxisY, trunk);
                    Prim.Sphere(wood, "Leaves", new Vector3(tx, height * 0.78f, tz),
                                Vector3.one * height * 0.78f,
                                rng.NextDouble() > 0.5 ? leafDark : leafLight);
                }
            }
        }

        /// <summary>
        /// Lakes, and a river winding out to the coast.
        ///
        /// Water is the single most useful thing to add to a green landscape: it is a completely
        /// different colour and it catches the light, so it breaks the ground up from any height.
        /// The river is a chain of overlapping boxes, each turned a little from the last, which is
        /// enough to read as a winding line without any curved geometry.
        /// </summary>
        static void Water(Transform parent)
        {
            var wt = Prim.Empty(parent, "Inland Water", Vector3.zero).transform;
            var rng = new System.Random(DetailSeed + 21);

            for (int i = 0; i < LakeCount; i++)
            {
                float x = Range(rng, -15000f, W.CoastX - 1500f);
                float z = Range(rng, -11000f, 11000f);
                if (NearAirfield(x, z)) continue;

                float wide = Range(rng, 500f, 1900f);
                float deep = Range(rng, 400f, 1300f);

                Prim.Box(wt, "Lake", new Vector3(x, W.GroundY + 0.09f, z),
                         new Vector3(wide, 0.05f, deep), new Vector3(0f, Range(rng, -30f, 30f), 0f), freshWater);
            }

            // The river starts inland and wanders east until it reaches the sea.
            float rx = -13000f;
            float rz = 5200f;
            float heading = 8f;

            for (int i = 0; i < RiverLinks; i++)
            {
                float length = 620f;
                heading += Range(rng, -13f, 13f);

                Prim.Box(wt, "River", new Vector3(rx, W.GroundY + 0.1f, rz),
                         new Vector3(length, 0.05f, Range(rng, 55f, 95f)),
                         new Vector3(0f, heading, 0f), freshWater);

                // Each link steps forward by less than its own length, so consecutive boxes
                // overlap. Step the full length and every bend leaves a visible notch.
                rx += Mathf.Cos(heading * Mathf.Deg2Rad) * length * 0.7f;
                rz -= Mathf.Sin(heading * Mathf.Deg2Rad) * length * 0.7f;

                if (rx > W.CoastX) break;
            }
        }

        /// <summary>
        /// Low rolling hills under the flight path, made from squashed spheres sunk into the
        /// ground so only their tops show.
        ///
        /// The distant ridges already gave the horizon a shape, but everything between here and
        /// there was flat. These are nearer and smaller, so the ground actually rises and falls
        /// underneath you as you fly over it.
        /// </summary>
        static void RollingHills(Transform parent)
        {
            var h = Prim.Empty(parent, "Rolling Hills", Vector3.zero).transform;
            var rng = new System.Random(DetailSeed + 33);

            for (int i = 0; i < RollingHillCount; i++)
            {
                float x = Range(rng, -17000f, W.CoastX - 900f);
                float z = Range(rng, -13000f, 13000f);
                if (NearAirfield(x, z)) continue;

                float wide = Range(rng, 700f, 2600f);
                float tall = Range(rng, 60f, 220f);

                // Sunk by half its height, so the sphere reads as a hill rather than a ball
                // sitting on a table.
                Prim.NoShadow(Prim.Sphere(h, "Hill", new Vector3(x, W.GroundY - tall * 0.35f, z),
                                          new Vector3(wide, tall * 2f, wide * Range(rng, 0.6f, 1.2f)), hillNear));
            }
        }

        // ---------------------------------------------------------------------- 4. towers

        /// <summary>
        /// A handful of tower blocks just off the airfield. Fields and roads are flat, so from the
        /// air they tell you nothing about how high you are; something a hundred-odd metres tall
        /// close to the runway does, because you can watch yourself climb past the top of it.
        /// </summary>
        static void Towers(Transform parent)
        {
            var t = Prim.Empty(parent, "Town Towers", new Vector3(TownX, W.GroundY, TownZ)).transform;
            var rng = new System.Random(TownSeed);

            for (int i = 0; i < TowerCount; i++)
            {
                float x = Range(rng, -TownSpread, TownSpread);
                float z = Range(rng, -TownSpread, TownSpread);
                float wide = Range(rng, 22f, 42f);
                float deep = Range(rng, 22f, 42f);
                float tall = Range(rng, 60f, 190f);

                Prim.Box(t, "Tower Block", new Vector3(x, tall * 0.5f, z),
                         new Vector3(wide, tall, deep), rng.Next(2) == 0 ? towerGreyA : towerGreyB);

                // Two strips of lit windows, one on each side, standing a little proud of the wall
                // so they do not fight with it over the same pixels.
                foreach (int side in new[] { -1, 1 })
                {
                    Prim.NoShadow(Prim.Box(t, "Tower Windows",
                                           new Vector3(x + side * (wide * 0.5f + 0.15f), tall * 0.52f, z),
                                           new Vector3(0.3f, tall * 0.82f, deep * 0.7f), towerWindow));
                }
            }
        }

        // ----------------------------------------------------------------------- 5. coast

        /// <summary>
        /// Where the land runs out. A pale strip of sand, a short bank down to the waterline, and
        /// then the sea: one very large, slightly shiny blue slab that reaches further east, north
        /// and south than the haze can see, so the water never shows a visible edge.
        /// </summary>
        static void Coast(Transform parent)
        {
            var c = Prim.Empty(parent, "Coast", Vector3.zero).transform;

            Prim.Box(c, "Beach", new Vector3(W.CoastX - BeachWidth * 0.5f, W.GroundY + 0.04f, 0f),
                     new Vector3(BeachWidth, 0.06f, SeaSpan * 0.75f), sand);

            // The land sits at W.GroundY and the water at W.SeaY, so this fills the step between
            // the two. Without it you would see straight under the world along the shoreline.
            float bankHeight = W.GroundY - W.SeaY + 0.4f;
            Prim.Box(c, "Shore Bank", new Vector3(W.CoastX + 30f, W.GroundY - bankHeight * 0.5f, 0f),
                     new Vector3(60f, bankHeight, SeaSpan * 0.75f), sand);

            Prim.Box(c, "Sea", new Vector3(W.CoastX + SeaReach * 0.5f, W.SeaY - 30f, 0f),
                     new Vector3(SeaReach, 60f, SeaSpan), sea);
        }

        // ----------------------------------------------------------------------- 6. hills

        /// <summary>
        /// A ridge of hills far to the north and another far to the south, made of wide, squashed
        /// spheres sunk into the ground so only their tops show. At that distance the haze eats
        /// all the detail anyway, so they only have to read as a bumpy silhouette along the
        /// horizon - which is exactly what stops the world looking like a flat table.
        /// </summary>
        static void Hills(Transform parent)
        {
            var h = Prim.Empty(parent, "Horizon Hills", Vector3.zero).transform;
            var rng = new System.Random(HillSeed);

            foreach (int side in new[] { -1, 1 })
            {
                var ridge = Prim.Empty(h, side < 0 ? "South Ridge" : "North Ridge", Vector3.zero).transform;

                for (int i = 0; i < HillsPerRidge; i++)
                {
                    // The ridge stops short of the coast, so no hill ends up standing in the sea.
                    float x = Mathf.Lerp(-18000f, W.CoastX - 2200f, i / (float)(HillsPerRidge - 1));
                    x += Range(rng, -400f, 400f);

                    float z = side * (RidgeZ + Range(rng, -900f, 900f));
                    float wide = Range(rng, 2200f, 3600f);
                    float tall = Range(rng, 900f, 1700f);
                    float deep = Range(rng, 1400f, 2400f);

                    // Sunk by a third of its height, so the sphere meets the ground as a hillside
                    // instead of sitting on it like a dropped ball.
                    Prim.Sphere(ridge, "Hill", new Vector3(x, W.GroundY - tall * 0.33f, z),
                                new Vector3(wide, tall, deep), hillHaze);
                }
            }
        }

        // ---------------------------------------------------------------------- 7. clouds

        /// <summary>
        /// The clouds. Each one is three to five overlapping squashed spheres, which is enough to
        /// look like a lump of cloud from a distance and costs almost nothing to draw. They are
        /// spread over tens of kilometres and between the two heights in FlightLayout, so you fly
        /// between them rather than under a ceiling - and passing one is the clearest sign of how
        /// fast you are really moving, because the ground alone is too far away to tell.
        ///
        /// None of them casts a shadow. A cloud that shadowed the ground would make Unity draw the
        /// whole world a second time from the sun's point of view, for nothing you would notice.
        /// </summary>
        static void Clouds(Transform parent)
        {
            var c = Prim.Empty(parent, "Clouds", Vector3.zero).transform;
            var rng = new System.Random(CloudSeed);

            for (int i = 0; i < W.CloudCount; i++)
            {
                float x = Range(rng, -14000f, 16000f);
                float z = Range(rng, -9000f, 9000f);
                float y = Range(rng, W.CloudLowY, W.CloudHighY);

                var slab = Prim.Empty(c, "Cloud", new Vector3(x, y, z)).transform;
                int puffs = 3 + rng.Next(3);
                float scale = Range(rng, 0.75f, 1.6f);

                for (int n = 0; n < puffs; n++)
                {
                    var offset = new Vector3(Range(rng, -280f, 280f) * scale,
                                             Range(rng, -25f, 25f) * scale,
                                             Range(rng, -160f, 160f) * scale);

                    var size = new Vector3(Range(rng, 420f, 760f), Range(rng, 90f, 150f),
                                           Range(rng, 320f, 560f)) * scale;

                    Prim.NoShadow(Prim.Sphere(slab, "Puff", offset, size, cloud));
                }
            }
        }

        // ----------------------------------------------------------------------- helpers

        /// <summary>
        /// A random number between two values. System.Random only hands out whole numbers and
        /// fractions between 0 and 1, so this stretches one of those fractions to fit the range.
        /// It is System.Random rather than UnityEngine.Random because a seeded System.Random gives
        /// the same answers on every build, which keeps the world identical each time.
        /// </summary>
        static float Range(System.Random rng, float low, float high)
        {
            return low + (float)rng.NextDouble() * (high - low);
        }
    }
}
