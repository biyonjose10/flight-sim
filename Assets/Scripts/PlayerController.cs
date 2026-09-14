using UnityEngine;

namespace FlightSim
{
    /// <summary>
    /// First-person walking: WASD moves you, the mouse turns your head.
    ///
    /// It uses Unity's CharacterController rather than a Rigidbody. A CharacterController slides
    /// along walls and climbs small steps without any physics tuning, and it can't be knocked
    /// over or sent spinning, which is exactly what you want for a person walking around a
    /// building.
    ///
    /// The whole body turns left and right (yaw); only the camera tilts up and down (pitch).
    /// That keeps "forward" flat, so looking at the ceiling doesn't make you walk into the air.
    ///
    /// There is also an autopilot (WalkTo) that the automated playtest uses to walk the route
    /// without a keyboard. A player never triggers it.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Walking")]
        [Tooltip("Metres per second. 2.4 is a relaxed walk.")]
        public float walkSpeed = 2.4f;
        [Tooltip("Pulls you down onto the floor. Metres per second, per second.")]
        public float gravity = -9.81f;

        [Header("Looking")]
        [Tooltip("The camera at eye height. Tilts up and down; the body handles left and right.")]
        public Transform playerCamera;
        public float mouseSensitivity = 2f;
        [Tooltip("How far you can look up or down, in degrees.")]
        public float maxLookUpDown = 80f;

        [Header("State")]
        [Tooltip("Switched off while the screen fades to black between scenes.")]
        public bool inputEnabled = true;

        CharacterController controller;
        float yaw;          // left/right, in degrees
        float pitch;        // up/down, in degrees
        float fallSpeed;    // vertical speed from gravity

        bool hasAutopilotTarget;
        Vector3 autopilotTarget;

        /// <summary>True while the autopilot is still walking to the point it was given.</summary>
        public bool IsWalkingToTarget { get { return hasAutopilotTarget; } }

        void Start()
        {
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            LockCursor(true);

            Debug.Log("[PLAYER] Spawned at " + transform.position + ", facing " + Mathf.RoundToInt(yaw) + " degrees");
        }

        void Update()
        {
            UpdateCursor();
            Look();
            Walk();
        }

        /// <summary>Autopilot: walk in a straight line to this point. Used by the playtest.</summary>
        public void WalkTo(Vector3 point)
        {
            autopilotTarget = point;
            hasAutopilotTarget = true;
            Debug.Log("[PLAYER] Autopilot walking to " + point);
        }

        // Esc frees the mouse so you can leave the game window; clicking captures it again.
        void UpdateCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(false);
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Look()
        {
            bool mouseControlsView = inputEnabled && !hasAutopilotTarget &&
                                     Cursor.lockState == CursorLockMode.Locked;

            if (mouseControlsView)
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, -maxLookUpDown, maxLookUpDown);
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        void Walk()
        {
            Vector3 move = Vector3.zero;

            if (hasAutopilotTarget)
            {
                Vector3 toTarget = autopilotTarget - transform.position;
                toTarget.y = 0f;

                if (toTarget.magnitude < 0.3f)
                {
                    hasAutopilotTarget = false;
                    Debug.Log("[PLAYER] Autopilot reached " + autopilotTarget);
                }
                else
                {
                    move = toTarget.normalized;

                    // Turn to face where we're walking, so playtest screenshots look ahead.
                    float wantedYaw = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                    yaw = Mathf.MoveTowardsAngle(yaw, wantedYaw, 360f * Time.deltaTime);
                    pitch = Mathf.MoveTowards(pitch, 0f, 90f * Time.deltaTime);
                }
            }
            else if (inputEnabled)
            {
                // GetAxisRaw gives -1, 0 or 1 straight from the keys (A/D and W/S, or the arrows).
                float right = Input.GetAxisRaw("Horizontal");
                float forward = Input.GetAxisRaw("Vertical");

                move = transform.right * right + transform.forward * forward;

                // Without this, walking diagonally would be about 1.4 times faster.
                if (move.sqrMagnitude > 1f) move.Normalize();
            }

            // Gravity. While standing on something, press down gently so we stay grounded.
            if (controller.isGrounded) fallSpeed = -1f;
            else fallSpeed += gravity * Time.deltaTime;

            controller.Move((move * walkSpeed + Vector3.up * fallSpeed) * Time.deltaTime);
        }
    }
}
