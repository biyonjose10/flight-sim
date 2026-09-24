# Flight Sim

A first-person flight, from the departure gate to the runway at the other end. You wait at Gate 7,
look out of the window at your plane, board, walk down the cabin to the cockpit, take off, fly the
aeroplane yourself, and then watch it land.

All four scenes are built:

| | |
|---|---|
| **1 Terminal** | Walk the departure hall, look at your plane through the glass, press E at the gate |
| **2 Cabin** | Walk the aisle past the passengers, press E between the pilot seats |
| **3 Take-off** | **You fly it.** Open the throttle, roll down the runway, pull back, and climb away |
| **4 Landing** | The approach and touchdown, which flies itself while you watch from either camera |

**You only ever open scene 1 and press Play.** Each scene loads the next one by itself.

---

## Just want to play it? One file, no Unity

Open the project once, run **Tools → Flight Sim → Build Windows Player**, then double-click
**`Build/FlightSim/FlightSim.exe`**. It boots straight into scene 1.

The `Build/` folder is deliberately **not committed**. A Unity player is a few hundred MB, and git
would carry that weight in every clone forever.

---

## Working on it in Unity

### What you need

| | |
|---|---|
| **Unity** | **6000.0.83f1**, the version this project was built and tested in |
| Render pipeline | Built-in (nothing to install) |
| Git | to clone the repo |
| Disk | about 1 GB once Unity has built its Library |

Unity Hub → **Installs** → **Install Editor** → **Archive** tab → find **6000.0.83f1**. The free
Personal licence is fine: sign in to Unity Hub once and it activates.

### Getting it

```bash
git clone https://github.com/biyonjose10/flight-sim.git
```

That's everything. Scenes, scripts and materials are all committed. There are **no Asset Store
packs** to download: every object is built from Unity's own shapes.

### Opening it

1. **Unity Hub** → **Projects** → **Add** → **Add project from disk** → pick the `flight-sim` folder.
2. Check the Editor Version column says **6000.0.83f1**, then click the project.
3. **The first open takes a few minutes** while Unity builds its Library. That only happens once.
4. Menu bar → **Tools → Flight Sim → Open Scene 1 (Terminal)**.

---

## Running it

**Press Play** with scene 1 open. Click in the Game view once so it captures the mouse.

### Controls

**On foot** (scenes 1 and 2):

| Key | Does |
|---|---|
| **W A S D** (or arrow keys) | Walk |
| **Mouse** | Look around |
| **E** | Use: board at the gate, take off in the cockpit |
| **Esc** | Free the mouse cursor (click the Game view to capture it again) |

**Flying** (scenes 3 and 4):

| Key | Does |
|---|---|
| **W / S** | Nose down / nose up |
| **A / D** | Bank left / right (on the ground, steer the nosewheel) |
| **Q / E** | Throttle down / up |
| **Space** | Wheels up / down (they also come up by themselves once you are climbing) |
| **V** | Swap between the pilot's seat and the camera outside |
| **Mouse** | In the seat, look around. Outside, swing the camera around the plane |
| **Scroll wheel** | Outside only: move the camera closer or further away |
| **L** | Begin the landing (once the wheels are up and you are above 300 m) |
| **R** | On the arrival card: fly the whole thing again |
| **Esc** | Free the mouse cursor |

### What should happen

1. The screen fades in on the departure hall. **Gate 7 - Departures** appears, with a controls
   hint in the top left.
2. You're standing in the middle of the hall facing a long glass wall. Through it: your plane
   parked side-on with a red beacon flashing, the jet bridge on the right, and the runway beyond.
3. Walk around. The benches, walls, pillars, glass and people are solid. The departures board is
   on the wall behind you where you started.
4. Walk to the **GATE 7** door (right-hand end of the window, by the queue posts).
   **Press E to board the plane** appears.
5. Press **E**. The screen fades to black and **scene 2** loads.
6. You're just inside the plane's door, looking at the galley and the open **FLIGHT DECK** door.
   Turn round to see the cabin: 20 rows of 3+3 seats, passengers, and the wings through the windows.
7. Walk through the cockpit door and stand between the two pilot seats. **The captain is sitting
   in the left-hand one**, in uniform and a headset, and turns to look at you as you come in. The
   right-hand seat is empty, because that one is yours.
   **Press E to take off** appears.
8. Press **E**. The screen fades and **scene 3** loads: you are in the same cockpit, but now lined
   up on the runway with the engines idling and the world stretching away in front of you.
9. Hold **E** to open the throttle. The readouts along the bottom show your speed climbing. The
   plane rolls, and the engines get louder.
10. At about **145 kt** the nose will come up. Hold **S** to pull back, and you fly. The wheels
    fold away by themselves once you are climbing.
11. Steer with **A** and **D**. Let go and the plane rights itself. Press **V** at any point to
    watch from outside, and swing the mouse to look at the aeroplane from any angle.
12. Above **300 m** with the wheels up, **Press L to begin landing** appears at the bottom. Press
    **L** whenever you have had enough of flying around.
13. **Scene 4** loads on final approach. You steer nothing here: watch the runway come up, the
    flare, the touchdown and the roll-out, from either camera.
14. The plane stops and the **arrival card** appears with your flight time. Press **R** to do the
    whole thing again from the terminal.

The Console logs every step, which is the quickest way to confirm it is working:

| Tag | Logged when |
|---|---|
| `[PLAYER]` | You spawn (with position and facing) |
| `[SCENE]` | A scene starts, a scene is loading, or the journey clock restarts |
| `[INTERACT]` | You enter or leave a zone, or press E in one |
| `[HUD]` | An on-screen message is shown |
| `[FLIGHT]` | The plane is ready, becomes airborne, moves its gear, touches down or crashes |
| `[CAMERA]` | You swap between the pilot's seat and the outside view |
| `[AUDIO]` | A loop starts or a one-shot plays |
| `[PANEL]` | The cockpit instruments come alive |
| `[LANDING]` | Each stage of the scripted approach |
| `[WORLD]` | The shared airfield and scenery are built |

