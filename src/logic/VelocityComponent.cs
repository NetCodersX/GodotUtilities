using System;
using Godot;

namespace Utilities.Logic;

[GlobalClass]
public partial class VelocityComponent : Node
{
    public enum GravityState
    {
        Floor,
        Ceiling
    }

    public enum FalloffCurve
    {
        Linear,
        Quadratic,
        InverseSquare
    }

    [Signal] public delegate void MotionModeChangedEventHandler();

    [Signal] public delegate void JumpedEventHandler(int count);

    [Signal] public delegate void FellEventHandler();
    [Signal] public delegate void FellOffEdgeEventHandler();

    [Signal] public delegate void ApexReachedEventHandler();
    [Signal] public delegate void LandedEventHandler();

    [Signal] public delegate void GravitySwitchedEventHandler();

    public const float DEFAULT_GRAVITY = 980f;
    public const float FALL_THRESHOLD  = 0.1f;

    [Export]
    private GravityState State
    {
        get => gravityState;
        set => SetGravityState(value);
    }

    [Export(PropertyHint.Range, "10, 600")]
    private float maxSpeed = 100f;

    [Export(PropertyHint.Range, "0.1, 100")]
    private float mass = 1f;

    [ExportGroup("Control")]

    [Export(PropertyHint.Range, "1, 300")]
    private float acceleration = 50;

    [Export(PropertyHint.Range, "1, 320")]
    private float deceleration = 60;

    [ExportSubgroup("Air Control")]

    [Export(PropertyHint.Range, "0.1, 100")]
    private float airAcceleration = 30f;

    [Export(PropertyHint.Range, "0, 100")]
    private float airDeceleration = 30f;

    [ExportGroup("Jump")]

    [Export(PropertyHint.Range, "5, 200")]
    private float jumpHeight = 40f;

    [Export(PropertyHint.Range, "1, 10")]
    private int maxJumps = 1;

    [Export(PropertyHint.Range, "0.05, 0.5")]
    private float coyoteTime = 0.15f;

    [Export(PropertyHint.Range, "0.05, 0.5")]
    private float jumpBufferTime = 0.15f;

    [ExportSubgroup("Apex")]

    [Export]
    private bool enableApexHanging = true;

    [Export(PropertyHint.Range, "1.1, 2")]
    private float apexHorizontalBoost = 1.4f;

    [Export(PropertyHint.Range, "0.02, 0.3")]
    private float apexHangDuration = 0.05f;

    [Export(PropertyHint.Range, "0.1, 0.9")]
    private float apexGravityReduction = 0.3f;

    [ExportGroup("Gravity")]

    [Export(PropertyHint.Range, "0.1, 20")]
    private float gravityScale = 1f;

    [Export(PropertyHint.Range, "0.1, 20")]
    private float fallGravityMultiplier = 1f;

    [Export(PropertyHint.Range, "50, 1000")]
    private float maxFallSpeed = 300f;


    private CharacterBody2D controller;
    private Vector2 velocity;

    private Cooldown coyoteTimer     = new();
    private Cooldown jumpBufferTimer = new();

    private float airAccelerationBase;
    private float airDecelerationBase;
    private float weightFactor = 1f;

    private float prevVerticalSpeed;
    private float apexHangTimer;
    private int   jumpsUsed;

    private bool useGravity = true;
    private bool floatingMode;
    private bool wasGrounded;
    private bool wasFalling;
    private bool isGrounded;
    private bool isFalling;
    private bool reachedApex;

    private GravityState gravityState = GravityState.Floor;

    #region Properties

    public int JumpsRemaining => Mathf.Max(0, maxJumps - jumpsUsed);

    public float Mass     => mass;
    public float MaxSpeed => maxSpeed;

    public float CurrentSpeed  => floatingMode ? velocity.Length() : Mathf.Abs(velocity.X);
    public float VerticalSpeed => velocity.Dot(controller.UpDirection);

