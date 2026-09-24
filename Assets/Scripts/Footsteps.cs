using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// A footstep sound every time the player has walked far enough to have taken a step.
    ///
    /// It counts DISTANCE rather than time. If it used a timer, walking into a wall would still
    /// produce footsteps while you stood still, and the steps would fall out of time with your
    /// speed. Distance is also how a real pace works: a step happens every so many metres,
    /// whatever else is going on.
    ///
    /// Each step is played at a slightly random pitch and volume. Exactly the same sound repeated
    /// is instantly recognisable as a recording being replayed, and a tiny variation is enough to
    /// hide that.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class Footsteps : MonoBehaviour
    {
        [Header("Sound")]
        public AudioSource source;
        public AudioClip step;

        [Tooltip("How far you walk between one footstep and the next, in metres.")]
        public float strideLength = 0.75f;
        [Range(0f, 1f)] public float volume = 0.35f;

        [Tooltip("How much the pitch wanders, so the steps don't sound identical.")]
        public float pitchVariation = 0.12f;

        CharacterController controller;
        Vector3 lastPosition;
        float walked;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            lastPosition = transform.position;
        }

        void Update()
        {
            if (source == null || step == null) return;

            // Only count movement along the floor. Otherwise being pushed up a step, or settling
            // under gravity, would add to the distance walked.
            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            lastPosition = transform.position;

            if (!controller.isGrounded) return;

            walked += moved.magnitude;

            if (walked >= strideLength)
            {
                walked = 0f;
                source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                source.PlayOneShot(step, volume * Random.Range(0.8f, 1f));
            }
        }
    }
}
