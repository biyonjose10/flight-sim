using UnityEngine;
using L = FlightSim.FlightLayout.Landing;

namespace FlightSim.Build
{
    /// <summary>
    /// Builds scene 4, the approach and landing (Assets/Scenes/04_Landing.unity).
    ///
    /// This scene flies itself. You begin a few kilometres out on final approach and watch the
    /// runway come up to meet you, and you can look around and swap between the pilot's seat and
    /// the outside camera the whole way down. When the plane stops, the arrival card appears.
    ///
    /// It is deliberately almost the same scene as scene 3: the same world from WorldParts, the
    /// same aeroplane from SharedParts.Airliner, the same cockpit, the same camera and the same
    /// instruments. The only differences are where the plane starts, and that LandingSequence
    /// moves it instead of the keyboard.
    ///
    /// Every number comes from FlightLayout.Landing.
    /// </summary>
    public static class LandingBuilder
    {
        public static void Build()
        {
            var scene = SceneKit.NewScene();

            WorldParts.Sky();
            WorldParts.Build(new GameObject("World").transform);

            Transform plane = Aeroplane();
            var aircraft = plane.GetComponent<Aircraft>();
            var audio = plane.GetComponent<AircraftAudio>();

            SceneKit.FlyingCamera(plane);
            GameSystems(aircraft, audio);

            SceneKit.Save(scene, FlightLayout.LandingScene);
        }

        /// <summary>
        /// The same aeroplane as scene 3, put on final approach instead of on the runway. Its
        /// starting point is worked out from the approach speed and the length of the timeline,
        /// so the speed shown on the instruments is the speed it is really travelling.
        /// </summary>
        static Transform Aeroplane()
        {
            var root = new GameObject("Aeroplane");
            root.transform.position = L.PlaneStart;
            root.transform.rotation = Quaternion.Euler(-L.ApproachPitch, 90f, 0f);

            // The quarter turn that points the model where the aeroplane is going. See
            // FlightLayout.Plane.ModelYaw - without it the plane lands sideways.
            var model = Prim.Empty(root.transform, "Model", Vector3.zero).transform;
            model.localRotation = Quaternion.Euler(0f, FlightLayout.Plane.ModelYaw, 0f);

            Transform body = SharedParts.Airliner(model, Vector3.zero);

            var mount = Prim.Empty(body, "Cockpit", FlightLayout.Plane.CockpitOffset).transform;
            CockpitParts.Cockpit(mount);

            var aircraft = root.AddComponent<Aircraft>();
            aircraft.landingGear = body.Find("Landing Gear");
            aircraft.throttle = 0.25f;
            aircraft.inputEnabled = false;      // the landing is not flown by the player
            aircraft.scriptedFlight = true;     // LandingSequence moves it, so Aircraft stands back

            Sound(root.transform);
            SceneKit.PilotEye(root.transform);
            return root.transform;
        }

        static void Sound(Transform plane)
        {
            var holder = Prim.Empty(plane, "Sound", Vector3.zero).transform;

            var audio = plane.gameObject.AddComponent<AircraftAudio>();
            audio.idle = SceneKit.Loop(holder, "Engine Idle", AudioBank.JetIdle);
            audio.power = SceneKit.Loop(holder, "Engine Power", AudioBank.JetPower);
            audio.wind = SceneKit.Loop(holder, "Wind", AudioBank.Wind);
            audio.tyres = SceneKit.Loop(holder, "Tyres", AudioBank.TyreRoll);
            audio.reverse = SceneKit.Loop(holder, "Reverse Thrust", AudioBank.Reverse);
            audio.oneShots = SceneKit.OneShotSource(holder, "One Shots");
            audio.gearClip = AudioBank.Load(AudioBank.Gear);
            audio.touchdownClip = AudioBank.Load(AudioBank.Touchdown);
        }

        /// <summary>
        /// The HUD, the timeline and the arrival card, all on one object.
        ///
        /// The landing prompt is switched off here: L begins an approach, and you are already on
        /// one.
        /// </summary>
        static void GameSystems(Aircraft aircraft, AircraftAudio audio)
        {
            var hud = SceneKit.GameSystems(
                "Final Approach - Runway 09",
                "Sit back. Press V to watch from outside.",
                "V view    Mouse look    Esc free cursor");

            var flight = hud.gameObject.AddComponent<FlightHUD>();
            flight.offerLanding = false;
            flight.flightName = "FS 204";

            var card = hud.gameObject.AddComponent<EndCard>();

            var sequence = hud.gameObject.AddComponent<LandingSequence>();
            sequence.plane = aircraft;
            sequence.sound = audio;
            sequence.endCard = card;
        }
    }
}
