using System;
using Godot;

namespace Utilities;

public static class MathUtil
{
    private static RandomNumberGenerator RNG { get; } = new();

    static MathUtil()
    {
        RNG.Randomize();
    }

    public static float DeltaLerp(float from, float to, float dt, float weight)
    {
        float smoothing = 1f - Mathf.Exp(-weight * dt);
        return Mathf.Lerp(from, to, smoothing);
    }

    public static Vector2 DeltaLerp(Vector2 from, Vector2 to, float dt, float weight)
    {
        float smoothing = 1f - Mathf.Exp(-weight * dt);
        return from.Lerp(to, smoothing);
    }

    public static Vector2 RandomUnit()
    {
        float angle = RNG.Randf() * Mathf.Tau;
        return Vector2.FromAngle(angle);
    }

    public static Vector2 RandomInRect(Rect2 rect)
    {
        return new Vector2(
            RNG.RandfRange(rect.Position.X, rect.End.X),
            RNG.RandfRange(rect.Position.Y, rect.End.Y)
        );
    }

    public static Vector2 RandomOnCircle(float radius)
    {
        return RandomUnit() * radius;
    }

    public static Vector2 RandomInCircle(float radius)
    {
        float r = Mathf.Sqrt(RNG.Randf()) * radius;
        return RandomUnit() * r;
    }

    public static Vector2[] PointsOnCircle(float radius, int count)
    {
        var points = new Vector2[count];
        float step = Mathf.Tau / count;

        for (int i = 0; i < count; i++)
            points[i] = Vector2.FromAngle(i * step) * radius;
        return points;
    }

    public static Vector2[] PointsOnCircleAdaptive(float radius, float pointsPerUnit = 1f)
    {
        int count = Mathf.Max(8, Mathf.RoundToInt(Mathf.Tau * pointsPerUnit * radius));
        return PointsOnCircle(radius, count);
    }

    public static float Clamp01(float value) => Mathf.Clamp(value, 0f, 1f);
    public static double Clamp01(double value) => Mathf.Clamp(value, 0.0, 1.0);

    public static bool CoinFlip() => (int)RNG.Randi() % 2 == 0;

    public static float RandfRange(float min, float max) => RNG.RandfRange(min, max);
    public static int RandRange(int min, int max) => RNG.RandiRange(min, max);
    public static bool Chance(float probability) => RNG.Randf() < probability;

    public static T PickRandom<T>(params T[] items)
    {
        if (items.Length == 0)
            throw new ArgumentException("Array is empty");
        return items[RNG.RandiRange(0, items.Length - 1)];
    }

    public static float Normalize(float value, float length)
    {
        if (length <= 0f) return 1f;
        return Clamp01(value / length);
    }

    public static double Normalize(double value, double length)
    {
        if (length <= 0.0) return 1.0;
        return Clamp01(value / length);
    }

    public static Vector2 OffscreenSpawn(Camera2D cam, float margin)
    {
        return RandomOutsideRectPoint(cam.GetViewportRect(), margin);
    }

    public static Vector2 RandomOutsideRectPoint(Rect2 rect, float margin)
    {
        Rect2 expanded = rect.Grow(margin);
        int side = (int)RNG.Randi() % 4;

        return side switch
        {
            0 => new Vector2(
                    expanded.Position.X,
                    RNG.RandfRange(expanded.Position.Y, expanded.End.Y)
                ),

            1 => new Vector2(
                    expanded.End.X,
                    RNG.RandfRange(expanded.Position.Y, expanded.End.Y)
                ),

            2 => new Vector2(
                    RNG.RandfRange(expanded.Position.X, expanded.End.X),
                    expanded.Position.Y
                ),

            _ => new Vector2(
                    RNG.RandfRange(expanded.Position.X, expanded.End.X),
                    expanded.End.Y
                ),
        };
    }
}

