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
