# How it works

A plain-English walkthrough of the Flight Sim project, written to explain it to someone else. It
covers what happens when you press Play, what every script does, the Unity ideas it relies on, and
the questions you're likely to be asked.

---

## 1. The big picture

There are two kinds of script in this project.

**Game scripts** (`Assets/Scripts/`) run while you play:

| Script | Its one job |
|---|---|
| `PlayerController` | Walk with WASD, look with the mouse |
| `Interactable` | "Is the player standing here? If they press E, go to the next scene" |
| `SceneFader` | Fade to black, load the next scene, fade back in |
| `PromptHUD` | Draw the text on screen |
| `BlinkingLight` | Flash the plane's beacon and the cockpit's caution light |
| `PassengerIdle` | Make passengers turn their heads now and then |
| `FlightLayout` | Not a behaviour: a list of every size and position |

**Editor scripts** (`Assets/Scripts/Editor/`) never run in the game. They **build the scenes**:

| Script | Its one job |
|---|---|
| `BuildAll` | The Tools → Flight Sim menu; runs the two builders |
| `TerminalBuilder` | Creates scene 1: the hall, the window, the plane, the runway |
| `CabinBuilder` | Creates scene 2: the cabin, the seats, the cockpit |
| `SharedParts` | Wings, engines, runway, jet bridge, used by both builders |
| `Figures` | Builds a sitting or standing person from simple shapes |
| `SceneKit` | The sun, sky, player, HUD and fader every scene needs |
| `Prim` | Places a cube/cylinder/sphere with a colour; makes materials |
| `SceneSnapshot` | Takes 8 pictures of the scenes |
| `Playtest` | Plays the whole game by itself and reports PASSED/FAILED |

How they connect:

```mermaid
flowchart LR
    subgraph Editor["Editor time (Tools → Flight Sim → Build Scenes)"]
        L[FlightLayout<br/>all the numbers] --> TB[TerminalBuilder]
        L --> CB[CabinBuilder]
        TB --> S1[(01_Terminal.unity)]
        CB --> S2[(02_Cabin.unity)]
    end

    subgraph Play["Play time"]
        P[PlayerController] -->|position| I[Interactable]
        I -->|prompt text| H[PromptHUD]
        I -->|E pressed| F[SceneFader]
        F -->|next scene exists| S2b[load 02_Cabin]
        F -->|scene 3 missing| H
    end
```

---

## 2. What happens when you press Play

1. **Scene 1 loads.** Unity creates every object saved in `01_Terminal.unity`: the floor, the
   walls, the plane, the player, and an object called **Game Systems** that carries `PromptHUD`
   and `SceneFader`.
2. **`Awake` runs on everything.** `PromptHUD` and `SceneFader` each register themselves as
   `Instance`, so other scripts can find them without a reference.
3. **`Start` runs on everything.**
   - `PlayerController` reads which way it's facing and locks the mouse cursor. Logs `[PLAYER] Spawned at ...`.
   - `SceneFader` logs `[SCENE] Now in '01_Terminal'` and starts fading from black.
   - `Interactable` (the boarding zone) finds the player so it can watch where they are.
4. **Every frame, `Update` runs:**
   - `PlayerController` reads the mouse and turns the body (left/right) and camera (up/down).
     It reads WASD, builds a direction relative to where you're facing, and moves the
     CharacterController, which stops at walls.
   - `Interactable` checks whether the player is inside its box. When that changes, it tells
     `PromptHUD` to show or hide **Press E to board the plane**.
   - `BlinkingLight` turns the beacon on or off depending on the clock.
   - `PassengerIdle` turns each passenger's head a little.
5. **Every frame, `OnGUI` runs:** `PromptHUD` draws the crosshair, the title, and the prompt if
   one is set. `SceneFader` draws a black rectangle over the screen while it's fading.
6. **You press E in the zone.** `Interactable.Use()` calls `SceneFader.GoTo("02_Cabin")`.
   - The fader checks the scene is in Build Settings. It is.
   - It switches off player input, fades to black over 0.8 s, and calls `SceneManager.LoadScene`.
7. **Scene 2 loads**, and everything from step 2 happens again, this time in the cabin. Scene 1's
   objects are destroyed.
8. **In the cockpit you press E.** `SceneFader.GoTo("03_Takeoff")` finds scene 3 is **not** in
   Build Settings, so instead of loading it asks `PromptHUD` to show
   **Take-off is scene 3 - it hasn't been built yet.** Nothing crashes.

---

## 3. The game scripts, one by one

### `PlayerController`: walking and looking

- Uses a **CharacterController** component, Unity's ready-made "person" collider. You give it
  a movement vector with `Move()`, and it slides along walls instead of passing through them.
- **Two angles:** `yaw` (left/right) rotates the whole body; `pitch` (up/down) rotates only the
  camera, which is a child of the body. Because the body never tilts, "forward" stays flat, so
  looking up doesn't make you walk into the air.
- `Input.GetAxis("Mouse X")` gives how far the mouse moved this frame. `Input.GetAxisRaw("Horizontal")`
  gives −1, 0 or 1 from A/D.
