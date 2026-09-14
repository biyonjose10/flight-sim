using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// A "walk up and press E" spot: the boarding gate in scene 1, the throttles in scene 2.
    ///
    /// Every frame it asks one simple question: is the player standing inside my box? If yes,
    /// it shows its prompt on screen, and pressing E loads the next scene.
    ///
    /// It checks the box with plain maths rather than a physics trigger. That is easier to
    /// reason about (no Rigidbody, no layers, no trigger events), and you can see the box in the
    /// Scene view as a yellow wireframe.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("Zone")]
        [Tooltip("Size of the box, centred on this object, that the player has to stand in.")]
        public Vector3 zoneSize = new Vector3(2f, 2.4f, 2f);

        [Header("What it says")]
        public string prompt = "Press E";
        public KeyCode key = KeyCode.E;

        [Header("What it does")]
        [Tooltip("The scene to load when the key is pressed.")]
        public string sceneToLoad;
        [Tooltip("Shown instead if that scene isn't in the build yet.")]
        public string notBuiltMessage;

        PlayerController player;
        bool playerInside;

        /// <summary>True while the player is standing in the zone.</summary>
        public bool PlayerInside { get { return playerInside; } }

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning("[INTERACT] '" + name + "' found no player in the scene");
            }
        }

        void Update()
        {
            if (player == null) return;

            // Test the middle of the body rather than the feet, which sit right on the floor.
            bool inside = Contains(player.transform.position + Vector3.up * 0.9f);

            if (inside != playerInside)
            {
                playerInside = inside;
                Debug.Log("[INTERACT] Player " + (inside ? "entered" : "left") + " '" + name + "'");

                if (PromptHUD.Instance != null)
                {
                    if (inside) PromptHUD.Instance.ShowPrompt(this, prompt);
                    else PromptHUD.Instance.HidePrompt(this);
                }
            }

            if (playerInside && Input.GetKeyDown(key))
            {
                Use();
            }
        }

        /// <summary>Does what pressing the key does. The playtest calls this directly.</summary>
        public void Use()
        {
            Debug.Log("[INTERACT] Used '" + name + "' -> " + sceneToLoad);
            SceneFader.GoTo(sceneToLoad, notBuiltMessage);
        }

        /// <summary>Converts the point into this object's own space, then checks it against half the box size on each axis.</summary>
        bool Contains(Vector3 worldPoint)
        {
            Vector3 p = transform.InverseTransformPoint(worldPoint);

            return Mathf.Abs(p.x) <= zoneSize.x * 0.5f &&
                   Mathf.Abs(p.y) <= zoneSize.y * 0.5f &&
                   Mathf.Abs(p.z) <= zoneSize.z * 0.5f;
        }

        void OnDisable()
        {
            if (PromptHUD.Instance != null) PromptHUD.Instance.HidePrompt(this);
        }

        // Draws the zone in the Scene view so you can see where it is.
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, zoneSize);
        }
    }
}
