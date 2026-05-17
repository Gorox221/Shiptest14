using Robust.Shared.GameStates;

namespace Content.Shared._Shiptest.SpaceBiomes.Components;

/// <summary>
/// Marks a regular FTL beacon as a space-travel planet target.
/// Stores nearby-arrival search settings so shuttles can land near occupied planets.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpaceTravelPlanetBeaconComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid DestinationMap = EntityUid.Invalid;

    [DataField, AutoNetworkedField]
    public float ArrivalMinOffset = 96f;

    [DataField, AutoNetworkedField]
    public float ArrivalSearchRadius = 384f;

    [DataField, AutoNetworkedField]
    public float ArrivalSearchStep = 32f;
}
