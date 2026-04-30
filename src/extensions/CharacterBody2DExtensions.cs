using Godot;

namespace Utilities;

public static class CharacterBody2DExtensions
{
    private static readonly float DefaultGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public static void ApplyGravity(this CharacterBody2D controller, float gravity, float dt, float maxFallSpeed = 1000f)
    {
        if (controller.IsOnFloor())
            return;

        Vector2 floorDirection = -controller.UpDirection;
        controller.Velocity += floorDirection * gravity * dt;

        float speed = controller.Velocity.Dot(floorDirection);

        if (speed > maxFallSpeed)
            controller.Velocity -= floorDirection * (speed - maxFallSpeed);
    }

    public static void Jump(this CharacterBody2D controller, float heightInPixels, float gravity)
    {
        float y = controller.UpDirection.Y;
        float jumpForce = Mathf.Sqrt(2f * Mathf.Abs(gravity) * heightInPixels);

        controller.Velocity = controller.Velocity with { Y = y * jumpForce };
    }

    public static void Jump(this CharacterBody2D controller, float heightInPixels) => 
        controller.Jump(heightInPixels, DefaultGravity);

    public static void ApplyKnockbackFrom(this CharacterBody2D controller, Vector2 sourcePosition, float force)
    {
        Vector2 dir = sourcePosition.DirectionTo(controller.GlobalPosition);
        controller.Velocity += force * dir;
    }

    public static void ApplyKnockback(this CharacterBody2D controller, Vector2 direction, float force)
    {
        Vector2 dir = direction.Normalized();
        controller.Velocity += force * dir;
    }
}

