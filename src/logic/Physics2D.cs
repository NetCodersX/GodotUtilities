using Godot;
using Godot.Collections;
using Utilities.Pooling;

namespace Utilities.Logic;

public partial class Physics2D : Node
{
    private const uint ALL_LAYERS = uint.MaxValue;
    private const int INITIAL_POOL_SIZE = 5;

    private static Physics2D instance;

    private PhysicsDirectSpaceState2D spaceState;

    private ObjectPool<PhysicsRayQueryParameters2D> queryRayPool;
    private ObjectPool<PhysicsShapeQueryParameters2D> queryShapePool;
    private ObjectPool<CircleShape2D> sphereShapePool;

    private Timer timer;

    public override void _EnterTree() => instance = this;

    public override void _Ready()
    {
        spaceState = GetViewport().World2D.DirectSpaceState;

        queryRayPool = new(() => new(), INITIAL_POOL_SIZE);
        queryShapePool = new(() => new(), INITIAL_POOL_SIZE);
        sphereShapePool = new(() => new(), INITIAL_POOL_SIZE);

        timer = new() { WaitTime = 8.0 }; AddChild(timer);
        timer.Timeout += OnTimerTimeout;
    }
    
    private void OnTimerTimeout()
    {
        queryRayPool.Trim(INITIAL_POOL_SIZE);
        queryShapePool.Trim(INITIAL_POOL_SIZE);
        sphereShapePool.Trim(INITIAL_POOL_SIZE);
    }

    #region Raycast

    public static bool Raycast(Vector2 origin, Vector2 direction, float distance)
    {
        return Raycast(origin, direction, distance, out var _);
    }

    public static bool Raycast(Vector2 origin, Vector2 direction, float distance, out RaycastHit hit)
    {
        return Raycast(origin, direction, distance, out hit, ALL_LAYERS);
    }

    public static bool Raycast(Vector2 origin, Vector2 direction, float distance, out RaycastHit hit, uint collisionMask)
    {
        return Raycast(origin, origin + direction.Normalized() * distance, out hit, collisionMask);
    }

    public static bool Raycast(Vector2 from, Vector2 to)
    {
        return Raycast(from, to, out var _);
    }

    public static bool Raycast(Vector2 from, Vector2 to, out RaycastHit hit)
    {
        return Raycast(from, to, out hit, ALL_LAYERS);
    }

    public static bool Raycast(Vector2 from, Vector2 to, out RaycastHit hit, uint collisionMask)
    {
        var query = instance.queryRayPool.Get();

        query.From = from;
        query.To = to;

        query.CollisionMask = collisionMask;

        var result = instance.spaceState.IntersectRay(query);
        instance.queryRayPool.Release(query);

        if (result.Count <= 0)
        {
            hit = default;
            return false;
        }
        
        hit = new RaycastHit
        {
            Normal = result["normal"].AsVector2(),
            Position = result["position"].AsVector2(),
            Collider = result["collider"].AsGodotObject(),
            ColliderRid = result["rid"].AsRid()
        };

        return true;
    }

    #endregion

    #region Check Sphere

    public static bool CheckSphere(Vector2 position, float radius)
    {
        return CheckSphere(position, radius, out _, ALL_LAYERS);
    }

    public static bool CheckSphere(Vector2 position, float radius, uint collisionMask)
    {
        return CheckSphere(position, radius, out _, collisionMask);
    }

    public static bool CheckSphere(Vector2 position, float radius, out GodotObject collider)
    {
        return CheckSphere(position, radius, out collider, ALL_LAYERS);
    }

    public static bool CheckSphere(Vector2 position, float radius, out GodotObject collider, uint collisionMask)
    {
        var overlaps = IntersectSphere(position, radius, collisionMask, 1);

        if (overlaps.Count > 0)
        {
            collider = overlaps[0]["collider"].AsGodotObject();
            return true;
        }

        collider = null;
        return false;
    }

    #endregion

    #region Overlap Sphere

    public static GodotObject[] OverlapSphere(Vector2 position, float radius)
    {
        return OverlapSphere(position, radius, ALL_LAYERS);
    }

    public static GodotObject[] OverlapSphere(Vector2 position, float radius, uint collisionMask)
    {
        return OverlapSphere(position, radius, collisionMask, 16);
    }

    public static GodotObject[] OverlapSphere(Vector2 position, float radius, int maxResults)
    {
        return OverlapSphere(position, radius, ALL_LAYERS, maxResults);
    }

    public static GodotObject[] OverlapSphere(Vector2 position, float radius, uint collisionMask, int maxResults)
    {
        var overlaps = IntersectSphere(position, radius, collisionMask, maxResults);
        var result = new GodotObject[overlaps.Count];
        
        for (int i = 0; i < overlaps.Count; i++)
            result[i] = overlaps[i]["collider"].AsGodotObject();
        return result;
    }

    #endregion

    private static Array<Dictionary> IntersectSphere(Vector2 position, float radius, uint mask, int maxResults)
    {
        var query = instance.queryShapePool.Get();
        var shape = instance.sphereShapePool.Get();
        
        query.Shape = shape;
        shape.Radius = radius;
        query.Transform = new Transform2D(0f, position);
        query.CollisionMask = mask;

        var overlaps = instance.spaceState.IntersectShape(query, maxResults);
        instance.queryShapePool.Release(query);
        instance.sphereShapePool.Release(shape);

        return overlaps;
    }
}

public readonly struct RaycastHit
{
    public Vector2 Position     { get; init; }
    public Vector2 Normal       { get; init; }
    public GodotObject Collider { get; init; }
    public Rid ColliderRid      { get; init; }

    public readonly T GetCollider<T>() where T : Node2D => Collider as T;
}

