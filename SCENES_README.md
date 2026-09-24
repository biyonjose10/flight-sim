# Flight Sim: the four scenes

The technical detail behind the scenes: where things are, and why they're built the way they are.
For running the project see `README.md`. For a walkthrough of the code see `HOW_IT_WORKS.md`.

## Scene 1: `Assets/Scenes/01_Terminal.unity`

A departure hall at Gate 7. The floor is at y = 0 and **+Z points out of the window**.

| | Value | Why |
|---|---|---|
| Hall | x −20 … +20, z −7 … +7, ceiling 6 m | Big enough to wander, small enough to cross in ~15 s |
| Window | glass from y 0.4 to 5.6 along z = 7 | Almost floor to ceiling, so the plane is the first thing you see |
| Apron | y = −4, four metres below the hall | Real terminals are raised, so you look *down* onto the plane |
| Player start | (0, 0, −5), facing +Z | In the middle, looking straight at the plane |
| Benches | x = −12, −6.5, 6.5, 12; rows z = −3.5 and −0.5 | Leaves a clear walkway from x −4.3 to +4.3 |
| Gate door | x = 14, 2 m wide, 2.6 m high | Lines up with the plane's front door |
| Boarding zone | centre (14, 1.2, 5.2), size 3 × 2.4 × 2.6 | The patch of floor in front of the door |
| Plane | centre (0, −0.2, 30), fuselage 36 m long, radius 2 m | A320-sized, parked side-on to the window |
| Runway | centre z = 85, 45 m wide, 1200 m long | Beyond the plane, fading into haze at both ends |

## Scene 2: `Assets/Scenes/02_Cabin.unity`

Inside the plane. The floor is at y = 0 and **+X points to the nose**. Negative Z is the left-hand
side (the door side).

| | Value | Why |
|---|---|---|
| Cabin | x −12.6 … 6.8, 3.9 m wide, ceiling 2.25 m | A320 cabin width |
| Seats | 20 rows, 0.8 m apart, first row x = 3.2 | Standard economy seat pitch (~31 inches) |
| Seat centres (z) | ±0.55, ±1.05, ±1.55 | Three each side of the aisle |
| Aisle | 0.62 m wide (seat blocks start at z = ±0.31) | Just wider than the player (0.5 m) |
| Windows | one per row, 0.35 m wide, y 0.95 … 1.4 | One per row, so they line up with the seats |
| Player start | (4.6, 0, −0.9), facing 68° | Just inside the boarding door, looking at the cockpit door |
| Bulkhead | x = 6.8, doorway 0.9 m wide × 2 m high | Wall between cabin and cockpit |
| Cockpit | x 6.8 … 10.4, 3.2 m wide | Two seats, panel, pedestal, windscreen |
| Take-off zone | centre (8.2, 1, 0), size 1.8 × 2 × 1 | Standing between the pilot seats |
| Ground | y = −3.6 | The cabin floor sits about door-sill height above the apron |

All of these live in `Assets/Scripts/FlightLayout.cs`. **Change them there and rebuild.**

## Scenes 3 and 4: `03_Takeoff.unity` and `04_Landing.unity`

Both are built from the same three shared pieces, so they are almost the same scene.

**The shared world** (`FlightLayout.World`, built by `WorldParts`): one runway along **X**, centred
on the origin, 2600 m long and 45 m wide. Heading **90°** points the nose along +X. Land lies to the
west, and the coast is at x = 6000 with the sea beyond it at y = −4. Clouds sit between 800 m and
1500 m, and the haze runs from 900 m out to 14 km.

**The aeroplane** (`FlightLayout.Plane`): `SharedParts.Airliner`, 36 m long, 2 m fuselage radius,
origin at the middle of the fuselage. Its wheels hang 3.85 m below that origin, which is why the
plane is parked at y = 3.85 rather than y = 0. `CockpitOffset` (7.6, −1.1, 0) is the shift that
drops the scene-2 cockpit interior into the nose of the exterior shell so the two line up.

**Scene 3** starts 120 m past the threshold with the engines idling. The numbers that decide how it
flies are in `FlightLayout.Flight`:

