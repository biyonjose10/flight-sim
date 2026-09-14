using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlightSim.Build
{
    /// <summary>
    /// The building blocks. The terminal, the plane and the people are all made from Unity's
    /// built-in shapes (cubes, cylinders, spheres, capsules), and this file makes those shapes
    /// quick to place and colour.
    ///
    /// "solid" decides whether a shape keeps its collider. Walls, floors and seat blocks are
    /// solid so you can't walk through them. Decoration isn't, because thousands of colliders you
    /// could never touch are wasted work for the physics engine.
    /// </summary>
    public static class Prim
    {
        public const string MaterialDir = "Assets/Materials";

        /// <summary>Cylinder rotations, named by the world axis the cylinder ends up lying along.</summary>
        public static readonly Vector3 AxisX = new Vector3(0f, 0f, 90f);
        public static readonly Vector3 AxisY = Vector3.zero;
        public static readonly Vector3 AxisZ = new Vector3(90f, 0f, 0f);

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static void ResetCache()
        {
            Cache.Clear();
        }

        // ------------------------------------------------------------------ materials

        /// <summary>A plain coloured surface.</summary>
        public static Material Mat(string name, Color albedo, float metallic = 0f, float smoothness = 0.25f)
        {
            return Build(name, albedo, metallic, smoothness, null, false);
        }

        /// <summary>A surface that glows: screens, light panels, runway lights.</summary>
        public static Material Emissive(string name, Color albedo, Color emission, float smoothness = 0.6f)
        {
            return Build(name, albedo, 0f, smoothness, emission, false);
        }

        /// <summary>See-through glass. Alpha in the colour sets how see-through it is.</summary>
        public static Material Glass(string name, Color tint)
        {
            return Build(name, tint, 0f, 0.9f, null, true);
        }

        static Material Build(string name, Color albedo, float metallic, float smoothness, Color? emission, bool transparent)
        {
            Material hit;
            if (Cache.TryGetValue(name, out hit) && hit != null) return hit;

            Directory.CreateDirectory(MaterialDir);
            string path = MaterialDir + "/" + name + ".mat";

            // Update an existing material in place rather than replacing it. CreateAsset deletes
            // whatever is already at the path, and both scenes share one palette, so recreating a
            // material would leave the scene built first pointing at a deleted object.
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Standard"));

            m.name = name;
            m.color = albedo;
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);

            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", emission.Value);
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                m.SetColor("_EmissionColor", Color.black);
            }

            if (transparent) MakeFade(m);

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);

            Cache[name] = m;
            return m;
        }

        /// <summary>
        /// The Standard shader's "Fade" rendering mode. Picking Fade in the material inspector
        /// sets all of this for you, but setting only _Mode from code does nothing on its own:
        /// the blend settings, keywords and render queue have to be set too, or the glass draws
        /// solid.
        /// </summary>
        static void MakeFade(Material m)
        {
            m.SetFloat("_Mode", 2f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        // ----------------------------------------------------------------- primitives

        public static GameObject Spawn(PrimitiveType type, Transform parent, string name,
                                       Vector3 pos, Vector3 scale, Quaternion rot, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            if (!solid)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool solid = false)
        {
            return Spawn(PrimitiveType.Cube, parent, name, pos, size, Quaternion.identity, mat, solid);
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Vector3 euler, Material mat, bool solid = false)
        {
            return Spawn(PrimitiveType.Cube, parent, name, pos, size, Quaternion.Euler(euler), mat, solid);
        }

        /// <summary>
        /// Unity's Cylinder mesh is 2 units tall and 1 unit across, so a cylinder of radius r
        /// and length L needs a scale of (2r, L/2, 2r). This hides that.
        /// </summary>
        public static GameObject Cyl(Transform parent, string name, Vector3 pos,
                                     float radius, float length, Vector3 euler, Material mat, bool solid = false)
        {
            return Spawn(PrimitiveType.Cylinder, parent, name, pos,
                         new Vector3(radius * 2f, length * 0.5f, radius * 2f),
                         Quaternion.Euler(euler), mat, solid);
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            return Spawn(PrimitiveType.Sphere, parent, name, pos, size, Quaternion.identity, mat, false);
        }

        /// <summary>Capsule mesh is 2 tall and 1 across, the same as the cylinder.</summary>
        public static GameObject Capsule(Transform parent, string name, Vector3 pos,
                                         float diameter, float height, Material mat)
        {
            return Spawn(PrimitiveType.Capsule, parent, name, pos,
                         new Vector3(diameter, height * 0.5f, diameter),
                         Quaternion.identity, mat, false);
        }

        public static GameObject Empty(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        public static GameObject Empty(Transform parent, string name, Vector3 pos, Vector3 euler)
        {
            var go = Empty(parent, name, pos);
            go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        /// <summary>An invisible wall: a collider with nothing to draw. Keeps you out of the seat rows.</summary>
        public static GameObject Blocker(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = Empty(parent, name, pos);
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>A glowing shape that doesn't cast shadows (glass and lights shouldn't).</summary>
        public static GameObject NoShadow(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go;
        }

        // ----------------------------------------------------------------------- text

        public static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        /// <summary>
        /// Text that hides behind walls. The font's own material draws over everything, so this
        /// uses Assets/Shaders/Text3D.shader instead and falls back to the font's material if
        /// that shader is missing.
        /// </summary>
        static Material TextMaterial(Font font)
        {
            var shader = Shader.Find("FlightSim/Text3D");
            if (shader == null) return font.material;

            const string path = MaterialDir + "/Text3D.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(shader);

            m.shader = shader;
            m.mainTexture = font.material.mainTexture;

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// A 3D sign. Text faces -Z, so it reads correctly to someone looking towards +Z.
        /// Rotate it 180 degrees on Y for a sign read from the other side.
        /// </summary>
        public static GameObject Text3D(Transform parent, string name, Vector3 pos, Vector3 euler,
                                        string content, float size, Color color,
                                        TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var font = BuiltinFont();
            if (font == null) return null;

            var go = Empty(parent, name, pos, euler);

            var tm = go.AddComponent<TextMesh>();
            tm.text = content;
            tm.font = font;
            tm.fontSize = 72;
            tm.characterSize = size;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft
                ? TextAlignment.Left : TextAlignment.Center;
            tm.color = color;

            go.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial(font);
            return go;
        }
    }
}
