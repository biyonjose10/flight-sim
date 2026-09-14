using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Keeps the passengers from looking like statues: each one slowly turns its head now and
    /// then.
    ///
    /// Two sine waves at different speeds are multiplied together. The result spends most of its
    /// time near zero (looking ahead) with an occasional glance to one side, which looks a lot
    /// less mechanical than a single sine wave. Each passenger gets a different phase so they
    /// don't all turn at once.
    /// </summary>
    public class PassengerIdle : MonoBehaviour
    {
        [Tooltip("The head pivot to turn.")]
        public Transform head;
        [Tooltip("Furthest the head turns, in degrees.")]
        public float lookAngle = 35f;
        public float lookSpeed = 0.4f;
        [Tooltip("Where in the cycle this passenger starts. Set differently for everyone by the scene builder.")]
        public float phase = 0f;

        Quaternion headRest;

        void Start()
        {
            if (head != null) headRest = head.localRotation;
        }

        void Update()
        {
            if (head == null) return;

            float t = Time.time * lookSpeed + phase;
            float turn = Mathf.Sin(t) * Mathf.Sin(t * 0.37f) * lookAngle;

            head.localRotation = headRest * Quaternion.Euler(0f, turn, 0f);
        }
    }
}