| | |
|---|---|
| Full-throttle acceleration | 4.5 m/s² |
| Drag | `0.000066 × speed²`, which balances thrust at about 260 m/s |
| Rotation speed | 75 m/s (about 146 kt) - below it the nose will not come up |
| Pitch | −20°…+25°, at 22°/s, returning to level at 18°/s |
| Roll | ±55°, at 50°/s, returning to level at 18°/s |
| Gear | comes up above 20 m, but **only while climbing** |
| Landing prompt | gear up and above 300 m |

**Scene 4** is a timeline, not a flight. Its start position is **derived** from the speed and the
durations (`ApproachSpeed × (DescentSeconds + FlareSeconds)`), which puts it 2808 m out at 180 m.
That matters: if the distance and the speed disagreed, the plane would have to cheat to arrive on
time and the speed on the instruments would be a lie. The flare height (14 m) falls out of the same
sum rather than being typed in.

## Design decisions

**Scenes are generated, not hand-placed.** The builders in `Assets/Scripts/Editor/` create each
scene from nothing every time. That makes rebuilds repeatable, lets you undo any accident by
rebuilding, keeps every number in one readable file, and means the whole scene can be explained by
reading code. The cost is that edits made in the editor don't survive a rebuild.

**Everything is made from primitive shapes.** Free airliner *interiors* basically don't exist on
the Asset Store, and mixing a downloaded terminal with a home-made cabin would give two art styles
side by side. Building everything from cubes, cylinders and spheres keeps one consistent low-poly
look, costs nothing, and means there's nothing to download before it runs.

**The player is a CharacterController, not a Rigidbody.** A CharacterController walks, slides along
walls and climbs small steps with no physics tuning, and can't be knocked over. A Rigidbody would
need friction, drag and rotation locks to stop the player tumbling.

**Zones use a box check, not physics triggers.** `Interactable` converts the player's position into
its own local space and checks it against half the box size on each axis. Trigger events need a
Rigidbody or CharacterController on the right object, the right layers and the right callback, and
fail silently when any of those is wrong. The box check has no hidden requirements and is one
readable function.

**The HUD uses OnGUI.** A Canvas needs a Canvas, a Canvas Scaler, an EventSystem and a Text object
wired together in the editor. OnGUI is one script that draws the prompt, the message, the title and
the crosshair.

**Every scene has its own fader.** The old scene fades to black and loads the next, and the next
scene starts black and fades in. Nothing has to survive between scenes, so there is no
`DontDestroyOnLoad` object to get wrong (a bug that cost a rebuild on an earlier project).

**Taking off to a scene that doesn't exist yet is handled, not ignored.**
`Application.CanStreamedLevelBeLoaded` tells the fader whether scene 3 is in the build. If it
isn't, a message appears instead of an error. When scene 3 is added to Build Settings, the same
button starts working with no code change.

**Ambient light is "trilight".** Separate sky, horizon and ground colours. Ambient light sampled
from the skybox is only correct after a lighting bake, and these scenes are never baked.

**Signs use a custom text shader** (`Assets/Shaders/Text3D.shader`). Unity's built-in 3D text
material draws on top of everything, so the departures board would show through walls. The custom
shader is identical except that it respects depth.

**Glass needs its render mode set in code.** Picking "Fade" in the material inspector sets several
hidden values at once. Setting only the mode from code does nothing, so `Prim.Glass` sets the blend
modes, keywords and render queue itself.

**Glass, lights and screens don't cast shadows**, so sunlight still comes through the windows.

## Verification

- `Tools → Flight Sim → Snapshot Scenes` renders 14 fixed views to `Snapshots/` (committed, so they
  can be reviewed without Unity). The snapshot camera's far clip is 22 km, not the 2.5 km the two
  indoor scenes needed - at 2.5 km the sea, the hills and the distant countryside disappear into
  the skybox and look as though they were never built.
- `Tools → Flight Sim → Playtest` plays the whole game by itself: it walks to the gate, boards,
  walks the cabin into the cockpit, takes off on the aeroplane's autopilot, climbs past 300 m,
  checks both cameras, begins the approach, and watches the landing through to the arrival card. It
  fails on any runtime error, if the player gets stuck, if a zone can't be reached, if a scene never
  loads, if the plane crashes, or if the landing never finishes.

## Not done yet

- Walking passengers.
- Nothing to collide with in the air: clouds and tower blocks are scenery, and only the ground ends
  a flight.
- No weather, no time of day, no other traffic.
