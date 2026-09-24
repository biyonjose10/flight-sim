# CLAUDE.md

Guidance for Claude Code working in this repository.

Read `README.md` first (running it), then `SCENES_README.md` (layout and design decisions) and
`HOW_IT_WORKS.md` (the teacher-facing walkthrough; keep it accurate when code changes).

## What this is

A four-scene first-person flight sim for a **college assignment the user must explain to a
teacher**. All four scenes are built:

1. **Terminal** - walk the departure hall, look at the plane, press E at the gate.
2. **Cabin** - walk the aisle, press E between the pilot seats.
3. **Take-off** - you fly it yourself (assisted arcade), V swaps pilot's seat / chase camera,
   L begins the landing once the gear is up above 300 m.
4. **Landing** - a scripted approach and touchdown you watch, ending on an arrival card; R flies
   the whole thing again.

Unity **6000.0.83f1**, **Built-in RP**, old Input Manager (`activeInputHandler: 0`), no packages
beyond Unity's built-in modules. The editor lives at `~/Unity/Editors/6000.0.83f1/`.

Explainability is a hard requirement: every script has a plain-English summary, inspector fields
carry tooltips, every runtime step logs a tag (`[PLAYER] [SCENE] [INTERACT] [HUD] [FLIGHT]
[CAMERA] [AUDIO] [PANEL] [LANDING] [WORLD]`), and every number sits in `FlightLayout.cs`. Keep new
code to that standard.

**Sound is generated, not recorded.** `Editor/AudioBank.cs` synthesises every clip from sine waves
and noise and writes real `.wav` files into `Assets/Audio/`. Nothing is downloaded and nothing is
licensed. `BuildAll.Build` regenerates them before building the scenes.

**Shared pieces live in shared files**: `SharedParts.Airliner` is the one aeroplane (scenes 1, 3
and 4), `CockpitParts.Cockpit` is the one cockpit interior (scenes 2, 3 and 4), and `WorldParts`
is the one airfield (scenes 3 and 4). Do not copy geometry into a scene builder.

## The rules that bite

- **Scene files are build output.** `TerminalBuilder`/`CabinBuilder` create them from nothing.
  Editing a scene in the inspector is undone by the next Build Scenes. Change the builder or `FlightLayout`.
- **Shared materials are updated in place, never recreated.** `AssetDatabase.CreateAsset` deletes
  what's at the path first, and both scenes are built in one run off one palette.
- **`CharacterController.minMoveDistance` must be 0** (set in `SceneKit.Player`). The default
  0.001 m discards any smaller move, and a headless play-mode run renders thousands of frames a
  second, so every step was discarded and the playtest reported the player "stuck" in open floor.
  It was first misread as a wall-clock timing problem. The playtest also uses game time
  (`Time.time`) for its stuck timer, which is correct, but that was not the cause.
- **Run Unity from PowerShell with `Start-Process -Wait`.** Unity.exe is a GUI binary, so `& $u`
  returns immediately. Snapshot and Playtest need graphics: no `-nographics`.
- **Glass**: `Prim.Glass` sets the Fade blend state and keywords explicitly. Setting `_Mode` alone
  renders opaque.
- **TextMesh faces −Z.** It reads correctly to a viewer looking along +Z. Rotate Y 180 for a viewer
  looking −Z, 90 for +X, −90 for −X. Signs use `FlightSim/Text3D` so they don't draw through walls.
- **Cylinder and Capsule meshes are 2 tall, 1 wide.** Use `Prim.Cyl` / `Prim.Capsule`.
- **Cabin aisle clearance is tight by design**: seat blocks start at z = ±0.31 and the player
  radius is 0.25. Widening the player breaks the cabin walk, and the playtest will say so.
- **`SceneFader` still refuses to load a scene that isn't in Build Settings**, showing a message
  instead of crashing. That safety net stays; `BuildAll.Scenes` now lists all four scenes.
- **`Aircraft.scriptedFlight` hands the plane over to `LandingSequence`.** When it is true the
  flight model does not run at all. Without it, `Aircraft.Fly` adds its own forward step on top of
  the one the landing timeline just set, so the plane travels at roughly double speed - or not,
  depending on which script Unity updated first, which is worse than a consistent bug.
- **The gear only auto-retracts while climbing** (`VerticalSpeed > 0`). Scene 4 starts 180 m up
  with the wheels down; a height test on its own folds them away on short final.
