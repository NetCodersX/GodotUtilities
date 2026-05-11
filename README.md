# Godot Utilities

A modular C# utility library for Godot 4, covering audio, particles, UI, combat, inventory, save/load, FSM, events, pooling, physics, and more — all wired up as autoload singletons via an editor plugin.

---

## Table of Contents

- [Installation](#installation)
- [Project Settings](#project-settings)
- [Modules](#modules)
  - [Audio Manager](#audio-manager)
  - [Particles Manager](#particles-manager)
  - [UI Manager](#ui-manager)
  - [Combat System](#combat-system)
  - [Event Bus](#event-bus)
  - [Finite State Machine (FSM)](#finite-state-machine-fsm)
  - [Inventory System](#inventory-system)
  - [Save System](#save-system)
  - [Physics2D](#physics2d)
  - [Input Buffer](#input-buffer)
  - [Object Pooling](#object-pooling)
  - [Tween Utilities](#tween-utilities)
  - [Extensions](#extensions)
  - [Utilities](#utilities)

---

## Installation

1. Copy the `addons/utilities` folder into your Godot project.
2. Enable the plugin: **Project → Project Settings → Plugins → Utilities → Enable**.
3. The plugin will automatically:
   - Register all required **Project Settings** keys.
   - Add a **"GodotUtilities: Add Autoloads"** menu item under the **Tools** menu.
4. Click **Tools → GodotUtilities: Add Autoloads** to register all singletons.
5. Set your asset folder paths in **Project Settings → godot_utilities/\*** (see [Project Settings](#project-settings)).
6. Click **Tools → GodotUtilities: Generate IDs** (or re-enable the plugin) to generate the typed ID files for audio, particles, and UI panels.

> **Note:** Generated files (`AudioManager.gen.cs`, `ParticlesManager.gen.cs`, `UIManager.gen.cs`) are created automatically and should **not** be edited manually.

---

## Project Settings

After enabling the plugin these settings will appear under `godot_utilities/`:

| Key | Default | Description |
|-----|---------|-------------|
| `inventory_config_path` | `res://resources/inventory_config.tres` | Path to the `InventoryConfig` resource |
| `sfx_folder` | `res://assets/audio/sfx` | Folder scanned for SFX files |
| `music_folder` | `res://assets/audio/music` | Folder scanned for music files |
| `ambience_folder` | `res://assets/audio/ambience` | Folder scanned for ambience files |
| `particles_folder` | `res://scenes/particles` | Folder scanned for particle scenes |
| `ui_panels_folder` | `res://scenes/ui/panels` | Folder scanned for UI panel scenes |
| `audio_sfx_pool_size` | `4` | Initial Size of Sfx Pool |
| `audio_sfx_pool_trim_cooldown` | `5.0` | A Cooldown to remove unused sfx nodes |

---

## Modules

---

### Audio Manager

**Autoload:** `AudioManager`

Handles SFX (pooled, 2D or global), music, and ambience with crossfading and lazy-loading. Audio buses expected: `Master`, `SFX`, `Music`, `Ambience`.

#### Playing SFX

```csharp
// Play a global sound effect
SfxHandle handle = AudioManager.Instance.PlaySfx(AudioManager.SfxName.Digging);

// Play a positional 2D sound effect
Sfx2DHandle handle2D = AudioManager.Instance.PlaySfx2D(
    AudioManager.SfxName.RockHit,
    GlobalPosition,
    pitchVariance: 0.1f
);

// Optional handle control while the sound is playing
handle.SetPitch(1.2f);
handle.SetVolume(0.5f);
handle2D.SetPosition(newPos);
```

#### Music & Ambience

```csharp
// Crossfade to a new music track (default 1.5s fade)
AudioManager.Instance.PlayMusic(AudioManager.MusicName.GentleBreeze);
AudioManager.Instance.PlayMusic(AudioManager.MusicName.GoldenGleam, fadeDuration: 2f);

// Queue the next track (plays immediately if nothing is playing)
AudioManager.Instance.ThenPlayMusic(AudioManager.MusicName.GoldenGleam);

AudioManager.Instance.StopMusic();
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();

// Ambience (same API, default 3s fade)
AudioManager.Instance.PlayAmbience(AudioManager.AmbienceName.Forest);
AudioManager.Instance.StopAmbience(fadeDuration: 2f);
```

#### Volume Control

```csharp
// All values are linear (0.0 – 1.0)
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetMusicVolume(0.6f);
AudioManager.Instance.SetSfxVolume(1f);
AudioManager.Instance.SetAmbienceVolume(0.4f);
```

#### Cache Management

```csharp
// Pre-load sounds to avoid first-play hitches
AudioManager.Instance.WarmUpSfx(AudioManager.SfxName.Digging, AudioManager.SfxName.RockHit);

// Unload specific or all sounds from memory
AudioManager.Instance.UnloadSfx(AudioManager.SfxName.WaterCan);
AudioManager.Instance.UnloadAllSfx();
AudioManager.Instance.UnloadAll();
```

#### Adding New Audio Files

Drop a file into the configured `sfx_folder`, `music_folder`, or `ambience_folder`, then run **Tools → GodotUtilities: Generate IDs**. The new file will appear as a typed constant — e.g., `AudioManager.SfxName.MyNewSound`.

---

### Particles Manager

**Autoload:** `ParticlesManager`

Manages a pool of `GpuParticles2D` scenes for one-shot particle effects.

```csharp
// Spawn a particle effect at a world position
ParticlesManager.Instance.Play(ParticlesManager.ParticleName.DirtDust, GlobalPosition);
ParticlesManager.Instance.Play(ParticlesManager.ParticleName.BushHit, hitPoint);
```

Available particle names are auto-generated from the `particles_folder`. Run **Generate IDs** after adding new `.tscn` files.

**Built-in names:** `BushHit`, `DirtDust`, `GroundHit`, `LeavesFall`, `RockHit`, `TrunkHit`

---

### UI Manager

**Autoload:** `UIManager`

Manages a registry of UI panels, allowing you to open/close them by name from anywhere in code.

```csharp
// Open a panel with custom animation
UIManager.Instance.ShowPanel(UIManager.PanelName.Inventory, PanelTransition.Fade);

// Close a panel
UIManager.Instance.HidePanel(UIManager.PanelName.ChestPanel);

// Toggle a panel
UIManager.Instance.TogglePanel(UIManager.PanelName.SellingBinPanel);
```

Panel names are auto-generated from the `ui_panels_folder`. Run **Generate IDs** after adding new panel scenes.

---

### Combat System

#### HealthComponent

Add to any entity that can take damage.

```csharp
// In the inspector, set: MaxHealth, InvincibilityTime, DestroyOnDeath
// Optionally configure per-type resistances (0.0 = immune, 1.0 = full damage, 2.0 = double)

// Subscribe to signals
healthComponent.Damaged += OnDamaged;
healthComponent.Died += OnDied;
healthComponent.Healed += OnHealed;
healthComponent.Revived += OnRevived;
healthComponent.HealthChanged += (old, current) => UpdateUI(current);

// Heal the entity
healthComponent.Heal(25f);

// Force kill
healthComponent.Kill(attackContext);

// Revive at full health or specific amount
healthComponent.Revive();
healthComponent.Revive(50f);

// Invincibility
healthComponent.MakeInvincible();
healthComponent.MakeInvincible(2.5f); // custom duration

// Immunities & Resistances
healthComponent.AddImmunity(DamageType.Magic);
healthComponent.SetResistance(DamageType.Physical, 0.5f); // 50% physical damage
```

#### HitboxComponent & HurtboxComponent

- **HitboxComponent** (`Area2D`) — deals damage when it overlaps a `HurtboxComponent`. Configure `damage`, `damageType`, `knockbackForce`, and `XPMultiplier` in the inspector.
- **HurtboxComponent** (`Area2D`) — receives damage and forwards it to a linked `HealthComponent`. Also forwards knockback to an optional `KnockbackComponent`.

```csharp
hitbox.Enable();
hitbox.Disable();
hitbox.SetCustomDamageSource(someOtherNode);

hurtbox.Enable();
hurtbox.Disable();
```

#### KnockbackComponent

Add to the same node as a `CharacterBody2D`. Assign a `HealthComponent` → link the `HurtboxComponent` to it.

```csharp
// KnockbackType in inspector: Additional (adds to velocity) or Constant (replaces velocity)
knockbackComponent.Knockback(direction * 300f);
knockbackComponent.Knockback(force, duration: 0.4f);
```

#### DamageType Enum

```csharp
public enum DamageType { Physical, Ranged, Magic, Disease, Emotional }
```

#### AttackContext

Passed through the damage pipeline. Read in signal handlers.

```csharp
void OnDamaged(AttackContext ctx)
{
    GD.Print(ctx.Source);           // Node2D that dealt damage
    GD.Print(ctx.RawDamage);        // Damage before resistances
    GD.Print(ctx.EffectiveDamage);  // Damage after resistances
    GD.Print(ctx.Knockback);        // Knockback vector
    GD.Print(ctx.DamageType);       // DamageType enum value
}
```

---

### Event Bus

A static, type-safe event bus. All calls must be made from the **Godot main thread**.

#### Define an event

```csharp
public readonly record struct PlayerDiedEvent();
public readonly record struct ScoreChangedEvent(int NewScore);
```

#### Subscribe

```csharp
// Standard subscription (manual unsubscribe)
EventBus.AddListener<PlayerDiedEvent>(OnPlayerDied);

// Auto-unsubscribe when the node leaves the scene tree
EventBus.AddListener<ScoreChangedEvent>(OnScoreChanged, owner: this);

// Subscribe once, then auto-remove
EventBus.AddListenerOnce<PlayerDiedEvent>(OnPlayerDiedOnce);

// Parameterless variant
EventBus.AddListener<PlayerDiedEvent>(() => GD.Print("Player died!"), owner: this);
```

#### Fire

```csharp
EventBus.Trigger(new ScoreChangedEvent(100));

// Fire with default constructor (no data needed)
EventBus.Trigger<PlayerDiedEvent>();
```

#### Unsubscribe

```csharp
EventBus.RemoveListener<PlayerDiedEvent>(OnPlayerDied);
EventBus.Clear<PlayerDiedEvent>(); // Clear all listeners for one event
EventBus.Clear();                  // Clear everything
```

#### Attribute-based wiring (EventSubscriber)

```csharp
public partial class MyNode : Node
{
    public override void _Ready() => this.WireEvents();

    [EventHandler(typeof(PlayerDiedEvent))]
    private void OnPlayerDied(PlayerDiedEvent e) { ... }

    [EventHandler(Once = true)]
    private void OnScoreOnce(ScoreChangedEvent e) { ... }
}
```

---

### Finite State Machine (FSM)

**Namespace:** `Utilities.FSM`

A fluent, enum-driven FSM with transitions, cooldowns, timeouts, and minimum durations.

#### Setup

```csharp
public enum MyState { Idle, Walk, Attack }

public partial class MyCharacter : CharacterBody2D
{
    private StateMachine<MyState> fsm;

    public override void _Ready()
    {
        fsm = new StateMachine<MyState>(this, MyState.Idle);

        fsm.AddState(MyState.Idle)
            .OnEnter(() => PlayAnimation("idle"))
            .OnUpdate(delta => CheckForInput())
            .OnExit(() => GD.Print("Leaving idle"));

        fsm.AddState(MyState.Walk)
            .OnEnter(() => PlayAnimation("walk"))
            .OnUpdate(delta => Move(delta));

        fsm.AddState(MyState.Attack)
            .OnEnter(() => PlayAnimation("attack"))
            .TimeoutAfter(0.5, MyState.Idle)   // auto-return after 0.5s
            .SetMinDuration(0.2)               // can't leave before 0.2s
            .SetCooldown(1.0);                 // 1s cooldown before re-entering

        // Transitions
        fsm.AddTransition(MyState.Idle, MyState.Walk)
            .SetCondition(() => Velocity.Length() > 10f);

        fsm.AddTransition(MyState.Walk, MyState.Idle)
            .SetCondition(() => Velocity.Length() <= 10f);

        fsm.AddTransition(MyState.Idle, MyState.Attack)
            .SetCondition(() => Input.IsActionJustPressed("attack"));
    }

    public override void _Process(double delta) => fsm.Update(delta);
}
```

#### Manual transitions

```csharp
fsm.ForceTransition(MyState.Walk);
```

---

### Inventory System

**Autoload:** `InventoryManager`

A slot-based inventory with stacking, merging, UI components, and save/load support.

#### InventoryConfig Resource

Create an `InventoryConfig` resource at the path set in Project Settings.

#### Core API

```csharp
// Add items
InventoryManager.Instance.TryStoreItem(itemData);
```

#### ItemData Resource

Create `ItemData` resources (`.tres`) in Godot's inspector. Key exported properties include `Id`, `Art`, `MaxStackSize`, and any custom data for your game.

#### UI Panels

- **PlayerInventoryPanel** — the player's own inventory grid.
- **ExternalInventoryPanel** — for chests, shops, etc.
- **SlotUI** — individual slot rendering; handles drag-and-drop automatically.

Assign panels in the inspector or open them via `UIManager`.

---

### Save System

**Autoload:** `SaveManager`

variant based save/load using reflection (if needed). Mark fields or properties with `[Save]` to include them automatically.

#### ISaveable

Implement `ISaveable` on any class you want saved.

```csharp
public partial class PlayerStats : Node, ISaveable
{
    [Save] public int Gold { get; private set; } = 0;
    [Save] private float health = 100f;
    [Save] private Vector2 position;
}
```

#### Saving & Loading

```csharp
// Save to the default slot
SaveManager.Instance.Save();

// Save to a named slot
SaveManager.Instance.Save("slot_2");

// Load the default slot (returns false if no save exists)
bool loaded = SaveManager.Instance.Load();

// Load a named slot
bool loaded = SaveManager.Instance.Load("slot_2");

// Delete saves
SaveManager.Instance.DeleteSave();
SaveManager.Instance.DeleteSave("slot_2");
```

#### PropertySaveAdapter

For nodes where you can't implement `ISaveable` directly, use `PropertySaveAdapter` as a child node. Assign the target node and configure the `[Save]` keys in the inspector.

#### Supported Types for `[Save]`

`int`, `float`, `double`, `bool`, `string`, `Vector2/3/4` (and Int variants), `Rect2/2I`, `Transform2D/3D`, `Color`, `Enum`, Godot `Array`, Godot `Dictionary`, and any `ISerializableResource`.

---

### Physics2D

**Autoload:** `Physics2D`

A static wrapper around Godot's physics queries with object pooling for zero-allocation raycasts.

```csharp
// Simple hit check
bool hit = Physics2D.Raycast(from, direction, distance);

// Hit check with result
if (Physics2D.Raycast(from, direction, distance, out RaycastHit hit))
{
    GD.Print(hit.Collider);
    GD.Print(hit.Normal);
    GD.Print(hit.Position);
}

// With a specific collision mask
Physics2D.Raycast(from, to, out hit, collisionMask: 0b0011);

// Point-to-point overload
Physics2D.Raycast(from, to);

// Check Sphere
if (Physics2D.CheckSphere(position, out GodotObject collider))
    GD.Print("Detected: {collider.Name}");

// Circle overlap check
var results = Physics2D.OverlapCircle(center, radius);
var results = Physics2D.OverlapCircle(center, radius, collisionMask: myMask);
```

---

### Input Buffer

Stores player input actions for a configurable duration, letting you consume them slightly after they were pressed — useful for jump buffering, combo windows, etc.

```csharp
private InputBuffer inputBuffer = new();

public override void _Process(double delta)
{
    inputBuffer.Tick((float)delta);

    if (Input.IsActionJustPressed("jump"))
        inputBuffer.BufferAction("jump", duration: 0.15f);

    if (IsGrounded() && inputBuffer.TryConsume("jump"))
        Jump();
}
```

Events:

```csharp
inputBuffer.Consumed += name => GD.Print($"{name} consumed");
inputBuffer.Expired  += name => GD.Print($"{name} expired");
```

---

### Object Pooling

**Namespace:** `Utilities.Pooling`

Two pool types are provided.

#### ObjectPool\<T\> — plain C# objects

```csharp
var pool = new ObjectPool<MyClass>(() => new MyClass(), initialSize: 10);

MyClass obj = pool.Get();
// ... use obj ...
pool.Release(obj);

// Trim excess free objects down to a target size
pool.Trim(targetSize: 5);
```

#### NodePool\<T\> — Godot nodes

```csharp
var pool = new NodePool<MyNode>(prefabScene, parentNode, initialSize: 5, extendable: true);

if (pool.TryGet(out MyNode node))
{
    node.GlobalPosition = spawnPoint;
    // ... use node ...
    pool.Release(node);
}

// Pre-warm the pool
pool.Prewarm(10);
```

#### IPoolable Interface

Implement `IPoolable` on your pooled type for automatic callbacks:

```csharp
public class MyPoolable : IPoolable
{
    public void OnGet()     { /* reset state */ }
    public void OnRelease() { /* clean up */    }
}
```

---

### Tween Utilities

#### TweenExtensions

Convenience methods on `Tween`:

```csharp
tween.TweenShakePosition(..);
tween.TweenVolumeLinear(audioPlayer, targetVolume: 0f, duration: 1f);
tween.OnFinished(myCallback);
tween.KillIfValid(); // safe kill — no-ops if null
```

#### TweenVirtual

Run a tween purely by value without a target node:

```csharp
TweenVirtual.Float(from: 0f, to: 100f, duration: 2f, value => GD.Print(value));
```

---

### Extensions

A collection of extension methods for common Godot types.

#### Node2D

```csharp
node2D.SmoothlyLookAt(target);             // face a target, accounting for sprite offsets
node2D.SmoothlyFollow(target);
```

#### CharacterBody2D

```csharp
body.ApplyGravity(..);
body.Jump(heightInPixels: 40f);
```

#### SceneTree

```csharp
await tree.Delay(1.5f);
await tree.NextFrame(3);
```

#### AnimatedSprite2D

```csharp
sprite.PlayIfExist("run");
sprite.PlayFrames("run", customSpriteFrames);
```

#### GodotObject

```csharp
obj.ConnectOnce(signalName, callable);
```

---

### Utilities

#### MathUtil

```csharp
float smoothed  = MathUtil.DeltaLerp(from, to, delta, weight: 8f);
Vector2 smoothed = MathUtil.DeltaLerp(fromVec, toVec, delta, weight: 8f);

float clamped  = MathUtil.Clamp01(value);
float norm     = MathUtil.Normalize(value, max);      // value / max
float rand     = MathUtil.RandfRange(-1f, 1f);
Vector2 unit   = MathUtil.RandomUnit();
Vector2 onCirc = MathUtil.RandomOnCircle(radius: 50f);
Vector2 inRect = MathUtil.RandomInRect(myRect);
```

#### Cooldown

A lightweight, allocation-free timer struct.

```csharp
private Cooldown attackCooldown = new Cooldown(0.5f);

public override void _Process(double delta)
{
    attackCooldown.Tick(delta);

    if (Input.IsActionPressed("attack") && attackCooldown.IsReady)
    {
        Attack();
        attackCooldown.Start();
    }

    // Read cooldown state
    float remaining = attackCooldown.Remaining;  // seconds left
    float progress  = attackCooldown.Progress;   // 0.0 → 1.0
}
```

#### WeightedTable\<T\>

```csharp
var rng   = new RandomNumberGenerator();
var table = new WeightedTable<string>(rng);

table.Add("Common",   weight: 60f)
     .Add("Rare",     weight: 25f)
     .Add("Epic",     weight: 10f)
     .Add("Legendary", weight: 5f);

string drop = table.Pick();

// Pick multiple
foreach (var item in table.PickMany(count: 3))
    GD.Print(item);

// Get the probability of a specific entry
float prob = table.GetProbability("Legendary");  // 0.05

table.Clear();
```

#### VelocityComponent

Attach to any `CharacterBody2D` to manage velocity with acceleration, friction, and max-speed clamping.

```csharp
velocityComponent.Accelerate(direction, delta);
velocityComponent.Decelerate(delta);
velocityComponent.Move(direction, delta);
```

## License

MIT — see `LICENSE` for details.
