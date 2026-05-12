using System.Numerics;
using Robust.Shared.Maths;

namespace Content.Shared._Shiptest.SpaceBiomes;

/// <summary>
/// World-space bounds aligned with the 19×1500 m space biome grid (same anchor as <see cref="SpaceBiomeGridSystem"/>).
/// </summary>
public static class SpaceMapWorldBounds
{
    /// <summary>
    /// Synthetic biome id for shuttle map / radar rendering only.
    /// </summary>
    public const string EdgeBiomeId = "MapWorldEdge";

    /// <summary>
    /// Depth (meters) of the impassable void fill drawn on shuttle maps outside the playable interior.
    /// </summary>
    public const float OutsideMapFillExtent = 120_000f;

    /// <summary>
    /// Inclusive axis-aligned box of the biome grid interior (playable space inside the collision shell).
    /// </summary>
    public static Box2 GetInteriorBounds(Vector2 anchor)
    {
        var halfGrid = SpaceBiomeGridCellComponent.GridSize / 2;
        var cell = SpaceBiomeGridCellComponent.CellSize;
        var bottomLeft = anchor + new Vector2(-halfGrid * cell, -halfGrid * cell);
        var topRight = anchor + new Vector2((halfGrid + 1) * cell, (halfGrid + 1) * cell);
        return new Box2(bottomLeft, topRight);
    }
}
