using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Crescent.ShipShields;

/// <summary>
/// Atmos machine that consumes a configurable fuel gas from internal storage to repair shield emitter stress while forcing the shield offline.
/// </summary>
[RegisterComponent]
public sealed partial class ShieldGasRegeneratorComponent : Component
{
    /// <summary>Gas prototype id matches <see cref="Gas"/> enum index (e.g. "3" for plasma).</summary>
    [DataField]
    public ProtoId<GasPrototype> FuelGas = new("3");

    [DataField("gasMixture")]
    public GasMixture Air = new(1000f);

    [DataField]
    public string PortName = "pipe";

    [DataField]
    public float ActivationDoAfter = 0.25f;

    /// <summary>Moles of fuel gas consumed per second while active.</summary>
    [DataField]
    public float FuelMolesPerSecond = 25.6f;

    /// <summary>Shield stress removed per mole of fuel consumed.</summary>
    [DataField]
    public float ShieldHpPerFuelMole = DefaultHpPerMoleRatio;

    [DataField]
    public float MinOverloadLock = 2f;

    /// <summary>5000 shield HP per 1024 moles — legacy tuning preserved as default ratio.</summary>
    public const float DefaultHpPerMoleRatio = 5000f / 1024f;

    [ViewVariables]
    public bool Active;
}
