# Godot Utilities

A C# utility library for Godot 4.4+, designed to speed up game development with reusable systems for audio, combat, physics, UI, saving, and more.

---

## Setup

### 1. Folder structure

The library **must** live at exactly this path inside your project:

```
res://
└── addons/
    └── utilities/
        ├── plugin.cfg
        ├── plugin.gd
        └── src/
            └── ...
```

> If the folder is named or placed differently, the plugin's autoload paths will break and singletons won't register.

### 2. Enable the plugin

1. Copy the `utilities` folder into your project's `addons/` directory.
2. Open **Project → Project Settings → Plugins**.
3. Find **Godot Utilities** and set it to **Enabled**.

Enabling the plugin automatically:
- Registers the following autoload singletons: `AudioManager`, `Physics2D`, `InventoryManager`, `ParticlesManager`, `SaveManager`.
- Adds the three config path keys to Project Settings with default values.

### 3. Create your config resources

Three resources need to exist before the relevant systems work:

| Resource | Default path | Used by |
|---|---|---|
| `AudioConfig` | `res://audio_config.tres` | AudioManager |
| `ParticlesRegistry` | `res://particles_registry.tres` | ParticlesManager |
| `InventoryConfig` | `res://inventory_config.tres` | InventoryManager |

Create each resource in the Godot editor (**right-click in FileSystem → New Resource**) and point the matching Project Settings key at its path if you move it.

### 4. Generate IDs

After filling in your `AudioConfig` and `ParticlesRegistry`, go to **Project → Tools → GodotUtilities: Generate IDs**. This writes the auto-generated enum and `StringName` files for audio and particles. Re-run it any time you add or rename entries.

> The generated files are `AudioManager.Ids.cs` and `ParticlesManager.Ids.cs`. They are safe to commit — do not edit them manually.

---

## Modules

### Cooldown
A lightweight struct for managing cooldown timers with zero heap allocation.
```csharp
private Cooldown attackCooldown = new(0.5f);

attackCooldown.Tick(delta);
if (attackCooldown.IsReady) Attack();
```
> **Note:** Do not mark as `readonly` — it must be mutable to tick.

---

### MathUtil
Static math helpers covering randomness, interpolation, and spatial utilities.
```csharp
MathUtil.DeltaLerp(from, to, dt, weight);   // frame-rate independent lerp
MathUtil.RandomInCircle(radius);             // uniform distribution
MathUtil.OffscreenSpawn(camPos, margin);     // spawns outside the viewport
MathUtil.Chance(0.25f);                      // 25% probability check
```

---

### WeightedTable\<T\>
Generic weighted random picker
```csharp
var table = new WeightedTable<string>()
    .Add("common", 70f)
    .Add("rare",   25f)
    .Add("epic",    5f);

string result = table.Pick();
```

---

### AudioManager
Singleton audio system with pooled SFX players, crossfading music, and ambience channels.
```csharp
// for godot serialization cases (export ids & play them)
AudioManager.Instance.PlaySfxWithId(AudioManager.SfxId.RockHit, pitchVariance: 0.1f);
AudioManager.Instance.PlayMusicWithId(AudioManager.MusicId.GentleBreeze, fadeDuration: 2f);

// for internal code cases
AudioManager.Instance.PlaySfx(AudioManager.SfxName.RockHit, pitchVariance: 0.1f);
AudioManager.Instance.PlayMusic(AudioManager.MusicName.GentleBreeze, fadeDuration: 2f);

AudioManager.Instance.SetMusicVolume(0.8f);
```
Configure via `AudioConfig` resource. SFX IDs are **auto-generated** — do not edit the generated file manually.

---

### Combat
Component-based combat system — attach to any node.

| Component | Role |
|---|---|
| `HealthComponent` | HP, invincibility, resistances, immunity |
| `HitboxComponent` | Deals damage on area overlap |
| `HurtboxComponent` | Receives damage, routes to HealthComponent |
| `KnockbackComponent` | Applies and decays velocity knockback |

