# CLAUDE.md

Guidance for Claude Code working in this repository.

Read `README.md` first (running it), then `SCENES_README.md` (layout and design decisions) and
`HOW_IT_WORKS.md` (the teacher-facing walkthrough; keep it accurate when code changes).

## What this is

A four-scene first-person flight sim for a **college assignment the user must explain to a
teacher**. Scene 1 is the terminal (walk, look at the plane, press E at the gate), scene 2 is the
cabin (walk the aisle, press E in the cockpit). **Scenes 3 (take-off/cruise) and 4 (landing) are not
built yet.** The user wants to approve 1 and 2 before they're started.

Unity **6000.0.83f1**, **Built-in RP**, old Input Manager (`activeInputHandler: 0`), no packages
beyond Unity's built-in modules. The editor lives at `~/Unity/Editors/6000.0.83f1/`.

Explainability is a hard requirement: every script has a plain-English summary, inspector fields
carry tooltips, every runtime step logs a tag (`[PLAYER] [SCENE] [INTERACT] [HUD]`), and every
number sits in `FlightLayout.cs`. Keep new code to that standard.

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
- **The scene-3 button is intentional.** `SceneFader` shows a message when the target scene isn't
  in Build Settings. When scene 3 exists, add it to `BuildAll.Scenes`, and the Playtest's final phase
  (which expects the "scene 3" message) must change with it.

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
