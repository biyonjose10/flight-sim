using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// Background sound for a room: one quiet loop that never stops, plus an occasional one-shot
    /// on top of it.
    ///
    /// That is really all an "atmosphere" is. The terminal gets a low hall hum with a boarding
    /// call every so often; the cabin gets air-conditioning hiss with the odd seatbelt chime. A
    /// silent room sounds broken, and a loop on its own starts to sound like a fault, so the
    /// occasional one-shot is what stops your ear noticing the loop.
    ///
    /// The gap between one-shots is deliberately random within a range. A sound that arrives on a
    /// perfectly regular beat is far more obvious than one that does not.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ambience : MonoBehaviour
    {
        [Header("The loop underneath everything")]
        public AudioSource bed;

        [Header("The occasional sound on top")]
        public AudioSource occasional;
        public AudioClip occasionalClip;

        [Tooltip("Shortest gap between one-shots, in seconds.")]
        public float minGap = 18f;
        [Tooltip("Longest gap between one-shots, in seconds.")]
        public float maxGap = 40f;
        [Range(0f, 1f)] public float occasionalVolume = 0.5f;

        float nextAt;

        void Start()
        {
            if (bed != null && bed.clip != null)
            {
                bed.loop = true;
                bed.Play();
            }

            ScheduleNext();
            Debug.Log("[AUDIO] Ambience running on '" + name + "'");
        }

        void Update()
        {
            if (occasional == null || occasionalClip == null) return;

            if (Time.time >= nextAt)
            {
                occasional.PlayOneShot(occasionalClip, occasionalVolume);
                Debug.Log("[AUDIO] " + occasionalClip.name);
                ScheduleNext();
            }
        }

        void ScheduleNext()
        {
            nextAt = Time.time + Random.Range(minGap, Mathf.Max(minGap + 0.1f, maxGap));
        }
    }
}