---

## If something looks wrong

**Unity opens on an empty "Untitled" scene.**
Use **Tools → Flight Sim → Open Scene 1 (Terminal)**.

**You start inside the plane.**
You have scene 2 open on its own. That's fine for editing it, but open scene 1 to play from the start.

**The mouse doesn't turn the view.**
Click inside the Game view. After you press Esc, the game waits for a click before it takes the
mouse back.

**Pressing E at the gate does nothing, or the Console says `'02_Cabin' is not in the build yet`.**
Run **Tools → Flight Sim → Add Scenes To Build Settings**. One scene can only load another if both
are listed there.

**You moved something in the editor and it moved back.**
The scenes are generated (see below). Change the number in `FlightLayout.cs` and rebuild instead.

**Everything is pink.**
A shader failed to load, usually because the project was opened in a different Unity version. Use
6000.0.83f1 and run **Build Scenes** again.

---

## Editor menu

Everything lives under **Tools → Flight Sim**:

| Item | Does |
|---|---|
| **Build Scenes** | Regenerates the sounds, rebuilds all four scenes, and lists them in Build Settings |
| **Open Scene 1 (Terminal)** | The one you press Play on |
| **Open Scene 2 (Cabin)** | The plane on its own, for editing |
| **Open Scene 3 (Take-off)** | Start already sitting on the runway |
| **Open Scene 4 (Landing)** | Start already on final approach |
| **Generate Audio** | Writes the 13 `.wav` files into `Assets/Audio/` |
| **Add Scenes To Build Settings** | Registers all four scenes so each can load the next |
| **Snapshot Scenes** | Saves 14 pictures of the four scenes to `Snapshots/` |
| **Playtest** | Plays the whole game by itself, start to finish, and reports PASSED or FAILED |
| **Build Windows Player** | Writes `Build/FlightSim/FlightSim.exe` |

---

## How the scenes are made

All four `.unity` files are **generated by C# scripts**, not placed by hand.
`TerminalBuilder.cs`, `CabinBuilder.cs`, `FlightBuilder.cs` and `LandingBuilder.cs` in
`Assets/Scripts/Editor/` create every floor, wall, seat, light, person, cloud and field from code,
and **Tools → Flight Sim → Build Scenes** runs them. The sounds are generated the same way, by
`AudioBank.cs`.

Three things are shared rather than copied, which is why the game hangs together:

| File | Used by |
|---|---|
| `SharedParts.Airliner` | The one aeroplane - parked in scene 1, flown in 3 and 4 |
| `CockpitParts.Cockpit` | The one cockpit - walked into in scene 2, flown from in 3 and 4 |
| `WorldParts` | The one airfield - you land back where you took off |

Every size and position lives in **`Assets/Scripts/FlightLayout.cs`**. To move the plane, add seat
rows or widen the aisle, **change the number there and run Build Scenes**. Dragging objects in the
editor works until the next rebuild, which puts them back.

- **`HOW_IT_WORKS.md`**: a plain-English walkthrough of every script, written for explaining
  the project, with likely questions and answers.
- **`SCENES_README.md`**: the layout numbers and why each design decision was made.

### Checking a change

1. **Build Scenes**
2. **Snapshot Scenes**, then look at the pictures in `Snapshots/`
3. **Playtest**: the Console must end with `[Playtest] PASSED`

---

## Going back

```bash
git tag -n1                     # list the restore points
git reset --hard working-v1     # back to a known good state
```

- **`working-v1`**: scenes 1 and 2 complete, playtest passing.

---

## Honest status

- **The flight model is not a simulation.** It keeps four numbers - speed, pitch, roll and heading
  - and moves the plane from them. There is no lift equation and no angle of attack, so the plane
  **cannot stall and cannot flip over**, and it levels itself out when you let go of the keys.
  Turns do use the real formula for a banked turn. This was a deliberate choice: a plane that can
  drop out of the sky is miserable to demonstrate.
- **Scene 4 is not flown, it is played.** The approach and touchdown are a fixed timeline, so they
  look right every time. You can look around and swap cameras, but nothing you press changes it.
- **Every sound is synthesised from sine waves and noise**, not recorded. The engines, wind and
  tyres are convincing; the footsteps and chimes are the weakest of them.
- **Everything is made from basic shapes** (cubes, cylinders, spheres, capsules) in one low-poly
  style. There are no imported models, so nothing needs downloading, but nothing is photo-realistic either.
- **The countryside is scenery only.** Fields, roads, towers and clouds have no colliders - you can
  fly straight through a cloud or a tower block. Only the ground ends a flight.
- **The captain's face is a photograph on a flat card.** Every head in the game is a sphere, and a
  photo wrapped around a sphere smears at the edges, so his face sits on a flat rectangle on the
  front of his head. It reads properly from your seat, which is the angle you actually see him
  from, and is obviously flat if you get side-on to it. His head is turned towards your seat on
  purpose - looking dead ahead he would show you nothing but the back of his skull.
- **The passengers don't walk.** They sit or stand, and turn their heads now and then.
- **The plane in scene 2 is lined up at the start of the runway** (so the windscreen looks down
  it) while also being connected to the jet bridge. A real plane is towed away from the gate first. It's a
  deliberate shortcut, so the cockpit view shows the runway you're about to take off from.
- Seated passengers have no colliders. You can't reach them anyway, because the seat blocks keep you in the aisle.
- Verified in Unity 6000.0.83f1 on Windows only.
