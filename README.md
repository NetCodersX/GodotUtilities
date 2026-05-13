# GodotUtilities

A collection of reusable C# systems for Godot 4. Covers audio management, particles, inventory, UI management, a save system, a finite state machine, physics helpers, pooling, and more. Each system is designed to be dropped in and configured through Project Settings rather than requiring code changes.

---

## Table of Contents

- [Installation](#installation)
- [Project Settings](#project-settings)
- [ID Generation](#id-generation)
- [Systems](#systems)
  - [Audio Manager](#audio-manager)
  - [Particles Manager](#particles-manager)
  - [UI Manager](#ui-manager)
  - [Inventory System](#inventory-system)
  - [Save System](#save-system)
  - [State Machine](#state-machine)
  - [Physics2D](#physics2d)
  - [Event Bus](#event-bus)
  - [Pooling](#pooling)
  - [Tween Extensions](#tween-extensions)
  - [Utility Classes](#utility-classes)

---

## Installation

1. Copy the `GodotUtilities` folder into your project's `addons/` directory.
2. Open **Project > Project Settings > Plugins** and enable **GodotUtilities**.
3. After enabling, open **Project > Tools** and run **GodotUtilities: Add Autoloads**. This registers all singleton managers (`SaveManager`, `AudioManager`, `ParticlesManager`, `UIManager`, `InventoryManager`, `Physics2D`) as autoloads.
4. Fill in the folder paths and configuration values under **Project Settings** (see the section below).
5. Run **GodotUtilities: Generate IDs** from the Tools menu to generate the typed constant files for audio, particles, and UI panels.

> The plugin also runs ID generation automatically when it first loads. You only need to re-run it manually when you add or remove assets.

---

## Project Settings

After enabling the plugin, the following settings will appear under `godot_utilities/` in Project Settings:

| Setting | Type | Description |
|---|---|---|
| `inventory_config_path` | File (`.tres`) | Path to the `InventoryConfig` resource |
| `sfx_folder` | Directory | Folder containing sound effect audio files |
| `music_folder` | Directory | Folder containing music audio files |
| `ambience_folder` | Directory | Folder containing ambience audio files |
| `particles_folder` | Directory | Folder containing particle `.tscn` scenes |
| `ui_panels_folder` | Directory | Folder containing standard UI panel scenes |
| `ui_hud_panels_folder` | Directory | Folder containing HUD panel scenes |
| `audio_sfx_pool_size` | Int | Initial and minimum pool size for SFX players (default: 4) |
| `audio_sfx_pool_trim_cooldown` | Float | How often (in seconds) the SFX pool is trimmed (default: 5.0) |

---

## ID Generation

To avoid using raw strings throughout the codebase, the plugin generates strongly-typed C# constant classes from your asset folders. The output files are placed in `addons/GodotUtilities/generated/` and are never edited manually.

Generated files:

- `AudioManager.gen.cs` — `SfxName`, `MusicName`, `AmbienceName` constants
- `ParticlesManager.gen.cs` — `ParticleName` constants
- `UIManager.gen.cs` — `PanelName` constants

Each generated class contains:

- A nested static class of `StringName` constants (e.g. `SfxName.RockHit`)
- A comma-separated `const string` suitable for use with `[Export(PropertyHint.Enum, ...)]`
- A dictionary mapping each constant to its resource path

Names are derived from file basenames. The generator strips extensions, sorts names alphabetically, and converts them to PascalCase for the constant names while keeping the original snake_case as the string value.

---

## Systems

### Audio Manager

`AudioManager` is a singleton that handles three categories of audio: SFX, Music, and Ambience. SFX are played from a pooled set of `AudioStreamPlayer` nodes. Music and ambience each have a dedicated crossfade channel.

**Playing SFX**

```csharp
// One-shot
AudioManager.Instance.PlaySfx(SfxName.RockHit);

// With pitch variance and volume
AudioManager.Instance.PlaySfx(SfxName.TreeHit, pitchVariance: 0.1f, volumeLinear: 0.8f);

// Positional (2D)
AudioManager.Instance.PlaySfx2D(SfxName.WaterCan, position: GlobalPosition);

// Looped N times
AudioManager.Instance.PlaySfx(SfxName.Digging, cycles: 3);
```

Both `PlaySfx` and `PlaySfx2D` return handle structs (`SfxHandle` and `Sfx2DHandle`) that let you modify the sound while it is playing:

```csharp
var handle = AudioManager.Instance.PlaySfx2D(SfxName.Digging, GlobalPosition);
handle.SetPitch(1.3f);
handle.SetVolume(0.5f);
handle.SetPosition(newPosition);
```

The handle becomes inert once the sound finishes, so it is safe to hold onto without checking for node validity.

**SFX Pooling**

SFX players are managed by two `ObjectPool` instances — one for `PooledAudioPlayer` (non-positional) and one for `PooledAudioPlayer2D`. When a sound finishes, the player resets its state and returns itself to the pool. The pool is periodically trimmed on a timer; excess players beyond `audio_sfx_pool_size * 2` are freed.

**Music and Ambience**

Music and ambience use a `CrossfadeChannel` internally, which holds two `AudioStreamPlayer` nodes and tweens volume between them. This gives smooth crossfades without any audio pops.

```csharp
// Play immediately with a crossfade
AudioManager.Instance.PlayMusic(MusicName.GentleBreeze, fadeDuration: 2f);

// Queue to play after the current track finishes
AudioManager.Instance.ThenPlayMusic(MusicName.GoldenGleam);

// Stop with fade
AudioManager.Instance.StopMusic(fadeDuration: 1.5f);

// Pause and resume (instant, no fade)
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();
```

Ambience uses the same API via `PlayAmbience`, `ThenPlayAmbience`, `StopAmbience`, etc.

**Volume Control**

All volumes are set as linear values (0.0 to 1.0) and converted to decibels internally:

```csharp
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetMusicVolume(0.5f);
AudioManager.Instance.SetSfxVolume(1.0f);
AudioManager.Instance.SetAmbienceVolume(0.6f);
```

**Preloading and Unloading**

Audio streams are lazy-loaded on first use and cached. You can warm up specific sounds in advance:

```csharp
AudioManager.Instance.WarmUpSfx(SfxName.RockHit, SfxName.TreeHit);
```

To free memory when leaving a scene:

```csharp
AudioManager.Instance.UnloadAllSfx();
AudioManager.Instance.UnloadMusic(MusicName.GoldenGleam);
```

---

### Particles Manager

`ParticlesManager` handles pooled `GpuParticles2D` scenes. Each particle ID gets its own `NodePool`, which grows on demand and is trimmed periodically. When a scene changes, all pools are destroyed automatically.

**Spawning particles**

```csharp
ParticlesManager.Instance.Spawn(ParticleName.RockHit, GlobalPosition);

// Optionally parent to a specific node (e.g. for relative positioning)
ParticlesManager.Instance.Spawn(ParticleName.DirtDust, GlobalPosition, someParentNode);
```

`Spawn` returns a `ParticleHandle`, a lightweight struct that lets you update a particle while it is emitting:

```csharp
var handle = ParticlesManager.Instance.Spawn(ParticleName.LeavesFall, tree.GlobalPosition);
handle.SetPosition(newPos);
handle.SetDirection(Vector2.Up);
```

The handle does nothing if the particle has already finished, so it is safe to store without node lifecycle concerns.

**Particle scenes**

Each scene in the configured `particles_folder` must have a scene root that is `PooledParticle`. The component calls `Restart()` and sets `Emitting = true` when retrieved from the pool, then releases itself back when its `Finished` signal fires.

**Preloading**

```csharp
ParticlesManager.Instance.WarmUp(ParticleName.DirtDust, ParticleName.GroundHit);
```

---

### UI Manager

`UIManager` handles loading, showing, hiding, and animating UI panels. Panels are lazy-loaded from the configured folders the first time they are requested.

**Panel types**

There are three panel types, each assigned to a different `CanvasLayer`:

- `HUD` — always-visible overlays like hotbars and debug labels. Loaded and set up on startup.
- `Screen` — full-screen panels managed in a stack. Showing a new screen closes the current one.
- `Popup` — temporary overlays that appear and disappear automatically.

**Creating a panel**

Create a scene with a `UIPanel` root (or a class that extends it). Set the `Type` export to the appropriate value and place the scene file in the correct configured folder.

Override the virtual methods as needed:

```csharp
public partial class MyPanel : UIPanel
{
    public override void OnSetup()  { /* called once when first loaded */ }
    public override void OnOpen()  { /* called every time the panel becomes visible */ }
    public override void OnClose() { /* called every time the panel is hidden */ }
}
```

**Showing and hiding panels**

```csharp
UIManager.Instance.ShowPanel(PanelName.Inventory);
UIManager.Instance.HidePanel(PanelName.Inventory);
UIManager.Instance.TogglePanel(PanelName.Inventory);

// With transition
UIManager.Instance.ShowPanel(PanelName.Inventory, PanelTransition.Fade);
UIManager.Instance.HidePanel(PanelName.Inventory, PanelTransition.Pop);
```

Available transitions: `Constant`, `Fade`, `Pop`, `Slide`.

**Popups**

```csharp
// Shows with a Pop transition, waits 2 seconds, then hides automatically
UIManager.Instance.ShowPopup(PanelName.ItemTooltip, delay: 2f);
```

**Screen stack navigation**

```csharp
// Opens a screen panel; closes the current one first
UIManager.Instance.ShowPanel(PanelName.Inventory, PanelTransition.Slide);

// Goes back to the previous screen
UIManager.Instance.GoBack(PanelTransition.Slide);
```

**Accessing a panel directly**

```csharp
var panel = UIManager.Instance.GetPanel<ExternalInventoryPanel>(PanelName.ChestPanel);

if (UIManager.Instance.TryGetPanel<ExternalInventoryPanel>(PanelName.ChestPanel, out var chestPanel))
    chestPanel.SetInventoryData(chestData);
```

**Listening to panel events**

```csharp
UIManager.Instance.PanelOpened += panelName => GD.Print($"{panelName} opened");
UIManager.Instance.PanelClosed += panelName => GD.Print($"{panelName} closed");
```

---

### Inventory System

The inventory system is built around three core types: `ItemData` (the item definition resource), `InventoryData` (a container of slots), and `SlotData` (a runtime instance of an item stack). The `InventoryManager` singleton ties everything together.

**Setup**

Create an `InventoryConfig` resource (`.tres`) and set the path in Project Settings under `godot_utilities/inventory_config_path`. The config holds references to the player inventory and hotbar `InventoryData` resources, the loot item scene, and the item stack window duration.

Items are loaded automatically from the folder specified in `itemsFolder` on the config. Each item is an `ItemData` resource with a unique `Id` string name.

**ItemData**

```
Id          — StringName used to look up the item at runtime and in save files
Art         — AtlasTexture shown in the UI
Stackable   — whether multiple can occupy one slot
MaxStackSize — stack limit (defaults to 64)
Description — multiline tooltip text
```

**InventoryData**

`InventoryData` is a Godot `Resource` that holds an array of `SlotData`. It emits `InventoryUpdated` whenever its contents change, and `InventoryInteract` when a slot is clicked from the UI.

Common operations:

```csharp
// Store an item (tries to merge into existing stacks first, then empty slots)
inventoryData.TryStoreItem(slotData);

// Transfer a slot to another inventory
inventoryData.TransferItemTo(fromIndex, targetInventoryData);

// Read a slot
SlotData slot = inventoryData.GetSlotData(index);

// Check capacity
bool full = inventoryData.IsFull();
```

**SlotData**

`SlotData` is the runtime wrapper around an `ItemData` with a quantity. It handles merging, splitting, and extraction:

```csharp
// Copy a slot
SlotData copy = SlotData.Copy(original);

// Merge another slot into this one; returns overflow if the stack is full
SlotData overflow = slotA.MergeWith(slotB);

// Pull one item out (useful for placing single items)
SlotData single = stack.ExtractOne();

// Consume one item (e.g. on use)
bool consumed = slot.TryConsume(clear: false);
```

**InventoryManager**

The manager is an autoloaded singleton. Access the main inventories via:

```csharp
InventoryManager.Instance.PlayerInventory
InventoryManager.Instance.HotbarData
```

Dropping items into the world:

```csharp
// Spawns a world item from a specific slot and removes it from the inventory
var loot = InventoryManager.Instance.DropItem<WorldItem>(data, slotIndex, spawnPosition);

// From the current hotbar slot
var loot = InventoryManager.Instance.DropHotbarItem<WorldItem>(spawnPosition);
```

The spawned node's scene must implement `ISlotDataReceiver`:

```csharp
public partial class WorldItem : Node2D, ISlotDataReceiver
{
    public void Receive(SlotData slotData) { /* set up the world item from slotData */ }
}
```

**Hotbar**

```csharp
// Scroll by direction (-1 or +1)
InventoryManager.Instance.UpdateHotbarDirection(1);

// Jump to a specific index
InventoryManager.Instance.UpdateHotbar(2);

// Consume the selected hotbar item
InventoryManager.Instance.TryConsumeHotbarItem();
```

Subscribe to `HotbarUpdated` to receive the newly selected `SlotData` whenever the selection changes:

```csharp
InventoryManager.Instance.HotbarUpdated += slotData => UpdateEquippedItemDisplay(slotData);
```

**External inventory panels (chests, shops)**

Any `UIPanel` that implements `IInventoryPanel` will trigger inventory-specific logic when opened. For external inventories (chests, containers), extend `ExternalInventoryPanel` and call `SetInventoryData` before opening:

```csharp
var chestPanel = UIManager.Instance.GetPanel<ExternalInventoryPanel>(PanelName.ChestPanel);
chestPanel.SetInventoryData(chestInventoryData);
UIManager.Instance.ShowPanel(PanelName.ChestPanel, PanelTransition.Fade);
```

**Shift-click stacking**

Holding Shift and left-clicking a slot will attempt to transfer it to the other open inventory. Double-clicking (or clicking quickly) a held item will collect all matching stacks from visible inventories.

---

### Save System

The save system serializes registered `ISaveable` nodes to a binary file using Godot's `FileAccess.StoreVar`. Data is keyed by `SaveKey` strings.

**Making a node saveable**

Implement `ISaveable` on a node and annotate the fields or properties you want saved with `[Save]`:

```csharp
public partial class PlayerStats : Node, ISaveable
{
    public string SaveKey => "player_stats";

    [Save] private int gold;
    [Save] public int Level { get; private set; }
    [Save] private Vector2 lastPosition;

    public void OnDeserialize(Dictionary data)
    {
        // called after reflection-based deserialization
        // useful for applying loaded values to the scene
    }
}
```

Register and unregister with the save manager in `_EnterTree` and `_ExitTree`:

```csharp
public override void _EnterTree() => SaveManager.Instance.Register(this);
public override void _ExitTree()  => SaveManager.Instance.Unregister(this);
```

**Supported field types**

The `[Save]` attribute supports:

- Primitives: `int`, `float`, `double`, `bool`, `string`
- Godot value types: `Vector2`, `Vector2I`, `Vector3`, `Vector3I`, `Vector4`, `Vector4I`, `Rect2`, `Rect2I`, `Transform2D`, `Transform3D`, `Color`
- Enums (saved as int)
- `Godot.Collections.Array` and `Godot.Collections.Dictionary`
- Any type implementing `ISerializableResource`
- `List<T>` and `Dictionary<K, V>` where `T` or `V` implements `ISerializableResource`

For anything else, use `OnSerialize` and `OnDeserialize` to handle it manually.

**Saving and loading**

```csharp
SaveManager.Instance.Save();        // saves to default slot ("slot1")
SaveManager.Instance.Load();        // loads from default slot

SaveManager.Instance.Save("slot2"); // named slot
SaveManager.Instance.Load("slot2");

bool exists = SaveManager.Instance.SaveExists();
SaveManager.Instance.DeleteSave();
```

**Dynamic spawning**

Nodes that are not present in the scene at load time can be re-created automatically. Set `ScenePath` on `ISaveable` to the node's `SceneFilePath`, and optionally `ParentPath` to the node path of the desired parent (or use `PropertySaveAdapter` + dynamic spawning). The save manager will instantiate and add the node before deserializing it.

**PropertySaveAdapter**

For nodes without a custom script (or built-in nodes), attach a `PropertySaveAdapter` child. In the inspector, select which built-in properties to save. The adapter handles serialization and deserialization transparently without touching the parent node's script.

Set a `uniqueId` on the adapter if the parent node's path might not be stable across sessions.

---

### State Machine

`StateMachine<T>` is a generic, data-driven FSM. `T` must be an enum.

**Basic setup**

```csharp
public enum PlayerState { Idle, Walk, Jump, Attack }

private StateMachine<PlayerState> fsm = new();

public override void _Ready()
{
    fsm.AddState(PlayerState.Idle)
        .OnEnter(() => PlayAnimation("idle"))
        .OnUpdate(dt => CheckMovement());

    fsm.AddState(PlayerState.Walk)
        .OnEnter(() => PlayAnimation("walk"))
        .OnUpdate(dt => Move(dt));

    fsm.AddState(PlayerState.Jump)
        .OnEnter(() => velocity.TryJump())
        .TimeoutAfter(0.8, PlayerState.Idle);

    fsm.AddTransition(PlayerState.Idle, PlayerState.Walk)
        .When(() => inputDir != Vector2.Zero);

    fsm.AddTransition(PlayerState.Walk, PlayerState.Idle)
        .When(() => inputDir == Vector2.Zero);

    fsm.AddTransition(PlayerState.Idle, PlayerState.Jump)
        .OnEvent("jump");

    fsm.SetInitialState(PlayerState.Idle);
    fsm.Start();
}

public override void _Process(double delta) => fsm.Tick(delta);
public override void _PhysicsProcess(double delta) { /* same */ }
```

**Transitions**

Transitions can be condition-based, event-based, or both.

```csharp
// Fires when the condition returns true
fsm.AddTransition(from, to).When(() => someCondition);

// Fires when a named event is triggered
fsm.AddTransition(from, to).OnEvent("hit");

// Blocked unless the guard passes (useful for prerequisites)
fsm.AddTransition(from, to).When(() => true).IfOnly(() => canAttack);

// Higher priority transitions are evaluated first
fsm.AddTransition(from, to).SetPriority(1);

// Applies to all states (useful for death, stun, etc.)
fsm.AddGlobalTransition(PlayerState.Dead).When(() => health.IsDead());
```

Trigger an event from outside:

```csharp
fsm.TriggerEvent("jump");
fsm.TriggerEvent("hit");
```

**State options**

```csharp
fsm.AddState(PlayerState.Attack)
    .OnEnter(() => hitbox.Enable())
    .OnExit(() => hitbox.Disable())
    .SetMinDuration(0.3)            // must stay at least 0.3s before any transition fires
    .TimeoutAfter(0.6, PlayerState.Idle) // auto-exits after 0.6s
    .SetCooldown(1.0);              // cannot re-enter for 1s after leaving
```

**Runtime control**

```csharp
fsm.Lock();         // prevents transitions from firing
fsm.Unlock();
fsm.Pause();        // stops Update and transitions but keeps state
fsm.Resume();
fsm.Reset();        // returns to initial state
fsm.ForceTransition(PlayerState.Idle); // bypasses locks and min duration

bool inJump = fsm.IsInState(PlayerState.Jump);
double elapsed = fsm.TimeInState;
double remaining = fsm.RemainingTime(); // only valid if TimeoutAfter is set
```

---

### Physics2D

`Physics2D` is a singleton that wraps Godot's `PhysicsDirectSpaceState2D` with a pooled query object API, removing the overhead of creating query objects every frame.

**Raycasting**

```csharp
// Direction + distance
Physics2D.Raycast(origin, direction, 200f);

// Point to point
Physics2D.Raycast(from, to, out RaycastHit hit);

// With collision mask
Physics2D.Raycast(origin, direction, 200f, out hit, collisionMask: 1 << 2);
```

`RaycastHit` contains `Position`, `Normal`, `Collider`, and `ColliderRid`. Use `hit.GetCollider<T>()` to cast the collider directly.

**Overlap checks**

```csharp
// Returns true if anything overlaps a circle at position
Physics2D.CheckSphere(position, radius: 50f);

// With out collider
Physics2D.CheckSphere(position, 50f, out GodotObject collider);

// Returns all overlapping objects
GodotObject[] hits = Physics2D.OverlapSphere(position, 50f);

// Limit results
GodotObject[] hits = Physics2D.OverlapSphere(position, 50f, collisionMask: myMask, maxResults: 4);
```

---

### Event Bus

`EventBus` is a static, type-safe publish/subscribe bus. All calls must be made from the main thread.

**Defining events**

Events are plain C# classes or structs:

```csharp
public readonly record struct PlayerDiedEvent();
public readonly record struct ItemPickedUpEvent(SlotData Item);
```

**Subscribing**

```csharp
// With event data
EventBus.AddListener<ItemPickedUpEvent>(OnItemPickedUp, owner: this);

private void OnItemPickedUp(ItemPickedUpEvent @event) { /* ... */ }

// Without event data (if event doesn't contain parameters)
EventBus.AddListener<PlayerDiedEvent>(ShowDeathScreen, owner: this);

private void ShowDeathScreen() { /* ... */ }

// One-shot
EventBus.AddListenerOnce<PlayerDiedEvent>(OnFirstDeath);
```

Passing `owner` (a `Node`) will automatically unsubscribe the listener when the node leaves the tree.

**Using the attribute**

On any Node, call `this.WireEvents()` in `_Ready`, and annotate methods with `[EventHandler]`:

```csharp
public override void _Ready() => this.WireEvents();

[EventHandler]
private void OnItemPickedUp(ItemPickedUpEvent e) { /* ... */ }

[EventHandler(typeof(PlayerDiedEvent))]
private void OnPlayerDied() { /* no parameter needed */ }

[EventHandler(Once = true)]
private void OnFirstPickup(ItemPickedUpEvent e) { /* fires once then removes itself */ }
```

**Publishing**

```csharp
EventBus.Trigger(new ItemPickedUpEvent(slotData));

// Default constructor shorthand (if event is contains no params)
EventBus.Trigger<PlayerDiedEvent>();
```

---

### Pooling

Two pool types are provided.

**ObjectPool\<T\>**

For plain C# objects. Pass a factory function and an optional initial size:

```csharp
var pool = new ObjectPool<MyObject>(() => new MyObject(), initialSize: 10);

var obj = pool.Get();
pool.Release(obj);
pool.Trim(keepCount: 5);
```

**NodePool\<T\>**

For Godot `Node` subclasses. Manages `AddChild`/`QueueFree` and handles visibility and `ProcessMode` automatically:

```csharp
var pool = new NodePool<PooledParticle>(scene, parent: currentScene, initialSize: 3);

var node = pool.Get();
pool.Release(node);
pool.Destroy(); // frees all nodes, in use or not
```

Implementing `IPoolable` on a node gives callbacks when it is retrieved or released:

```csharp
public partial class MyPooledNode : Node2D, IPoolable
{
    public void OnGet()     { /* set up */ }
    public void OnRelease() { /* clean up */ }
}
```

---

### Tween Extensions

Extension methods on `Tween` and `PropertyTweener` for common patterns. Import with `using Utilities;`.

**Movement and transform**

```csharp
tween.TweenPosition(node, targetPos, 0.3f);
tween.TweenGlobalPosition(node, targetPos, 0.3f);
tween.TweenScale(node, new Vector2(1.5f, 1.5f), 0.2f);
tween.TweenScaleUniform(node, 1.2f, 0.2f);
tween.TweenRotationDeg(node, 45f, 0.4f);
```

**Shake, punch, and squish**

```csharp
tween.TweenShakePosition(node, duration: 0.4f, strength: 8f);
tween.TweenShakeRotation(node, duration: 0.3f, strength: 0.4f);

tween.TweenPunchScale(node, duration: 0.25f, amount: new Vector2(0.3f, 0.3f));
tween.TweenPunchPosition(node, duration: 0.2f, amount: new Vector2(20f, 0f));
tween.TweenPunchRotation(node, duration: 0.2f, amount: 15f);

tween.TweenSquish(node, duration: 0.3f, ratio: 0.2f, dir: TweenExtensions.SquishDir.Up);
tween.TweenWiggle(node, degrees: 12f, duration: 0.3f);
```

**UI**

```csharp
tween.TweenFadeIn(canvasItem, 0.2f);
tween.TweenFadeOut(canvasItem, 0.2f);
tween.TweenPopIn(control, 0.15f);
tween.TweenPopOut(control, 0.15f);
tween.TweenTypewriter(label, duration: 2f);
tween.TweenCounter(label, from: 0f, to: 100f, duration: 1f);
```

**Transition and ease shorthands**

```csharp
tween.TweenScale(node, Vector2.One, 0.3f).Back().EaseOut();
tween.TweenFade(node, 0f, 0.2f).Sine().EaseIn();
```

**Signals**

```csharp
tween.OnFinished(() => DoSomething());
await tween.WaitToFinish();
```

**TweenVirtual**

For tweening arbitrary values without a target property:

```csharp
TweenVirtual.Float(this, 0f, 1f, 0.5f, value => myShader.SetShaderParameter("progress", value));
TweenVirtual.Color(this, Color.White, Color.Red, 0.3f, c => sprite.Modulate = c);
```

---

### Utility Classes

**MathUtil**

```csharp
MathUtil.DeltaLerp(from, to, dt, weight);   // framerate-independent lerp
MathUtil.RandfRange(0f, 1f);
MathUtil.Chance(0.25f);                      // returns true 25% of the time
MathUtil.CoinFlip();
MathUtil.PickRandom(itemA, itemB, itemC);
MathUtil.RandomInCircle(radius: 100f);
MathUtil.PointsOnCircle(radius: 80f, count: 8);
MathUtil.OffscreenSpawn(camera, margin: 50f);
MathUtil.Normalize(health, maxHealth);
```

**Cooldown**

A lightweight value-type timer. Do not mark fields as `readonly`.

```csharp
private Cooldown attackCooldown = new(duration: 0.5f);

public override void _Process(double delta)
{
    attackCooldown.Tick(delta);
}

public void TryAttack()
{
    if (!attackCooldown.IsReady) return;
    attackCooldown.Start();
    // ...
}

// One-off duration
attackCooldown.Start(2f);
float remaining = attackCooldown.Remaining;
float progress  = attackCooldown.Progress; // 0 to 1
```

**WeightedTable\<T\>**

```csharp
var table = new WeightedTable<string>()
    .Add("common", 60f)
    .Add("uncommon", 30f)
    .Add("rare", 10f);

string result = table.Pick();
IEnumerable<string> drops = table.PickMany(3);
float rareProbability = table.GetProbability("rare");
```

**InputBuffer**

Stores player inputs for a short window so they are not lost if the action cannot be consumed immediately (e.g. pressing jump just before landing).

```csharp
private InputBuffer inputBuffer = new();

public override void _Process(double delta) => inputBuffer.Tick((float)delta);

public override void _Input(InputEvent @event)
{
    if (@event.IsActionPressed("jump"))
        inputBuffer.BufferAction("jump", duration: 0.15f);
}

public override void _PhysicsProcess(double delta)
{
    if (velocity.IsGrounded() && inputBuffer.TryConsume("jump"))
        velocity.Jump();
}
```

**Log**

```csharp
Log.Info("Player", "Game started");
Log.Warn("Inventory", "Item not found");
Log.Error("SaveSystem", "File could not be opened");
Log.Debug("AI", "Path recalculated");

Log.Disable("AI");  // suppress a category
Log.Enable("AI");
Log.Enabled = false; // disable all logging
```

**Extension methods**

A set of small extension methods are included for common tasks:

```csharp
// Node
node.TryGetChild<HealthComponent>(out var health);
node.GetComponent<HitboxComponent>();
node.RequireComponent<HealthComponent>(); // throws if missing
node.DestroyChildren();

// Vector2
position.IsWithinDistance(target, 100f);
direction.RotatedDegrees(90f);

// Node2D
node.SmoothlyLookAt(target, acceleration: 10f, dt);
node.SmoothlyFollow(target, acceleration: 8f, dt);
node.GetMouseDirection();

// SceneTree
GetTree().Delay(1.5, () => DoSomething());
await GetTree().Delay(1.5);
await GetTree().NextFrame();

// AnimatedSprite2D
sprite.PlayIfExist("run");
await sprite.WaitToFinish();

// bool
bool moving = inputDir != Vector2.Zero;
float blend = moving.ToSingle(); // 0f or 1f
int sign = moving.ToSign();      // -1 or 1
```

---
