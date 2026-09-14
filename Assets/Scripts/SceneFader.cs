using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlightSim
{
    /// <summary>
    /// Moves you from one scene to the next: fade to black, load the new scene, fade back in.
    ///
    /// Each scene has its own fader. The old one fades out and asks Unity to load the next
    /// scene, and the new scene's fader starts on black and fades in. Nothing has to survive the
    /// scene change, so there's no DontDestroyOnLoad to get wrong.
    ///
    /// If the next scene isn't in the build yet (scene 3, for now), it shows a message instead
    /// and you stay where you are, so the button never crashes the game.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [Tooltip("How long a fade to or from black takes, in seconds.")]
        public float fadeSeconds = 0.8f;

        float alpha = 1f;       // 1 = fully black. Every scene starts black and fades in.
        bool loading;
        Texture2D black;

        public bool IsLoading { get { return loading; } }

        void Awake()
        {
            Instance = this;

            black = new Texture2D(1, 1);
            black.SetPixel(0, 0, Color.white);   // tinted black when drawn
            black.Apply();
        }

        void Start()
        {
            Debug.Log("[SCENE] Now in '" + SceneManager.GetActiveScene().name + "'");
            StartCoroutine(Fade(1f, 0f));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (black != null) Destroy(black);
        }

        /// <summary>Go to another scene, or show notBuiltMessage if it doesn't exist yet.</summary>
        public static void GoTo(string sceneName, string notBuiltMessage)
        {
            if (Instance == null)
            {
                Debug.LogError("[SCENE] There is no SceneFader in this scene, so '" + sceneName + "' can't be loaded");
                return;
            }

            Instance.StartLoad(sceneName, notBuiltMessage);
        }

        void StartLoad(string sceneName, string notBuiltMessage)
        {
            if (loading) return;   // already on the way out; ignore repeated presses

            // True only for scenes listed in File > Build Profiles (Build Settings).
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.Log("[SCENE] '" + sceneName + "' is not in the build yet - staying here");

                string text = string.IsNullOrEmpty(notBuiltMessage)
                    ? "'" + sceneName + "' hasn't been built yet."
                    : notBuiltMessage;

                if (PromptHUD.Instance != null) PromptHUD.Instance.ShowMessage(text, 5f);
                return;
            }

            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            Debug.Log("[SCENE] Loading '" + sceneName + "'");

            // Stop the player wandering off while the screen goes dark.
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.inputEnabled = false;

            yield return Fade(0f, 1f);

            SceneManager.LoadScene(sceneName);
        }

        IEnumerator Fade(float from, float to)
        {
            float t = 0f;

            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }

            alpha = to;
        }

        void OnGUI()
        {
            if (alpha <= 0.001f) return;

            GUI.depth = -1000;   // lower depth draws on top, so the black covers the HUD too
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), black);
            GUI.color = Color.white;
        }
    }
}
