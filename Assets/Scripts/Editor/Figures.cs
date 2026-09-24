using UnityEngine;

namespace FlightSim.Build
{
    /// <summary>
    /// The passengers, built from the same simple shapes as everything else so they match the
    /// low-poly look: a capsule body, box limbs and a sphere head.
    ///
    /// A figure is built facing +Z and then turned to face its seat's direction. The head is an
    /// empty pivot with the skull, hair and nose inside it. The nose and hair are what make a
    /// head turn visible (a plain sphere looks identical from every angle), and PassengerIdle
    /// turns that pivot.
    /// </summary>
    public static class Figures
    {
        static readonly Color[] Shirts =
        {
            new Color(0.75f, 0.25f, 0.22f), new Color(0.22f, 0.4f, 0.65f), new Color(0.3f, 0.55f, 0.35f),
            new Color(0.85f, 0.7f, 0.3f),   new Color(0.45f, 0.3f, 0.55f), new Color(0.9f, 0.9f, 0.88f),
            new Color(0.2f, 0.2f, 0.24f),   new Color(0.85f, 0.5f, 0.25f)
        };

        static readonly Color[] Trousers =
        {
            new Color(0.18f, 0.2f, 0.28f), new Color(0.3f, 0.27f, 0.22f), new Color(0.15f, 0.15f, 0.15f),
            new Color(0.4f, 0.42f, 0.45f)
        };

        static readonly Color[] Skins =
        {
            new Color(0.95f, 0.8f, 0.68f), new Color(0.8f, 0.6f, 0.45f),
            new Color(0.55f, 0.38f, 0.26f), new Color(0.36f, 0.25f, 0.18f)
        };

        static readonly Color[] Hairs =
        {
            new Color(0.12f, 0.09f, 0.07f), new Color(0.35f, 0.22f, 0.12f),
            new Color(0.75f, 0.6f, 0.35f),  new Color(0.55f, 0.55f, 0.55f)
        };

        /// <summary>Picks colours from the lists above. "look" is just a number that varies each person.</summary>
        static void Palette(int look, out Material shirt, out Material trousers, out Material skin, out Material hair)
        {
            int s = look % Shirts.Length;
            int t = (look / 3) % Trousers.Length;
            int k = (look / 2 + look) % Skins.Length;
            int h = (look * 7 + 1) % Hairs.Length;

            shirt = Prim.Mat("Shirt " + s, Shirts[s]);
            trousers = Prim.Mat("Trousers " + t, Trousers[t]);
            skin = Prim.Mat("Skin " + k, Skins[k], 0f, 0.35f);
            hair = Prim.Mat("Hair " + h, Hairs[h], 0f, 0.15f);
        }

        /// <summary>Someone sitting. Place at the seat's position on the floor, facing the way the seat faces.</summary>
        public static GameObject Seated(Transform parent, string name, Vector3 floorPos, float yaw, int look)
        {
            Material shirt, trousers, skin, hair;
            Palette(look, out shirt, out trousers, out skin, out hair);

            var root = Prim.Empty(parent, name, floorPos, new Vector3(0f, yaw, 0f)).transform;

            Prim.Box(root, "Thighs", new Vector3(0f, 0.54f, 0.1f), new Vector3(0.34f, 0.14f, 0.42f), trousers);
            Prim.Box(root, "Shins", new Vector3(0f, 0.25f, 0.3f), new Vector3(0.3f, 0.48f, 0.12f), trousers);
            Prim.Box(root, "Shoes", new Vector3(0f, 0.04f, 0.37f), new Vector3(0.3f, 0.08f, 0.22f), Prim.Mat("Shoes", new Color(0.1f, 0.1f, 0.1f)));
            Prim.Capsule(root, "Torso", new Vector3(0f, 0.9f, -0.08f), 0.38f, 0.66f, shirt);
            Prim.Box(root, "Arm L", new Vector3(-0.21f, 0.82f, 0f), new Vector3(0.09f, 0.42f, 0.11f), new Vector3(-25f, 0f, 0f), shirt);
            Prim.Box(root, "Arm R", new Vector3(0.21f, 0.82f, 0f), new Vector3(0.09f, 0.42f, 0.11f), new Vector3(-25f, 0f, 0f), shirt);

            var head = Head(root, new Vector3(0f, 1.33f, -0.06f), skin, hair);
            Idle(root.gameObject, head, look);
            return root.gameObject;
        }

