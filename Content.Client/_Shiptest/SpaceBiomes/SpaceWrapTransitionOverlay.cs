using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Shiptest.SpaceBiomes;

/// <summary>
/// Placeholder for optional visuals when crossing the space map edge / biome wrap (no-op until implemented).
/// </summary>
public sealed class SpaceWrapTransitionOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    protected override bool BeforeDraw(in OverlayDrawArgs args) => false;

    protected override void Draw(in OverlayDrawArgs args)
    {
    }
}
