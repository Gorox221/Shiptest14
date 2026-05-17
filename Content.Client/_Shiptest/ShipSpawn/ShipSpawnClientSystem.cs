using System.Collections.Generic;
using Content.Shared._Shiptest.ShipSpawn;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._Shiptest.ShipSpawn;

public sealed class ShipSpawnClientSystem : EntitySystem
{
    private readonly HashSet<string> _consumedBlueprints = new();
    private readonly Dictionary<NetEntity, bool> _lockedStations = new();
    private Action? _afterAvailabilityReceived;

    public IReadOnlyCollection<string> ConsumedBlueprints => _consumedBlueprints;
    public IReadOnlyDictionary<NetEntity, bool> LockedStations => _lockedStations;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayerShipConsumedBlueprintsSyncEvent>(OnConsumedSync);
        SubscribeNetworkEvent<PlayerShipJoinLockStatusEvent>(OnLockStatusSync);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _consumedBlueprints.Clear();
        _lockedStations.Clear();
        _afterAvailabilityReceived = null;
    }

    private void OnLockStatusSync(PlayerShipJoinLockStatusEvent msg)
    {
        _lockedStations.Clear();
        foreach (var (station, isLocked) in msg.LockedStations)
        {
            _lockedStations[station] = isLocked;
        }
    }

    private void OnConsumedSync(PlayerShipConsumedBlueprintsSyncEvent msg)
    {
        _consumedBlueprints.Clear();
        foreach (var id in msg.ConsumedBlueprintIds)
            _consumedBlueprints.Add(id);

        if (msg.RespondedToRequest)
        {
            _afterAvailabilityReceived?.Invoke();
            _afterAvailabilityReceived = null;
        }
    }

    /// <summary>
    /// Ask the server for the current list of already-spawned ship blueprints, then run <paramref name="continuation"/>.
    /// </summary>
    public void RequestAvailabilityAndThen(Action continuation)
    {
        _afterAvailabilityReceived = continuation;
        RaiseNetworkEvent(new RequestPlayerShipSpawnAvailabilityEvent());
    }

    public void RequestSpawn(
        ProtoId<PlayerShipFactionPrototype> faction,
        ProtoId<PlayerShipBlueprintPrototype> blueprint,
        bool closedShip = false,
        string? password = null)
    {
        RaiseNetworkEvent(new RequestPlayerShipSpawnEvent(faction, blueprint, closedShip, password));
    }

    public void RequestJoinWithPassword(string jobId, NetEntity station, string? password = null)
    {
        RaiseNetworkEvent(new ShipJoinPasswordRequestEvent(jobId, station, password));
    }
}