        /// <summary>Someone standing. Place at their feet.</summary>
        public static GameObject Standing(Transform parent, string name, Vector3 feet, float yaw, int look)
        {
            Material shirt, trousers, skin, hair;
            Palette(look, out shirt, out trousers, out skin, out hair);

            var root = Prim.Empty(parent, name, feet, new Vector3(0f, yaw, 0f)).transform;

            Prim.Box(root, "Leg L", new Vector3(-0.1f, 0.43f, 0f), new Vector3(0.15f, 0.86f, 0.17f), trousers);
            Prim.Box(root, "Leg R", new Vector3(0.1f, 0.43f, 0f), new Vector3(0.15f, 0.86f, 0.17f), trousers);
            Prim.Capsule(root, "Torso", new Vector3(0f, 1.2f, 0f), 0.42f, 0.7f, shirt);
            Prim.Box(root, "Arm L", new Vector3(-0.27f, 1.13f, 0f), new Vector3(0.1f, 0.62f, 0.12f), shirt);
            Prim.Box(root, "Arm R", new Vector3(0.27f, 1.13f, 0f), new Vector3(0.1f, 0.62f, 0.12f), shirt);

            // Standing people are solid, so you walk around them rather than through them.
            Prim.Blocker(root, "Body Collider", new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 1.8f, 0.35f));

            var head = Head(root, new Vector3(0f, 1.66f, 0f), skin, hair);
            Idle(root.gameObject, head, look);
            return root.gameObject;
        }

        /// <summary>
        /// A pilot sitting at the controls: the same build as a seated passenger, but in a white
        /// uniform with shoulder boards, wearing a headset, with both arms reaching forward to the
        /// controls instead of folded in a lap.
        ///
        /// If a picture file is given, it is used for the face. Pass null for a plain-faced crew
        /// member.
        /// </summary>
        /// <param name="headYaw">
        /// How far the head is turned from straight ahead, in degrees. This matters more than it
        /// sounds: you enter the cockpit from behind, so a pilot looking dead ahead shows you
        /// nothing but the back of his skull. Turning the head towards the other seat means he is
        /// glancing over at you, which is both what a captain does when someone comes in and the
        /// only angle a flat photographed face actually reads from.
        /// </param>
        public static GameObject Pilot(Transform parent, string name, Vector3 seatPos, float yaw,
                                       string faceTexture, float headYaw)
        {
            var uniform = Prim.Mat("Pilot Uniform", new Color(0.93f, 0.94f, 0.96f), 0f, 0.2f);
            var darks = Prim.Mat("Pilot Blues", new Color(0.11f, 0.13f, 0.2f));
            var skin = Prim.Mat("Skin 2", Skins[2], 0f, 0.35f);
            var hair = Prim.Mat("Hair 0", Hairs[0], 0f, 0.15f);
            var headset = Prim.Mat("Headset", new Color(0.07f, 0.07f, 0.08f), 0.2f, 0.3f);

            var root = Prim.Empty(parent, name, seatPos, new Vector3(0f, yaw, 0f)).transform;

            Prim.Box(root, "Thighs", new Vector3(0f, 0.54f, 0.1f), new Vector3(0.34f, 0.14f, 0.42f), darks);
            Prim.Box(root, "Shins", new Vector3(0f, 0.25f, 0.3f), new Vector3(0.3f, 0.48f, 0.12f), darks);
            Prim.Box(root, "Shoes", new Vector3(0f, 0.04f, 0.37f), new Vector3(0.3f, 0.08f, 0.22f),
                     Prim.Mat("Shoes", new Color(0.1f, 0.1f, 0.1f)));
            Prim.Capsule(root, "Torso", new Vector3(0f, 0.9f, -0.06f), 0.38f, 0.66f, uniform);
            Prim.Box(root, "Tie", new Vector3(0f, 0.97f, 0.14f), new Vector3(0.045f, 0.2f, 0.03f), darks);
            Prim.Cyl(root, "Neck", new Vector3(0f, 1.19f, -0.04f), 0.055f, 0.14f, Prim.AxisY, skin);

            foreach (int side in new[] { -1, 1 })
            {
                // Leaning forward towards the sidestick and the thrust levers.
                Prim.Box(root, "Arm", new Vector3(side * 0.21f, 0.84f, 0.12f),
                         new Vector3(0.09f, 0.42f, 0.11f), new Vector3(-58f, 0f, 0f), uniform);
                Prim.Box(root, "Shoulder Board", new Vector3(side * 0.17f, 1.13f, -0.03f),
                         new Vector3(0.11f, 0.03f, 0.13f), darks);
            }

            var head = PilotHead(root, new Vector3(0f, 1.33f, -0.04f), skin, hair, headset, faceTexture);
            head.localRotation = Quaternion.Euler(0f, headYaw, 0f);

            // A pilot watching the runway barely moves their head, so the glance is small and slow.
            var idle = root.gameObject.AddComponent<PassengerIdle>();
            idle.head = head;
            idle.lookAngle = 9f;
            idle.lookSpeed = 0.17f;
            idle.phase = 1.1f;

            return root.gameObject;
        }

