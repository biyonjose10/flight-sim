using UnityEngine;
using C = FlightSim.FlightLayout.FlightCam;

namespace FlightSim
{
    /// <summary>
    /// The two ways of watching the flight, in both scene 3 and scene 4. Press V to swap.
    ///
    ///   * **Pilot's seat** - the camera sits where the pilot's head would be, inside the same
    ///     cockpit you walked into in scene 2. The mouse turns your head, within the limits a neck
    ///     actually has, so you can look out of the side window but never right through your own
    ///     seat.
    ///
    ///   * **Chase** - the camera hangs outside the aeroplane and follows it. The mouse swings it
    ///     around so you can look at the plane from any angle, and the scroll wheel moves it
    ///     closer or further away.
    ///
    /// The camera is NOT a child of the aeroplane. It is placed by hand every frame in
    /// LateUpdate, which runs after the plane has finished moving. Parenting it would work for the
    /// cockpit view, but the chase camera has to lag behind the plane to look smooth, and a child
    /// object cannot lag behind its own parent.
    ///
    /// The chase view deliberately ignores the plane's roll. If the camera rolled with the plane,
    /// banking would spin the whole picture and make the horizon useless.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlightCamera : MonoBehaviour
    {
        /// <summary>The camera in the current scene, so the HUD can ask which view is showing.</summary>
        public static FlightCamera Instance { get; private set; }

        [Header("What to follow")]
        [Tooltip("The aeroplane. Set by the scene builder.")]
        public Transform target;

        [Tooltip("Where the pilot's head sits inside the aeroplane. Set by the scene builder.")]
        public Transform eye;

        [Header("Looking")]
        public float mouseSensitivity = 2f;

        [Tooltip("Switched off while the screen fades between scenes.")]
        public bool inputEnabled = true;

        bool chaseView;          // false is the pilot's seat, true is the camera outside
        float lookYaw, lookPitch;    // where the pilot's head is turned, in degrees
        float orbitYaw, orbitPitch;  // where the chase camera hangs, in degrees
        float distance;

        /// <summary>True when the outside camera is showing.</summary>
        public bool ChaseView { get { return chaseView; } }

        /// <summary>The name of the view on screen, for the HUD.</summary>
        public string ViewName { get { return chaseView ? "CHASE" : "COCKPIT"; } }

        void Awake()
        {
            Instance = this;

            orbitYaw = FlightLayout.FlightCam.StartOrbitYaw;
            orbitPitch = FlightLayout.FlightCam.StartOrbitPitch;
            distance = FlightLayout.FlightCam.ChaseDistance;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            LockCursor(true);
            Debug.Log("[CAMERA] Starting in the pilot's seat. Press V for the outside view.");
        }

        void Update()
        {
            UpdateCursor();

            if (!inputEnabled) return;

            if (Input.GetKeyDown(FlightLayout.FlightCam.ToggleKey) || VirtualInput.ConsumeView()) Toggle();

            // A phone has no cursor to lock, so the drag has to be added before that test - put it
            // after and the touch controls would silently do nothing on the only platform they
            // exist for.
            float mx = VirtualInput.LookDelta.x;
            float my = VirtualInput.LookDelta.y;

            // Android reports the primary touch as the mouse too, so without this the
            // joystick finger swung the camera around as well as flying the plane.
            if (!VirtualInput.Active && Cursor.lockState == CursorLockMode.Locked)
            {
                mx += Input.GetAxis("Mouse X") * mouseSensitivity;
                my += Input.GetAxis("Mouse Y") * mouseSensitivity;
            }

            if (Mathf.Abs(mx) < 0.0001f && Mathf.Abs(my) < 0.0001f &&
                Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) < 0.0001f) return;


            if (chaseView)
            {
                orbitYaw += mx;
                orbitPitch = Mathf.Clamp(orbitPitch - my, C.MinOrbitPitch, C.MaxOrbitPitch);

                // The scroll wheel pulls the camera in and pushes it out.
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f)
                    distance = Mathf.Clamp(distance - scroll * C.ZoomRate,
                                           C.ChaseMinDistance, C.ChaseMaxDistance);
            }
            else
            {
                lookYaw = Mathf.Clamp(lookYaw + mx, -C.CockpitYawLimit, C.CockpitYawLimit);
                lookPitch = Mathf.Clamp(lookPitch - my, -C.CockpitPitchLimit, C.CockpitPitchLimit);
            }
        }

        /// <summary>
        /// Runs after everything else has moved. Placing the camera here rather than in Update is
        /// what stops the picture juddering: if the camera moved first, it would be following
        /// where the plane was a frame ago.
        /// </summary>
        void LateUpdate()
        {
            if (target == null) return;

            if (chaseView) PlaceChaseCamera();
            else PlaceCockpitCamera();
        }

        void PlaceCockpitCamera()
        {
            // The eye marker is a child of the aeroplane, so it already carries the plane's
            // attitude. Falling back to the plane's own centre keeps the game running rather than
            // throwing if a scene was built without one.
            Transform seat = eye != null ? eye : target;

            transform.position = seat.position;
            transform.rotation = seat.rotation * Quaternion.Euler(lookPitch, lookYaw, 0f);
        }

        void PlaceChaseCamera()
        {

            // Only the plane's heading is used, not its pitch or roll, so banking and climbing
            // swing the aeroplane inside the frame instead of swinging the whole world.
            float heading = target.eulerAngles.y;
            Quaternion around = Quaternion.Euler(orbitPitch, heading + orbitYaw, 0f);

            Vector3 focus = target.position + Vector3.up * (FlightLayout.Plane.FuselageRadius * 0.5f);
            Vector3 wanted = focus + around * new Vector3(0f, 0f, -distance) + Vector3.up * C.ChaseHeight;

            // Ease towards the wanted spot instead of snapping to it. Framerate-independent, so it
            // feels the same on a fast machine and a slow one.
            float blend = 1f - Mathf.Exp(-C.FollowSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, wanted, blend);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        /// <summary>Swaps the view. Also snaps the chase camera into place, so V never shows a sweep.</summary>
        public void Toggle()
        {
            chaseView = !chaseView;

            if (chaseView && target != null)
            {
                // Put it straight where it belongs, otherwise the first frame eases in from
                // wherever the pilot's head happened to be.
                //
                // The rotation has to be set here too, not just the position. Leaving it out
                // means the first frame still points wherever the pilot was looking - straight
                // ahead - so pressing V gives you a view from behind the plane with the plane
                // itself off the edge of the screen.
                float heading = target.eulerAngles.y;
                Quaternion around = Quaternion.Euler(orbitPitch, heading + orbitYaw, 0f);
                Vector3 focus = target.position + Vector3.up * (FlightLayout.Plane.FuselageRadius * 0.5f);
                transform.position = focus + around * new Vector3(0f, 0f, -distance) + Vector3.up * C.ChaseHeight;
                transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            }

            Debug.Log("[CAMERA] View is now " + ViewName);
        }

        // Esc frees the mouse so you can leave the window; clicking captures it again. Copied from
        // PlayerController so the two halves of the game behave the same way.
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
    }
}