    public Vector2 Velocity => velocity;

    public GravityState CurrentGravityState => gravityState;

    public float FallSpeed => isFalling ? velocity.Dot(-controller.UpDirection) : 0f;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float MaxFallSpeed => maxFallSpeed;

    public float WeightFactor => weightFactor;

    #endregion

    public override void _Ready()
    {
        controller = GetOwnerOrNull<CharacterBody2D>()
            ?? throw new Exception("[VelocityComponent] Invalid Controller, Make sure that owner is a CharacterBody2D");

        airAccelerationBase = airAcceleration;
        airDecelerationBase = airDeceleration;

        floatingMode = controller.MotionMode == CharacterBody2D.MotionModeEnum.Floating;
    }

    public override void _PhysicsProcess(double delta)
    {
        coyoteTimer.Tick(delta);
        jumpBufferTimer.Tick(delta);

        wasGrounded = isGrounded;
        wasFalling  = isFalling;

        ApplyGravity(delta);
        MoveAndSlide();

        Vector2 gravityDir = -controller.UpDirection;
        float fallSpeed    = velocity.Dot(gravityDir);

        isGrounded = controller.IsOnFloor();
        isFalling = !isGrounded && fallSpeed > FALL_THRESHOLD;

        CheckApex();
        CheckStates();
    }

    #region Internal

    private void MoveAndSlide()
    {
        controller.Velocity = velocity;
        controller.MoveAndSlide();
        velocity = controller.Velocity;
    }

    private void CheckStates()
    {
        if (!wasFalling && isFalling)
        {
            EmitSignalFell();

            if (!reachedApex)
            {
                EmitSignalFellOffEdge();
                AcquireCoyote();
                ConsumeJump();
            }
        }

        if (!wasGrounded && isGrounded)
        {
            EmitSignalLanded();
            ResetJumps();
        }
    }

    private void CheckApex()
    {
        float verticalSpeed = velocity.Dot(controller.UpDirection);

        bool wasGoingDown = prevVerticalSpeed < 0f;
        bool wasGoingUp   = prevVerticalSpeed > 0f;
        bool nowGoingUp   = verticalSpeed     > 0f;
        bool nowGoingDown = verticalSpeed     <= 0f;

        if (!reachedApex && wasGoingUp && nowGoingDown)
        {
            reachedApex = true;
            OnApex();
        }

        bool reset = (reachedApex && wasGoingDown && nowGoingUp) || isGrounded;

        if (reset)
            reachedApex = false;

        prevVerticalSpeed = verticalSpeed;
    }

    private void OnApex()
    {
        if (enableApexHanging)
            apexHangTimer = apexHangDuration;
        EmitSignalApexReached();
    }

    #endregion

    #region Movement

    public void AccelerateWithSpeed(Vector2 dir, float dt, float speed) =>
        ApplyMovement(dir, dt, speed, (isGrounded || floatingMode) ? acceleration : airAcceleration);

    public void AccelerateScaled(Vector2 dir, float dt, float scale) =>
        AccelerateWithSpeed(dir, dt, maxSpeed * scale);

    public void Accelerate(Vector2 dir, float dt) =>
        AccelerateWithSpeed(dir, dt, maxSpeed);

    public void Decelerate(float dt) =>
        ApplyMovement(Vector2.Zero, dt, maxSpeed, (isGrounded || floatingMode) ? deceleration : airDeceleration);

    public void Move(Vector2 dir, float dt)
    {
        if (dir.IsZeroApprox()) Decelerate(dt);
        else                    Accelerate(dir, dt);
    }

    public void ApplyMovement(Vector2 dir, float dt, float speed, float weight)
    {
        Vector2 desired     = dir.Normalized() * speed;
        Vector2 newVelocity = MathUtil.DeltaLerp(velocity, desired, dt, weight * weightFactor);

        if (!floatingMode)
            newVelocity.Y = velocity.Y;
        velocity = newVelocity;
    }

