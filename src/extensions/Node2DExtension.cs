using Godot;

namespace Utilities;

public static class Node2DExtensions
{
    public static void SmoothlyLookAt(this Node2D node, Vector2 target, float acceleration, float dt)
    {
        float rad = (target - node.GlobalPosition).Angle();
        node.Rotation = Mathf.LerpAngle(node.Rotation, rad, acceleration * dt);
    }

    public static void SmoothlyFollow(this Node2D node, Vector2 target, float acceleration, float dt)
    {
        node.GlobalPosition = MathUtil.DeltaLerp(node.GlobalPosition, target, dt, acceleration);
    }

    public static Vector2 GetMouseDirection(this Node2D node)
    {
        Vector2 mousePos = node.GetGlobalMousePosition();
        return node.GlobalPosition.DirectionTo(mousePos);
    }
}