        /// <summary>
        /// The pilot's head: skull, hair, headset, and a photograph on a flat card where the face
        /// goes. The nose is deliberately left off - a 3D nose poking out of a photographed face
        /// looks wrong.
        /// </summary>
        static Transform PilotHead(Transform root, Vector3 pos, Material skin, Material hair,
                                   Material headsetMat, string faceTexture)
        {
            var pivot = Prim.Empty(root, "Head", pos).transform;

            Prim.Sphere(pivot, "Skull", Vector3.zero, Vector3.one * 0.23f, skin);
            Prim.Sphere(pivot, "Hair", new Vector3(0f, 0.04f, -0.03f), new Vector3(0.235f, 0.2f, 0.22f), hair);

            // Headset: a band over the top and a cup on each ear.
            Prim.Box(pivot, "Headset Band", new Vector3(0f, 0.12f, -0.01f), new Vector3(0.24f, 0.03f, 0.05f), headsetMat);
            foreach (int side in new[] { -1, 1 })
                Prim.Box(pivot, "Ear Cup", new Vector3(side * 0.115f, 0.01f, -0.01f),
                         new Vector3(0.04f, 0.09f, 0.09f), headsetMat);

            Prim.Box(pivot, "Mic Boom", new Vector3(-0.1f, -0.05f, 0.05f), new Vector3(0.02f, 0.02f, 0.13f),
                     new Vector3(0f, 25f, 0f), headsetMat);

            if (!string.IsNullOrEmpty(faceTexture))
            {
                // Deliberately smaller than the skull. Sized to match it, the photo covers the head
                // completely and the figure reads as a cutout on a snowman; leaving a margin means
                // the skull and hair frame the face the way a real head does.
                //
                // It also sits just clear of the sphere - at the same radius the two would fight
                // for the same pixels and flicker.
                var faceMat = Prim.Textured("Pilot Face", faceTexture, Color.white);
                Prim.NoShadow(Prim.Picture(pivot, "Face", new Vector3(0f, -0.012f, 0.118f), 0.135f, 0.202f, faceMat));
            }
            else
            {
                Prim.Box(pivot, "Nose", new Vector3(0f, -0.01f, 0.115f), new Vector3(0.04f, 0.05f, 0.04f), skin);
            }

            return pivot;
        }

        static Transform Head(Transform root, Vector3 pos, Material skin, Material hair)
        {
            var pivot = Prim.Empty(root, "Head", pos).transform;
            Prim.Sphere(pivot, "Skull", Vector3.zero, Vector3.one * 0.23f, skin);
            Prim.Sphere(pivot, "Hair", new Vector3(0f, 0.04f, -0.03f), new Vector3(0.235f, 0.2f, 0.22f), hair);
            Prim.Box(pivot, "Nose", new Vector3(0f, -0.01f, 0.115f), new Vector3(0.04f, 0.05f, 0.04f), skin);
            return pivot;
        }

        static void Idle(GameObject root, Transform head, int look)
        {
            var idle = root.AddComponent<PassengerIdle>();
            idle.head = head;
            idle.phase = look * 1.7f;
            idle.lookSpeed = 0.3f + (look % 5) * 0.05f;
        }
    }
}