```csharp
// HealthComponent signals
healthComponent.Damaged  += (source, amount) => { ... };
healthComponent.Died     += (source) => { ... };
healthComponent.Revived  += () => { ... };
```

---

### EventBus
Type-safe, decoupled event system. All calls must be made from the Godot main thread.
```csharp
// event setup
public record struct PlayerDied();
public record struct GameModeChanged(GameMode Mode);

// Subscribe
EventBus.AddListener<PlayerDied>(OnPlayerDied, owner: this);

// Fire
EventBus.Trigger<PlayerDied>(); // empty event
EventBus.Trigger(new GameModeChanged(GameMode.Hard)); // event holding values

// Auto-wire via attribute
[EventHandler]
private void OnGameModeChanged(GameModeChanged @event) { ... }

[EventHandler(typeof(PlayerDied))]
private void OnPlayerDied() { ... }

// In _Ready():
this.WireEvents();
```

---

### StateMachine\<T\>
Fluent, enum-driven finite state machine with guards, priorities, and event-driven transitions.
```csharp
var fsm = new StateMachine<State>();

fsm.AddState(State.Idle)
   .OnEnter(() => PlayAnim("idle"));

fsm.AddState(State.Run)
   .OnUpdate(dt => Move(dt));

fsm.AddTransition(State.Idle, State.Run)
   .When(() => input.IsMoving())
   .SetPriority(1);

fsm.SetInitialState(State.Idle);
fsm.Start();

// In _PhysicsProcess:
fsm.Update(delta);
```

---

### VelocityComponent
Physics movement component for `CharacterBody2D` with jump-feel features built in.

- Coyote time & jump buffering
- Apex hanging (reduced gravity + air boost at jump peak)
- Gravity flip (floor ↔ ceiling)
- Explosion knockback with falloff curves
- Mass-based force and impulse

```csharp
velocityComponent.Move(direction, dt);
velocityComponent.TryJumpBuffered();
velocityComponent.CutJump();
velocityComponent.ApplyExplosion(origin, force: 1200f, radius: 200f);
```

---

### Physics2D
Static wrapper around Godot's physics queries with pooled query objects.
```csharp
if (Physics2D.Raycast(origin, direction, distance, out RaycastHit hit))
    GD.Print(hit.Position);

GodotObject[] hits = Physics2D.OverlapSphere(position, radius: 64f);
bool occupied = Physics2D.CheckSphere(position, radius: 32f);
```

---

### Pooling
Two generic object pools for managed and node objects.

```csharp
// Object pool
var pool = new ObjectPool<Bullet>(() => new Bullet(), initialSize: 10);
var bullet = pool.Get();
pool.Release(bullet);

// Node pool (scene-based)
var pool = new NodePool<PooledParticle>(scene, parent, initialSize: 5);
```
Nodes implementing `IPoolable` receive `OnGet()` / `OnRelease()` callbacks automatically.

---

### ParticlesManager
Pooled GPU particle spawner with auto-generated IDs.
```csharp
// godot serialization cases (export ids & use them) (Enum)
ParticlesManager.Instance.SpawnById(ParticlesManager.ParticleId.DirtDust, position);

// internal code cases (StringName)
ParticlesManager.Instance.Spawn(ParticlesManager.ParticleName.DirtDust, position);
```
Register particle scenes in a `ParticlesRegistry` resource and set its path in Project Settings.

---

### Save System
Reflection-based save/load with multi-slot support and dynamic scene respawning.

#### Making a node saveable

Implement `ISaveable` on any node and mark the fields or properties you want persisted with `[Save]`. The system serializes and restores them automatically — no manual read/write code needed.

```csharp
public partial class Player : CharacterBody2D, ISaveable
{
    public string SaveKey => "player"; // must be unique across all saveables

    [Save] private int      score;
    [Save] private Vector2  lastPosition;
    [Save] private GameMode currentMode;   // enums are supported
}
```

