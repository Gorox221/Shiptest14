using System.Collections.Generic;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Shiptest.ShipSpawn;

/// <summary>
/// Broadcasts station lock state for client late-join UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayerShipJoinLockStatusEvent : EntityEventArgs
{
    public Dictionary<NetEntity, bool> LockedStations { get; }

    public PlayerShipJoinLockStatusEvent(Dictionary<NetEntity, bool> lockedStations)
    {
        LockedStations = lockedStations;
    }
}