- **Movement direction** = `transform.right × sideways + transform.forward × forwards`. If both keys
  are held that vector is longer than 1, so it's normalised; otherwise diagonal walking would be about 41% faster.
- **Gravity:** while `isGrounded`, a small downward speed keeps you pressed onto the floor.
  Otherwise the speed grows by −9.81 every second.
- Everything is multiplied by `Time.deltaTime` (the seconds since the last frame), so you walk the
  same speed at 30 fps and 144 fps.
- The CharacterController's **Min Move Distance is set to 0**. Unity's default ignores any move
  shorter than 1 mm, and at very high frame rates every frame's step is shorter than that, so
  the player would barely move. The automated playtest caught this.
- **Cursor:** Esc unlocks it; clicking locks it again.
- **Autopilot (`WalkTo`)** is used only by the automated playtest. It walks towards a point and
  turns to face it.

### `Interactable`: "press E here"

- Has a **box size** (`zoneSize`), a **prompt**, a **key** and a **scene to load**.
- Every frame: `transform.InverseTransformPoint(playerPosition)` converts the player's position
  into the zone's own coordinates, where the zone's centre is (0,0,0). Then the player is inside
  if each coordinate is within half the box size.
- It only tells the HUD when "inside" **changes**, so it isn't spamming the HUD every frame.
- `OnDrawGizmos` draws the box as a yellow wireframe in the Scene view, so you can see the zone.

### `SceneFader`: changing scenes

- `alpha` is how black the screen is (1 = black). Each scene starts at 1 and fades to 0.
- The fade is a **coroutine**: a function that pauses at `yield return null` and continues next
  frame. That lets a 0.8 s fade be written as a simple loop.
- `Application.CanStreamedLevelBeLoaded(name)` is true only for scenes listed in Build Settings,
  which is how it knows scene 3 doesn't exist yet.
- It draws the black overlay in `OnGUI` with `GUI.depth = -1000`, which puts it in front of the HUD.

### `PromptHUD`: text on screen

- Stores the current prompt (and who owns it), and the current message with an expiry time.
- `OnGUI` redraws everything every frame: the crosshair, the title for the first 4 seconds, the
  controls hint for 12 seconds, the prompt box, and the message box.
- Font size follows the window height, so it looks the same in the small editor view and full screen.
- **Only the zone that showed a prompt can hide it**, so two zones can't clear each other's prompt.

### `BlinkingLight`

`Mathf.Repeat(time, onSeconds + offSeconds)` wraps the clock into one cycle, and the light is on
during the first `onSeconds` of each cycle. There's no timer variable to manage. `offset` staggers
lights so they don't flash in unison.

### `PassengerIdle`

Head angle = `sin(t) × sin(0.37 t) × 35°`. Multiplying two sine waves of different speeds gives
mostly-still with occasional glances, which looks less robotic than a steady back-and-forth. Each
passenger gets a different `phase`.

### `FlightLayout`

Not a component: a `static class` of constants. The builders read it to place things, and the
playtest reads it for its walking route. **Change a number here and rebuild** to move anything.

---

## 4. The editor scripts, one by one

### `BuildAll`
Adds the **Tools → Flight Sim** menu using `[MenuItem]`. **Build Scenes** sets a few project
settings (linear colour space, 8 per-pixel lights, shadows), runs both builders, and writes the
scene list into Build Settings.

### `TerminalBuilder` and `CabinBuilder`
Each one creates an empty scene, calls a series of small functions (`Shell`, `WindowWall`,
`Seating`, `Gate`, …), adds the sun, the player, the zone and the HUD, and saves the scene. Each
function is a list of "put a box of this size and colour here" calls, reading positions from
`FlightLayout`.

Two ideas worth knowing:
- **Solid or not.** Walls, floors, glass and seat blocks keep their colliders. Decoration
  (screens, lights, seat cushions) has them removed, which saves the physics engine checking
  thousands of shapes you can never touch.
- **Invisible blockers.** In the cabin, one invisible box covers each side's seats. That one box is
  what keeps you in the aisle, rather than 120 individual seat colliders.

### `SharedParts`
The wings, engines, runway and jet bridge, used by both scenes so they match. A wing is a flat box
rotated by the sweep angle. Its centre is found by rotating "half a wing length sideways" by the
same angle.

### `Figures`
A person is a capsule body, box legs and arms, and a head pivot holding a sphere, hair and a nose.
Colours are picked from lists by a number, so each person looks different but a rebuild gives the
same result.

### `SceneKit`
Creates the sun (a directional light), the procedural sky, the ambient light colours, the fog,
the player (a CharacterController body with a camera child at eye height), and the Game Systems
object.

### `Prim`
Helpers that wrap `GameObject.CreatePrimitive`. It also hides a Unity quirk: the cylinder mesh is
2 units tall and 1 wide, so a cylinder of radius r and length L needs scale (2r, L/2, 2r).
Materials are **updated in place** rather than recreated, because both scenes share them.