Then add the node to the `Saveable` group so `SaveManager` can find it — either in the editor (Node → Groups) or in code:

```csharp
public override void _Ready()
{
    AddToGroup(SaveManager.SaveGroup);
}
```

#### Custom serialize / deserialize logic

If you need to handle fields the reflection system can't cover (e.g. nested resources, computed state), override `OnSerialize` and `OnDeserialize`. They run after the automatic reflection pass.

```csharp
public void OnSerialize(Dictionary data)
{
    data["inventory"] = inventory.Serialize();
}

public void OnDeserialize(Dictionary data)
{
    if (data.TryGetValue("inventory", out var inv))
        inventory.Deserialize(inv.AsGodotArray(), GetItemById);
}
```

#### Saving and loading

```csharp
SaveManager.Instance.Save();         // save to slot 1
SaveManager.Instance.Load();         // load from slot 1

SaveManager.Instance.Save(SaveManager.Slot.Slot2);
SaveManager.Instance.Load(SaveManager.Slot.Slot2);

SaveManager.Instance.SaveExists();   // check before loading
SaveManager.Instance.DeleteSave();
```

#### Dynamically spawned nodes

If a node is created at runtime (not placed in the scene), set `ScenePath` and `ParentPath` on your `ISaveable` so the system can recreate it on load:

```csharp
public string ScenePath  => "res://scenes/enemy.tscn";
public string ParentPath => "/root/Game/Enemies";
```

On load, if the node isn't found in the scene tree, it will be instantiated from `ScenePath` and added under `ParentPath` automatically.

#### Script-less nodes

For built-in nodes with no script, attach a `PropertySaveAdapter` node as a child and select which properties to save in the inspector — no code required.

---

### Inventory System
Full grid inventory with stacking, merging, drag-and-drop, and serialization.

- `InventoryData` — data layer, signals on every change
- `InventoryUi` — UI layer, attaches to any `InventoryData`
- `ItemData` / `SlotData` — resource-based item definitions
- `ExternalInventoryUi` — base class for chests, shops, etc.

Configure via `InventoryConfig` resource and set its path in Project Settings.

---

### TweenExtensions
Extension methods on `Tween` covering all common animation needs.
```csharp
tween.TweenFadeIn(target, duration: 0.3f);
tween.TweenShakePosition(target, duration: 0.5f, strength: 8f);
tween.TweenPunchScale(target, duration: 0.4f, amount: Vector2.One * 0.2f);
tween.TweenBlink(target, blinks: 3);
tween.TweenTypewriter(label, duration: 2f);
tween.TweenSquish(target, duration: 0.3f);
tween.TweenWiggle(target, degrees: 15f, duration: 0.4f);
tween.Sine().EaseOut();  // fluent transition/ease chaining
```

---

### Other Utilities

**NodeInjector** — inject node references by attribute instead of `GetNode`.
```csharp
[NodeRef] private Sprite2D sprite; // uses field name as a path
[NodeRef("UI/HealthBar")] private ProgressBar healthBar; // custom path

public override void _Notification(int what)
{
    if (what == NotificationSceneInstantiated)
        this.WireNodes();
}
```

**InputBuffer** — buffers and expires input actions by name.
```csharp
buffer.BufferAction("jump", duration: 0.15f);
buffer.Tick(delta); // in process/physics process
if (buffer.TryConsume("jump")) Jump();
```

**Extension helpers** — small quality-of-life extensions on `Node`, `Node2D`, `Vector2`, `CharacterBody2D`, `AnimatedSprite2D`, `SceneTree`, `GpuParticles2D`, and `bool`.

---

## Project Settings

The following paths must be set in **Project → Project Settings**:

| Key | Points to |
|---|---|
| `godot_utilities/audio_config_path` | `AudioConfig` resource |
| `godot_utilities/inventory_config_path` | `InventoryConfig` resource |
| `godot_utilities/particles_registry_path` | `ParticlesRegistry` resource |
