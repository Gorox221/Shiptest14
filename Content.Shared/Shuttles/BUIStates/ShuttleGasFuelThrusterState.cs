using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// One gas-fuel thruster entry for shuttle helm NAV UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleGasFuelThrusterState
{
    public NetEntity Thruster;
    public string Label = string.Empty;
    /// <summary>Fuel fill 0–100 for UI.</summary>
    public byte FillPercent;
}
