# Baraf-Paani — Single-Player

A 3D single-player freeze-tag game built in Unity and C#. *Baraf-Paani* is the South Asian playground version of freeze tag: a catcher freezes runners on
touch, and runners can free their frozen teammates by reaching them first.


Built as my final-year BSc Software Engineering project.

A networked version of the same project lives in
[Freeze_Tag_Multiplayer](https://github.com/Aoisama19/Freeze_Tag_Multiplayer).

---

## Gameplay

You pick a character and a role before the round starts. Every match runs one
catcher against four runners; whichever role you don't take is filled by AI.

- **Playing as a runner** — stay out of the catcher's reach, and unfreeze frozen
  teammates by getting close and pressing the action key. You win if at least one
  runner is still unfrozen when the round ends.
- **Playing as the catcher** — freeze every runner. You win when the unfrozen count
  reaches zero.

Frozen players are moved to a `Freeze` layer and have their movement, animator,
input and vision components disabled, so a frozen runner is genuinely inert until
another runner reaches them.

Four playable maps ship in the build: 3 Talwaar, Badshahi Masjid, Faisal Mosque and
Clock Tower. Wandering NPCs populate each map as obstacles.

### Power-ups

Three collectable power-ups, up to three held at a time
(`Assets/_C#/Powerups/`). All three derive from a shared `PowerupsBase` class and
work identically for a human player or an AI agent:

| Power-up | Effect |
|---|---|
| Speed Boost | Doubles movement speed, with a mesh-trail and screen VFX |
| Clone | Spawns a decoy copy of the character |
| Invisibility | Dissolve-shader fade, moves to an `Invisible` layer, drops off the minimap |

Pickups respawn ten seconds after being collected.

---

## The AI

This is the part of the project worth reading. Agents are built on Unity's NavMesh
(`com.unity.ai.navigation`) with a hand-written sensing and decision layer on top.

**Vision — `AI/FieldOfView.cs`.** Each agent runs a cone-of-vision check rather than
an omniscient distance test: `Physics.OverlapSphere` for candidates, an angle test
against the agent's forward vector, then a raycast against an obstruction mask so
walls actually block sight. The catcher and runners get different profiles — the
catcher sees 6m at 100°, runners see 50m at 100°, so runners spot danger long before
the catcher spots them. Runners run a second pass against the `Freeze` layer to find
frozen teammates.

**Catcher — `AI/CatcherAI.cs`.** Retargets to the nearest unfrozen runner on a
1-second interval, overriding that immediately if a runner enters its vision cone.
When it has no target it samples a random reachable NavMesh point and patrols.
Power-up use is distance-gated rather than random: Speed Boost inside 6m,
Invisibility inside 8m, Clone beyond 10m, behind a 5-second cooldown. It also
subscribes to an invisibility event so it drops a target that vanishes instead of
chasing a ghost.

**Runners — `AI/RunnerAI.cs`.** Scan for frozen teammates and path to them to
perform a rescue, and wander when there are none. They carry their own stuck
detection: if an agent hasn't moved 0.1m in 3 seconds it is re-tasked to a fresh
NavMesh destination, which clears agents wedged on geometry or against each other.

**NPCs — `AI/NPC.cs`.** Ambient crowd agents with an active-ragdoll state machine
(`Walking → Ragdoll → ResettingBones → StandingUp`). On contact they go limp, then
stand back up: the script samples the first frame of the stand-up animation clip for
its bone transforms, interpolates every bone from its ragdoll pose to that pose, then
hands control back to the animator — so the transition into the animation is seamless
rather than a snap. It picks a face-up or face-down stand-up variant based on hip
orientation, and re-aligns the root transform to the hips with a downward raycast to
keep the character on the ground.

**Cost control.** Sensing and steering are deliberately kept off the per-frame path:
field-of-view checks run on a 0.2s coroutine, retargeting on a 1s interval, and the
catcher's chase loop on a configurable `updateSpeed` tick (0.1s) — rather than every
agent doing overlap tests, distance sorting and `SetDestination` calls in `Update()`.

---

## Controls

Built on Unity's Input System, with keyboard/mouse and gamepad bindings
(`Assets/StarterAssets/InputSystem/StarterAssets.inputactions`).

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Look | Mouse | Right stick |
| Sprint | Left Shift | Left trigger |
| Jump | Space | A / South |
| Freeze / Unfreeze | F | B / East |
| Power-up 1 | Q | Y / North |
| Power-up 2 | E | A / South |
| Power-up 3 | R | X / West |
| Pause | Esc | Select |

---

## Stack

- **Unity 2021.3.18f1** (URP 12.1.10)
- **C#**
- Unity AI Navigation (NavMesh), Input System, Cinemachine, Visual Effect Graph,
  TextMeshPro
- Ready Player Me avatar SDK + glTFast for character loading

---

## Running it

```bash
git clone https://github.com/Aoisama19/Freeze_Tag_Single.git
```

1. Open the project in **Unity 2021.3.18f1**. First import takes a while — the
   Ready Player Me and glTFast packages are pulled from Git URLs, so you need a
   network connection on first open.
2. Open `Assets/Scenes/Intro.unity` and press Play. Intro leads into the main menu,
   where you choose a character, a role and a map.
3. To skip straight to gameplay, open any of `Assets/Scenes/3Talwaar.unity`,
   `Badshahi Masjid.unity`, `Faisal Mosque.unity` or `ClockTower.unity`.

