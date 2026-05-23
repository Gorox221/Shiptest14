namespace Content.Shared._Shiptest.Mining.Components;

/// <summary>
/// Foldable drill that mines ore from nearby deposits when powered.
/// </summary>
[RegisterComponent]
public sealed partial class OreDrillComponent : Component
{
    [DataField]
    public float SpawnInterval = 12f;

    [DataField]
    public float PowerPerCycle = 250f;

    [DataField]
    public float ChargePerSecond = 150f;

    [DataField]
    public float DepositRange = 0.8f;

    [DataField]
    public bool Active;

    [ViewVariables]
    public float Accumulator;
}

