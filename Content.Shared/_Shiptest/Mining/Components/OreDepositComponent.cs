using Robust.Shared.Prototypes;

namespace Content.Shared._Shiptest.Mining.Components;

/// <summary>
/// Marks an entity as a valid drill deposit and defines what ore it can produce.
/// </summary>
[RegisterComponent]
public sealed partial class OreDepositComponent : Component
{
    /// <summary>
    /// Ore prototypes that can be spawned by drills on this deposit.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Ores = new();

    /// <summary>
    /// Minimum ore entities per drill cycle.
    /// </summary>
    [DataField]
    public int MinYield = 1;

    /// <summary>
    /// Maximum ore entities per drill cycle.
    /// </summary>
    [DataField]
    public int MaxYield = 2;
}

