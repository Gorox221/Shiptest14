using Content.Server.PowerCell;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Shiptest.Mining.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Shiptest.Mining;

/// <summary>
/// Handles drill activation and periodic ore spawning from nearby deposits.
/// </summary>
public sealed class OreDrillSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreDrillComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<OreDrillComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<OreDrillComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnInteractHand(Entity<OreDrillComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // Unanchored drills are picked up / folded via other interactions.
        if (!Transform(ent).Anchored)
            return;

        if (TryComp<FoldableComponent>(ent, out var foldable) && foldable.IsFolded)
        {
            _popup.PopupEntity(Loc.GetString("ore-drill-popup-unfold-first"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!TryGetDeposit(ent, out _))
        {
            _popup.PopupEntity(Loc.GetString("ore-drill-popup-no-deposit"), ent, args.User);
            args.Handled = true;
            return;
        }

        ent.Comp.Active = !ent.Comp.Active;
        ent.Comp.Accumulator = 0f;

        _popup.PopupEntity(
            Loc.GetString(ent.Comp.Active ? "ore-drill-popup-turned-on" : "ore-drill-popup-turned-off"),
            ent,
            args.User);

        args.Handled = true;
    }

    private void OnAnchorAttempt(Entity<OreDrillComponent> ent, ref AnchorAttemptEvent args)
    {
        if (Transform(ent).Anchored)
            return;

        if (TryGetDeposit(ent, out _))
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("ore-drill-popup-anchor-on-deposit"), ent, args.User);
    }

    private void OnAnchorStateChanged(Entity<OreDrillComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        ent.Comp.Active = false;
        ent.Comp.Accumulator = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OreDrillComponent>();
        while (query.MoveNext(out var uid, out var drill))
        {
            TryChargeFromWire(uid, drill, frameTime);

            if (!drill.Active)
                continue;

            if (!Transform(uid).Anchored)
            {
                drill.Active = false;
                drill.Accumulator = 0f;
                continue;
            }

            if (TryComp<FoldableComponent>(uid, out var foldable) && foldable.IsFolded)
            {
                drill.Active = false;
                drill.Accumulator = 0f;
                continue;
            }

            if (!TryGetDeposit((uid, drill), out var deposit))
            {
                drill.Active = false;
                drill.Accumulator = 0f;
                continue;
            }

            drill.Accumulator += frameTime;
            if (drill.Accumulator < drill.SpawnInterval)
                continue;

            drill.Accumulator -= drill.SpawnInterval;

            if (!_powerCell.TryUseCharge(uid, drill.PowerPerCycle))
            {
                drill.Active = false;
                continue;
            }

            if (deposit.Comp.Ores.Count == 0)
                continue;

            var amount = _random.Next(Math.Max(1, deposit.Comp.MinYield), Math.Max(deposit.Comp.MinYield, deposit.Comp.MaxYield) + 1);
            var coords = Transform(uid).Coordinates;
            for (var i = 0; i < amount; i++)
            {
                var oreProto = _random.Pick(deposit.Comp.Ores);
                Spawn(oreProto, coords);
            }
        }
    }

    private bool TryGetDeposit(Entity<OreDrillComponent> drill, out Entity<OreDepositComponent> deposit)
    {
        var coords = Transform(drill).Coordinates;
        foreach (var uid in _lookup.GetEntitiesInRange(coords, drill.Comp.DepositRange))
        {
            if (!TryComp<OreDepositComponent>(uid, out var oreDeposit))
                continue;

            deposit = (uid, oreDeposit);
            return true;
        }

        deposit = default;
        return false;
    }

    private void TryChargeFromWire(EntityUid uid, OreDrillComponent drill, float frameTime)
    {
        if (!Transform(uid).Anchored)
            return;

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            return;

        if (!receiver.Powered)
            return;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out var batteryUid, out var battery))
            return;

        _battery.AddCharge(batteryUid.Value, drill.ChargePerSecond * frameTime, battery);
    }
}

