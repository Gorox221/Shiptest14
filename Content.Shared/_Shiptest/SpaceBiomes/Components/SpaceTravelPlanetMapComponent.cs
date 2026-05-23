using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Shiptest.SpaceBiomes.Components;

/// <summary>
/// Runtime data for a BSS travel planet map (arrival point and playable boundary).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpaceTravelPlanetMapComponent : Component
{
    /// <summary>
    /// Fixed shuttle arrival position on this planet map (local map coordinates).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 ArrivalCenter;

    /// <summary>
    /// Playable radius around <see cref="ArrivalCenter"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BoundaryRange = 700f;
}