    #endregion

    #region Jump

    public bool CanJump()         => jumpsUsed < maxJumps || HasCoyote();
    public bool CanJumpBuffered() => CanJump() && HasBufferedJump();

    public bool TryJump()         => CanJump()         && InvokeJump();
    public bool TryJumpBuffered() => CanJumpBuffered() && InvokeJump();

    public void CutJump() => CutJump(0.5f);

    private bool InvokeJump() { Jump(); return true; }

    public void CutJump(float ratio)
    {
        if (isGrounded) return;

        float vertical = velocity.Dot(controller.UpDirection);
        float cut      = vertical * ratio;

        velocity -= controller.UpDirection * cut;
    }

    public void Jump(float height)
    {
        apexHangTimer = 0f;

        float g         = GetJumpGravity();
        float jumpSpeed = ComputeJumpVelocity(g, height);

        float currentVertical = velocity.Dot(controller.UpDirection);
        velocity += controller.UpDirection * (jumpSpeed - currentVertical);

        bool hadCoyote = HasCoyote();

        ConsumeBufferedJump();
        ConsumeCoyote();

        // fellOffEdge already consumed the ground jump slot via ConsumeJump(),
        // so coyote jumps don't increment — they spend the pre-consumed slot.
        if (!hadCoyote)
            jumpsUsed++;
        EmitSignalJumped(jumpsUsed);
    }

    public void Jump() => Jump(jumpHeight);

    public bool HasCoyote()                   => !coyoteTimer.IsReady;
    public void ConsumeCoyote()               => coyoteTimer.Stop();
    public void AcquireCoyote(float duration) => coyoteTimer.Start(duration);
    public void AcquireCoyote()               => coyoteTimer.Start(coyoteTime);

    public bool HasBufferedJump()          => !jumpBufferTimer.IsReady;
    public void ConsumeBufferedJump()      => jumpBufferTimer.Stop();
    public void BufferJump()               => jumpBufferTimer.Start(jumpBufferTime);
    public void BufferJump(float duration) => jumpBufferTimer.Start(duration);

    private static float ComputeJumpVelocity(float g, float h) => Mathf.Sqrt(2f * g * h);

    #endregion

    #region Explosion

    public void ApplyExplosion(Vector2 origin, float force, float radius,
        FalloffCurve falloff = FalloffCurve.Quadratic,
        float upwardBias     = 0.4f)
    {
        Vector2 diff     = controller.GlobalPosition - origin;
        float   distance = diff.Length();

        if (distance > radius) return;

        float   t         = Mathf.Clamp(distance / radius, 0f, 1f);
        float   strength  = ComputeFalloff(t, falloff);
        Vector2 direction = distance > 0.01f ? diff.Normalized() : controller.UpDirection;

        if (upwardBias > 0f)
            direction = (direction + controller.UpDirection * upwardBias).Normalized();

        float verticalSpeed = velocity.Dot(controller.UpDirection);
        if (verticalSpeed < 0f)
            velocity -= controller.UpDirection * verticalSpeed;

        AddImpulse(direction * force * strength);
    }

    public void ApplyExplosion(Vector2 origin, float force) => ApplyExplosion(origin, force, 200f);
    public void ApplyExplosion(Vector2 origin)              => ApplyExplosion(origin, 1200f, 200f);

    private static float ComputeFalloff(float t, FalloffCurve curve) => curve switch
    {
        FalloffCurve.Linear        => 1f - t,
        FalloffCurve.Quadratic     => (1f - t) * (1f - t),
        FalloffCurve.InverseSquare => 1f / (1f + t * t * 10f),
        _                          => 1f - t
    };

    #endregion

    #region Gravity

