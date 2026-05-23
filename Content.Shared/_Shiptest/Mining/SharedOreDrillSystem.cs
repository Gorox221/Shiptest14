using Content.Shared._Shiptest.Mining.Components;
using Content.Shared.Foldable;

namespace Content.Shared._Shiptest.Mining;

/// <summary>
/// Shared drill rules (folding while anchored must be blocked on client and server).
/// </summary>
public sealed class SharedOreDrillSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreDrillComponent, FoldAttemptEvent>(OnFoldAttempt);
    }

    private void OnFoldAttempt(Entity<OreDrillComponent> ent, ref FoldAttemptEvent args)
    {
        if (!Transform(ent).Anchored)
            return;

        // Unfolding while anchored is allowed; folding is not.
        if (args.Comp.IsFolded)
            return;

        args.Cancelled = true;
    }
}