### `SceneSnapshot` and `Playtest`
`SceneSnapshot` places a temporary camera at 8 fixed viewpoints and saves each render as a PNG.
`Playtest` enters play mode and uses the player's autopilot to walk the route. It records any error
message and fails if the player is stuck, a zone isn't reached, or a scene never loads.

---

## 5. Unity ideas used

| Idea | Where |
|---|---|
| **GameObject + components**: an object is a container; behaviour comes from components attached to it | Everywhere |
| **Transform**: position, rotation, scale, and parent/child relationships | The camera is a child of the player; screens are children of the tilted panel |
| **MonoBehaviour lifecycle**: `Awake` → `Start` → `Update` every frame → `OnGUI` | All game scripts |
| **CharacterController**: movement with collision, without physics simulation | `PlayerController` |
| **Colliders**: invisible shapes that block movement | Walls, floor, glass, seat blocks |
| **Local vs world space**: `InverseTransformPoint` | `Interactable` |
| **Coroutines**: functions that run across several frames | `SceneFader` |
| **Scene management and Build Settings** | `SceneFader`, `BuildAll` |
| **Materials and shaders**: Standard shader, emission for glowing things, Fade mode for glass | `Prim` |
| **Lights**: directional (sun), point (ceiling lights) | `SceneKit`, both builders |
| **Editor scripting**: `[MenuItem]`, `EditorSceneManager` | `BuildAll`, both builders |

---

## 6. Likely questions, with answers

**Why a CharacterController instead of a Rigidbody?**
A Rigidbody is a physics object. It can be pushed, can tip over, and needs friction, drag and
rotation locks to behave like a person. A CharacterController is made for exactly this: you tell it
where to move, it stops at walls and climbs small steps, and nothing can knock it over.

**How does the game know you're at the gate?**
The boarding zone is an invisible box. Every frame it converts the player's position into the box's
own coordinates and checks whether it's within half the box's size on every axis. If so it shows
the prompt, and pressing E loads the next scene.

**Why not use a trigger collider for that?**
Triggers work, but they depend on several things being set up correctly (a Rigidbody or
CharacterController, the right layers, the right callback) and fail silently when one is wrong. A
box check is one short function with no hidden requirements.

**How does the scene change work?**
`SceneFader` fades the screen to black with a coroutine, then calls `SceneManager.LoadScene`. The
new scene has its own fader, which starts black and fades in.

**What happens if you press take-off, since scene 3 doesn't exist?**
The fader asks `Application.CanStreamedLevelBeLoaded("03_Takeoff")`. It returns false because scene
3 isn't in Build Settings, so a message is shown instead of loading. Once scene 3 is built and added,
the same button works without changing any code.

**Why were the scenes made by code instead of dragging objects in?**
It's repeatable (a rebuild always gives the same scene), every measurement is in one file
(`FlightLayout`), mistakes are undone by rebuilding, and the whole scene can be explained by reading
code. The trade-off is that editor changes don't survive a rebuild.

**Why do you multiply by `Time.deltaTime`?**
Update runs once per frame, and frame rates vary. Multiplying a speed (metres per second) by the
seconds since the last frame gives the distance for *this* frame, so the player walks at the same
real speed on any computer.

**Why is only the camera tilted up and down, not the whole player?**
If the body tilted, "forward" would point into the floor or the sky, and looking down while pressing W
would push you into the ground. Keeping the body level means forward is always along the floor.

**Why normalise the movement vector?**
Holding W and D gives a vector of (1, 0, 1), which is about 1.41 long, so diagonal walking would be
41% faster. Normalising makes it length 1.

**How is the glass see-through?**
Its material uses the Standard shader's Fade mode with a low alpha (about 15% opaque). Glass also
doesn't cast shadows, so sunlight reaches the cabin floor through the windows.

**How do you stop the player walking into the seats in the cabin?**
One invisible box collider covers all the seats on each side. The gap between the two boxes is the
aisle, 0.62 m wide, and the player is 0.5 m wide.

**How do you know it works?**
The Playtest tool plays the game by itself: it walks to the gate, boards, walks the aisle, enters
the cockpit and presses take-off. It fails if there's any error, if the player gets stuck, or if a
scene doesn't load. The Console also logs each step with a tag (`[PLAYER]`, `[INTERACT]`, `[SCENE]`).

---

## 7. Changing things

| To… | Change… then **Tools → Flight Sim → Build Scenes** |
|---|---|
| Move the plane outside the terminal | `FlightLayout.Terminal.PlaneCentre` |
| Change where you start | `PlayerSpawn` / `PlayerSpawnYaw` in `Terminal` or `Cabin` |
| Add seat rows | `FlightLayout.Cabin.Rows` (also move `RearWallX` back 0.8 m per row) |
| Make the gate zone bigger | `FlightLayout.Terminal.BoardingZoneSize` |
| Change the prompt text | the `SceneKit.Zone(...)` call in `TerminalBuilder.Build` or `CabinBuilder.Build` |
| Walk faster | `walkSpeed` on the Player's `PlayerController` (also fine to change in the Inspector while testing) |
