# Flight Sim

A first-person airport-to-cockpit walk. You wait at Gate 7, look out of the window at your plane
and the runway, board, walk down the cabin to the cockpit and press take-off.

The finished game will have four scenes. **Scenes 1 and 2 are built.** Scenes 3 (take-off and
cruise) and 4 (landing) come next. Until then, the take-off button tells you scene 3 isn't built
yet instead of crashing.

**You only ever open scene 1 and press Play.** Scene 2 loads by itself when you board.

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

| Key | Does |
|---|---|
| **W A S D** (or arrow keys) | Walk |
| **Mouse** | Look around |
| **E** | Use: board at the gate, take off in the cockpit |
| **Esc** | Free the mouse cursor (click the Game view to capture it again) |

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
7. Walk through the cockpit door and stand between the two pilot seats.
   **Press E to take off** appears.
8. Press **E**. Because scene 3 isn't built yet, the message
   **Take-off is scene 3 - it hasn't been built yet.** appears and you stay in the cockpit.

The Console logs every step, which is the quickest way to confirm it is working:

| Tag | Logged when |
|---|---|
| `[PLAYER]` | You spawn (with position and facing) |
| `[SCENE]` | A scene starts, a scene is loading, or a scene isn't built yet |
| `[INTERACT]` | You enter or leave a zone, or press E in one |
| `[HUD]` | An on-screen message is shown |

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
| **Build Scenes** | Rebuilds both scenes from the scripts, and lists them in Build Settings |
| **Open Scene 1 (Terminal)** | The one you press Play on |
| **Open Scene 2 (Cabin)** | The plane on its own, for editing |
| **Add Scenes To Build Settings** | Registers both scenes so one can load the other |
| **Snapshot Scenes** | Saves 8 pictures of both scenes to `Snapshots/` |
| **Playtest** | Plays the game by itself, start to finish, and reports PASSED or FAILED |
| **Build Windows Player** | Writes `Build/FlightSim/FlightSim.exe` |

---

## How the scenes are made

Both `.unity` files are **generated by C# scripts**, not placed by hand.
`Assets/Scripts/Editor/TerminalBuilder.cs` and `CabinBuilder.cs` create every floor, wall, seat,
light and person from code, and **Tools → Flight Sim → Build Scenes** runs them.

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

- **Scenes 3 and 4 are not built.** The take-off button deliberately shows a message instead.
- **There is no sound.** The user chose a silent build for now.
- **Everything is made from basic shapes** (cubes, cylinders, spheres, capsules) in one low-poly
  style. There are no imported models, so nothing needs downloading, but nothing is photo-realistic either.
- **The passengers don't walk.** They sit or stand, and turn their heads now and then.
- **The plane in scene 2 is lined up at the start of the runway** (so the windscreen looks down
  it) while also being connected to the jet bridge. A real plane is towed away from the gate first. It's a
  deliberate shortcut, so the cockpit view shows the runway you're about to take off from.
- Seated passengers have no colliders. You can't reach them anyway, because the seat blocks keep you in the aisle.
- Verified in Unity 6000.0.83f1 on Windows only.