- **The landing's start position is derived, not typed.** `FlightLayout.Landing.PlaneStart` is
  computed from `ApproachSpeed x (DescentSeconds + FlareSeconds)`. If you hardcode a distance that
  disagrees with the speed, the plane has to cheat to arrive on time and the speed on the
  instruments becomes a lie.
- **A looping sound whose end does not join its start clicks once per loop.** `AudioBank` snaps
  every tone to a whole number of cycles per clip and crossfades each noise layer's tail into a
  generated pre-roll. Do not "simplify" that away.
- **The artificial horizon overflows its screen when it rotates.** `CockpitParts` hides the
  overflow behind a bezel of boxes drawn slightly in front of the panel. Moving the screens without
  moving the bezel breaks it.
- **Heading 90 degrees points the nose along +X**, which is the way both runways run. `Aircraft`
  reads its starting heading off the transform the builder placed, so the scene and the flight
  model cannot disagree.
- **The aeroplane model is built nose-along-+X, but Unity flies along +Z.** The flying scenes put
  the shell and the cockpit under a `Model` child turned by `FlightLayout.Plane.ModelYaw` (−90°).
  Leave it out and the plane travels **sideways**: the fuselage sits across the runway and the
  cockpit lands 7.6 m to one side of the pilot's head, so the cockpit view shows open sky. Nothing
  errors, the playtest still passes, and only a screenshot catches it — **look at the snapshots**.
- **`CockpitOffset` and `EyeLocal` are measured in the model's +X frame**, not the aeroplane's.
  Convert with `FlightLayout.Plane.ModelToPlane` before using them against the plane's transform.
- **Runway numbers only read correctly from straight down the runway.** Judge
  `Snapshots/3a_on_the_runway.png`, not an oblique view - seen from the side, "09" looks like "60"
  whatever the rotation actually is, and "fixing" it from that angle breaks a correct value.
- **Judge anything visual from a camera the player can actually reach.** The captain looked wrong
  from a snapshot placed in front of his face - a spot no player can stand in. From between the
  seats, where you really are, he was fine. Same class of mistake as the runway numbers.
- **The captain's head is turned ~52° towards the empty seat on purpose.** You enter the cockpit
  from behind; facing forward he shows you the back of his skull and the photographed face is never
  seen. The face card is also deliberately smaller than the skull, so head and hair frame it.
- **`Prim.Textured` is the only textured material in the project.** It loads from
  `Assets/Textures/` and degrades to a plain colour with a warning if the file is missing, because
  the scenes are build output and a missing asset must not stop a build.
- **Snapshot cameras are fixed world positions.** Moving something in `FlightLayout` (the pilot's
  eye, for instance) does not move them - update `SceneSnapshot` too, or you will review a stale
  viewpoint and think nothing changed.
- **Edits can land after a background build has already started.** Twice a "fix" appeared not to
  work because the build read the old file. Check the built scene actually contains the change
  (`grep m_Name:` in the `.unity`) before concluding the fix was wrong.

## Verifying a change

```powershell
$u = "$HOME\Unity\Editors\6000.0.83f1\Editor\Unity.exe"; $p = "$HOME\projects\flight-sim"
Start-Process $u -Wait -NoNewWindow -ArgumentList "-batchmode","-nographics","-projectPath",$p,"-executeMethod","FlightSim.Build.BuildAll.BatchBuild","-logFile","$p\Logs\build.log"
Start-Process $u -Wait -NoNewWindow -ArgumentList "-batchmode","-projectPath",$p,"-executeMethod","FlightSim.Build.SceneSnapshot.BatchSnapshot","-logFile","$p\Logs\snapshot.log"
Start-Process $u -Wait -NoNewWindow -ArgumentList "-batchmode","-projectPath",$p,"-executeMethod","FlightSim.Build.Playtest.BatchPlaytest","-logFile","$p\Logs\playtest.log"
```

Then look at `Snapshots/*.png`, and check that `Logs/playtest.log` contains `[Playtest] PASSED`.

## Git

Own repository, public at `biyonjose10/flight-sim`. Commit after each milestone and **stage
explicit paths, never `git add -A`**, because the user runs concurrent Claude sessions. `Snapshots/` is
committed; `Library/`, `Logs/`, `Playtest/`, `Build/` are ignored.
