using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Shiptest.Shuttles;

/// <summary>
/// Linear thruster fed by piping a configurable fuel gas instead of APC power; contributes extra shuttle max speed along its thrust axis while enabled.
/// </summary>
[RegisterComponent]
public sealed partial class PlasmaThrusterComponent : Component
{
    /// <summary>Gas prototype id matches <see cref="Gas"/> enum index (e.g. <c>"3"</c> for plasma).</summary>
    [DataField]
    public ProtoId<GasPrototype> FuelGas = new("3");

    [DataField("gasMixture")]
    public GasMixture Air = new(50f);

    [DataField]
    public string PortName = "pipe";

    /// <summary>Added to shuttle max linear velocity along this thruster's cardinal axis while the thruster is enabled.</summary>
    [DataField]
    public float MaxVelocityBonus = 5f;

    /// <summary>Fuel gas consumed per second while the thruster is actively firing.</summary>
    [DataField]
    public float FuelMolesPerSecondFiring = 3f;

    /// <summary>Thruster shuts off when internal fuel drops below this (after mixing with pipes).</summary>
    [DataField]
    public float MinimumFuelMoles = 0.05f;

    /// <summary>Denominator for helm UI fuel percentage (moles at 100%).</summary>
    [DataField]
    public float MaxFuelMolesForDisplay = 100f;
}
