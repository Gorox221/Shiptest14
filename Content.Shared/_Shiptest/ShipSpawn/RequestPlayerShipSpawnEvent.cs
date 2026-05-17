using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Shiptest.ShipSpawn;

/// <summary>
/// Client requests spawning a player ship from lobby after round start.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestPlayerShipSpawnEvent : EntityEventArgs
{
    public ProtoId<PlayerShipFactionPrototype> Faction { get; }
    public ProtoId<PlayerShipBlueprintPrototype> Blueprint { get; }
    public bool ClosedShip { get; }
    public string? Password { get; }

    public RequestPlayerShipSpawnEvent(
        ProtoId<PlayerShipFactionPrototype> faction,
        ProtoId<PlayerShipBlueprintPrototype> blueprint,
        bool closedShip = false,
        string? password = null)
    {
        Faction = faction;
        Blueprint = blueprint;
        ClosedShip = closedShip;
        Password = password;
    }
}