    private void ApplyGravity(double delta)
    {
        if (isGrounded || !useGravity || floatingMode)
            return;

        float   dt             = (float)delta;
        Vector2 floorDirection = -controller.UpDirection;

        float gravity = GetCurrentGravity();
        gravity = TickApexHang(gravity, dt);

        velocity += gravity * dt * floorDirection;

        float fallSpeed = velocity.Dot(floorDirection);
        if (fallSpeed > maxFallSpeed)
            velocity -= floorDirection * (fallSpeed - maxFallSpeed);
    }

    private float TickApexHang(float gravity, float dt)
    {
        if (apexHangTimer > 0f)
        {
            apexHangTimer -= dt;
            UpdateApexAirControl(apexHorizontalBoost);

            return gravity * apexGravityReduction;
        }

        UpdateApexAirControl();
        return gravity;
    }

    private void UpdateApexAirControl(float boost = 1f)
    {
        airAcceleration = airAccelerationBase * boost;
        airDeceleration = airDecelerationBase * boost;
    }

    #endregion

    #region Settings

    public void SetMotionMode(CharacterBody2D.MotionModeEnum mode)
    {
        var oldMode           = controller.MotionMode;
        controller.MotionMode = mode;
        floatingMode          = mode == CharacterBody2D.MotionModeEnum.Floating;

        if (oldMode != controller.MotionMode)
            EmitSignalMotionModeChanged();
    }

    public void SwitchGravity()
    {
        SetGravityState(gravityState == GravityState.Floor ? GravityState.Ceiling : GravityState.Floor);
    }

    public void SetGravityState(GravityState state)
    {
        controller.UpDirection = state switch
        {
            GravityState.Floor   => Vector2.Up,
            GravityState.Ceiling => Vector2.Down,
            _                    => throw new ArgumentOutOfRangeException(
                                    nameof(state), state, "Invalid GravityState value.")
        };

        var oldState = gravityState;
        gravityState = state;

        if (oldState != gravityState)
            EmitSignalGravitySwitched();
    }

    #endregion

    #region Utilities

    public void SetMass(float value) => mass = Mathf.Max(0.01f, value);

    public void AddForce(Vector2 force, float dt) => velocity += force   / mass * dt;
    public void AddImpulse(Vector2 impulse) => velocity += impulse / mass;

    public void SetVelocity(Vector2 value) => velocity   = value;
    public void SetVelocityX(float value) => velocity.X = value;
    public void SetVelocityY(float value) => velocity.Y = value;

    public void ResetJumps() => jumpsUsed = 0;
    public void SetMaxJumps(int value) => maxJumps  = Mathf.Max(1, value);
    public void AddJump(int count) => jumpsUsed = Mathf.Max(0, jumpsUsed - count);
    public void AddJump() => AddJump(1);

    public void ConsumeJump(int count) => jumpsUsed = Mathf.Min(maxJumps, jumpsUsed + count);
    public void ConsumeJump() => ConsumeJump(1);

    public void EnableGravity() => useGravity = true;
    public void DisableGravity() => useGravity = false;

    public bool IsGrounded() => isGrounded;
    public bool IsFalling() => isFalling;
    public bool IsRising() => !isGrounded && velocity.Dot(controller.UpDirection) > FALL_THRESHOLD;

    public bool IsGravityActive() => useGravity;
    public bool IsOnWall() => controller.IsOnWall();
    public bool IsOnCeiling() => controller.IsOnCeiling();

    public void SetFallGravityMultiplier(float value) => fallGravityMultiplier = Mathf.Max(0, value);
    public void SetMaxFallSpeed(float value) => maxFallSpeed = Mathf.Max(0, value);
    public void SetWeightFactor(float value) => weightFactor = Mathf.Max(0f, value);

    public float GetJumpGravity() => DEFAULT_GRAVITY * gravityScale;
    public float GetFallGravity() => DEFAULT_GRAVITY * gravityScale * fallGravityMultiplier;

    public float GetCurrentGravity() => isFalling ? GetFallGravity() : GetJumpGravity();

    #endregion


}

