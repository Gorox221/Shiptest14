using System.Numerics;
using Content.Shared._Shiptest.SpaceBiomes;
using Content.Shared.GameTicking;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Shiptest.SpaceBiomes;

/// <summary>
/// Spawns kinematic colliders on the biome grid edge. Uses <see cref="BodyType.Kinematic"/> (not Static) so
/// they still collide with static map grids — the engine skips Static↔Static contacts.
/// Also periodically clamps map grids fully inside the playable AABB (FTL / placement edge cases).
/// </summary>
public sealed class SpaceMapBoundarySystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private MapId _boundsMapId;
    private Vector2 _boundsAnchor;
    private bool _boundsActive;
    private float _clampAccumulator;
    private const float ClampInterval = 0.25f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent _)
    {
        ClearSegments();
    }

    private void ClearSegments()
    {
        var query = AllEntityQuery<SpaceMapBoundarySegmentComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }

        _boundsActive = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_boundsActive)
            return;

        _clampAccumulator += frameTime;
        if (_clampAccumulator < ClampInterval)
            return;

        _clampAccumulator = 0f;
        ClampGridsInsideBounds();
    }

    /// <summary>
    /// Recreates boundary walls for the given map and biome anchor (station center at grid init).
    /// </summary>
    public void CreateBoundaries(MapId mapId, Vector2 anchor)
    {
        ClearSegments();

        _boundsMapId = mapId;
        _boundsAnchor = anchor;
        _boundsActive = true;

        var bounds = SpaceMapWorldBounds.GetInteriorBounds(anchor);
        const float thickness = 384f;
        var halfT = thickness / 2f;
        var midX = (bounds.Left + bounds.Right) * 0.5f;
        var midY = (bounds.Bottom + bounds.Top) * 0.5f;
        var extX = bounds.Width * 0.5f + halfT;
        var extY = bounds.Height * 0.5f + halfT;

        SpawnWall(mapId, new Vector2(bounds.Left - halfT, midY), halfT, extY);
        SpawnWall(mapId, new Vector2(bounds.Right + halfT, midY), halfT, extY);
        SpawnWall(mapId, new Vector2(midX, bounds.Bottom - halfT), extX, halfT);
        SpawnWall(mapId, new Vector2(midX, bounds.Top + halfT), extX, halfT);
    }

    private void SpawnWall(MapId mapId, Vector2 worldCenter, float halfWidth, float halfHeight)
    {
        var uid = Spawn(null, new MapCoordinates(worldCenter, mapId));
        var physics = AddComp<PhysicsComponent>(uid);
        // Kinematic: still blocks static grids (Static↔Static is ignored by the physics engine).
        _physics.SetBodyType(uid, BodyType.Kinematic, body: physics);
        _physics.SetFixedRotation(uid, true, body: physics);

        var shape = new PolygonShape();
        shape.SetAsBox(halfWidth, halfHeight);

        _fixtures.TryCreateFixture(
            uid,
            shape,
            "map-edge",
            density: 1f,
            hard: true,
            collisionLayer: (int) CollisionGroup.WallLayer,
            collisionMask: (int) CollisionGroup.AllMask,
            body: physics);

        _physics.SetCanCollide(uid, true, body: physics);
        _physics.WakeBody(uid, body: physics);
        EnsureComp<SpaceMapBoundarySegmentComponent>(uid);
    }

    private void ClampGridsInsideBounds()
    {
        var interior = SpaceMapWorldBounds.GetInteriorBounds(_boundsAnchor);
        const float inset = 32f;
        var inner = new Box2(
            interior.Left + inset,
            interior.Bottom + inset,
            interior.Right - inset,
            interior.Top - inset);

        if (inner.Width <= 0 || inner.Height <= 0)
            return;

        var query = AllEntityQuery<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var grid, out var xform))
        {
            if (xform.MapID != _boundsMapId)
                continue;

            var worldAabb = _transform.GetWorldMatrix(xform).TransformBox(grid.LocalAABB);
            var halfW = worldAabb.Width * 0.5f;
            var halfH = worldAabb.Height * 0.5f;

            var minCx = inner.Left + halfW;
            var maxCx = inner.Right - halfW;
            var minCy = inner.Bottom + halfH;
            var maxCy = inner.Top - halfH;

            if (minCx > maxCx || minCy > maxCy)
                continue;

            var c = worldAabb.Center;
            var nx = Math.Clamp(c.X, minCx, maxCx);
            var ny = Math.Clamp(c.Y, minCy, maxCy);
            var delta = new Vector2(nx, ny) - c;
            if (delta.LengthSquared() < 0.01f)
                continue;

            var worldPos = _transform.GetWorldPosition(xform) + delta;
            _transform.SetWorldPosition((uid, xform), worldPos);

            if (TryComp(uid, out PhysicsComponent? phys))
                _physics.WakeBody(uid, body: phys);
        }
    }
}
