using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Shiptest.GameTicking;

/// <summary>
/// Handles a short preparation phase at round start.
/// </summary>
public sealed class RoundPreparationSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private EntityQuery<MapGridComponent> _gridQuery;

    private static readonly TimeSpan PreparationDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RadiationTickDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BoltRefreshDelay = TimeSpan.FromSeconds(2);
    private static readonly DamageSpecifier SpaceRadiationDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Radiation", 2 }
        }
    };

    private readonly HashSet<EntityUid> _forcedBolts = new();

    private bool _isPreparationActive;
    private TimeSpan _preparationEndsAt = TimeSpan.Zero;
    private TimeSpan _nextRadiationTick = TimeSpan.Zero;
    private TimeSpan _nextBoltRefreshTick = TimeSpan.Zero;

    public bool IsPreparationActive => _isPreparationActive;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<DockEvent>(OnDockEvent);
        SubscribeLocalEvent<UndockEvent>(OnUndockEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_isPreparationActive)
            return;

        var now = _timing.CurTime;
        if (now >= _preparationEndsAt)
        {
            DisablePreparation(notify: true);
            return;
        }

        if (now >= _nextRadiationTick)
        {
            _nextRadiationTick = now + RadiationTickDelay;
            ApplySpaceRadiation();
        }

        if (now < _nextBoltRefreshTick)
            return;

        _nextBoltRefreshTick = now + BoltRefreshDelay;
        ForceAllDockBolts();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        EnablePreparation();
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        EnablePreparation();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        DisablePreparation(notify: false);
    }

    private void OnDockEvent(DockEvent ev)
    {
        if (!_isPreparationActive)
            return;

        ForceDockBolt(ev.DockA.Owner);
        ForceDockBolt(ev.DockB.Owner);
    }

    private void OnUndockEvent(UndockEvent ev)
    {
        if (!_isPreparationActive)
            return;

        ForceDockBolt(ev.DockA.Owner);
        ForceDockBolt(ev.DockB.Owner);
    }

    private void EnablePreparation()
    {
        if (_isPreparationActive)
            return;

        _isPreparationActive = true;
        _preparationEndsAt = _timing.CurTime + PreparationDuration;
        _nextRadiationTick = _timing.CurTime;
        _nextBoltRefreshTick = _timing.CurTime;
        ForceAllDockBolts();
    }

    private void DisablePreparation(bool notify)
    {
        if (!_isPreparationActive && _forcedBolts.Count == 0)
            return;

        _isPreparationActive = false;
        _preparationEndsAt = TimeSpan.Zero;
        _nextRadiationTick = TimeSpan.Zero;
        _nextBoltRefreshTick = TimeSpan.Zero;

        foreach (var uid in _forcedBolts)
        {
            if (Deleted(uid) || !TryComp<DoorBoltComponent>(uid, out var bolt))
                continue;

            _doorSystem.SetBoltsDown((uid, bolt), false);
        }

        _forcedBolts.Clear();

        if (notify)
            _chat.DispatchGlobalAnnouncement(Loc.GetString("round-preparation-calibration-announcement"));
    }

    private void ForceAllDockBolts()
    {
        var query = EntityQueryEnumerator<DockingComponent, DoorBoltComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            ForceDockBolt(uid);
        }
    }

    private void ForceDockBolt(EntityUid uid)
    {
        if (!TryComp<DoorBoltComponent>(uid, out var bolt))
            return;

        if (!bolt.BoltsDown)
            _forcedBolts.Add(uid);

        _doorSystem.TryClose(uid);
        _doorSystem.SetBoltsDown((uid, bolt), true);
    }

    private void ApplySpaceRadiation()
    {
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mobState, out var xform))
        {
            if (mobState.CurrentState == MobState.Dead || xform.MapID == MapId.Nullspace)
                continue;

            var inSpace = false;
            if (xform.GridUid is { } gridUid && _gridQuery.TryComp(gridUid, out var grid))
            {
                var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
                inSpace = _turf.IsSpace(tile);
            }
            else if (_turf.TryGetTileRef(xform.Coordinates, out var turfTile))
            {
                inSpace = _turf.IsSpace(turfTile.Value);
            }
            else
            {
                // No grid and no tile at coordinates on a valid map means open space.
                inSpace = true;
            }

            if (!inSpace)
                continue;

            _damageable.TryChangeDamage(uid, SpaceRadiationDamage, interruptsDoAfters: false);
        }
    }
}
