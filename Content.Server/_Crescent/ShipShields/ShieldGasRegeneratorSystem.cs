using System.Collections.Generic;
using System.Globalization;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._Crescent.ShipShields;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Server._Crescent.ShipShields;

public sealed class ShieldGasRegeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanister = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly ShipShieldsSystem _shipShields = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShieldGasRegeneratorComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ShieldGasRegeneratorComponent, ShieldGasRegeneratorDoAfterEvent>(OnDoAfterComplete);
        SubscribeLocalEvent<ShieldGasRegeneratorComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<ShieldGasRegeneratorComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<ShieldGasRegeneratorComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
    }

    private void OnInteractHand(EntityUid uid, ShieldGasRegeneratorComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var ev = new ShieldGasRegeneratorDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, component.ActivationDoAfter, ev, uid, target: uid, used: null)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 2f,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfterComplete(EntityUid uid, ShieldGasRegeneratorComponent component, ref ShieldGasRegeneratorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        component.Active = !component.Active;
        if (!component.Active)
            SetForcedOffline(uid, false);
        else if (!CanRun(uid, component, out var emitter))
        {
            component.Active = false;
            SetForcedOffline(uid, false);
            return;
        }
        else
        {
            emitter.Comp.ForcedOffline = true;
            Dirty(emitter.Owner, emitter.Comp);
        }

        Dirty(uid, component);

        var key = component.Active
            ? "shield-gas-regenerator-started"
            : "shield-gas-regenerator-stopped";
        _popup.PopupEntity(Loc.GetString(key), uid, args.User, PopupType.SmallCaution);
    }

    private void OnAtmosUpdate(EntityUid uid, ShieldGasRegeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        if (_nodeContainer.TryGetNode(uid, component.PortName, out PipeNode? pipeNode))
        {
            _atmosphere.React(component.Air, pipeNode);
            if (pipeNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
                _gasCanister.MixContainerWithPipeNet(component.Air, net.Air);
        }

        if (!component.Active)
        {
            SetForcedOffline(uid, false);
            return;
        }

        if (!TryResolveFuelGas(component.FuelGas, out var fuelGas))
        {
            component.Active = false;
            SetForcedOffline(uid, false);
            Dirty(uid, component);
            return;
        }

        if (!CanRun(uid, component, out var emitter))
        {
            component.Active = false;
            SetForcedOffline(uid, false);
            Dirty(uid, component);
            return;
        }

        var availableFuel = component.Air.GetMoles(fuelGas);
        if (availableFuel <= 0f)
        {
            component.Active = false;
            Dirty(uid, component);
            PopupNoFuel(uid, component.FuelGas);
            return;
        }

        emitter.Comp.Recharging = true;
        emitter.Comp.ForcedOffline = true;
        emitter.Comp.OverloadAccumulator = MathF.Max(emitter.Comp.OverloadAccumulator, component.MinOverloadLock);
        if (emitter.Comp.Shielded is { } grid && emitter.Comp.Shield is not null)
        {
            _shipShields.UnshieldEntity(grid);
            emitter.Comp.Shield = null;
            emitter.Comp.Shielded = null;
        }

        var molesRequested = component.FuelMolesPerSecond * args.dt;
        var molesUsed = MathF.Min(molesRequested, availableFuel);
        component.Air.AdjustMoles(fuelGas, -molesUsed);

        var healed = molesUsed * component.ShieldHpPerFuelMole;
        emitter.Comp.Damage = MathF.Max(0f, emitter.Comp.Damage - healed);
        Dirty(emitter.Owner, emitter.Comp);

        if (component.Air.GetMoles(fuelGas) <= 0f || emitter.Comp.Damage <= 0f)
        {
            component.Active = false;
            SetForcedOffline(uid, false);
            Dirty(uid, component);
        }
    }

    private void OnUiOpenAttempt(EntityUid uid, ShieldGasRegeneratorComponent component, ref ActivatableUIOpenAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnGasAnalyzed(EntityUid uid, ShieldGasRegeneratorComponent component, ref GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(uid), component.Air));
    }

    private bool CanRun(EntityUid uid, ShieldGasRegeneratorComponent component, out Entity<ShipShieldEmitterComponent> emitter)
    {
        emitter = default;

        if (!TryResolveFuelGas(component.FuelGas, out var fuelGas))
            return false;

        if (Transform(uid).GridUid is not { } grid)
        {
            _popup.PopupEntity(Loc.GetString("shield-gas-regenerator-no-grid"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!_shipShields.TryGetShieldEmitter(grid, out var emitterUid, out var emitterComp))
        {
            _popup.PopupEntity(Loc.GetString("shield-gas-regenerator-no-emitter"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (component.Air.GetMoles(fuelGas) <= 0f)
        {
            PopupNoFuel(uid, component.FuelGas);
            return false;
        }

        emitter = (emitterUid!.Value, emitterComp!);
        return true;
    }

    private void PopupNoFuel(EntityUid uid, ProtoId<GasPrototype> fuelGasProto)
    {
        var gasLabel = FuelGasLabel(fuelGasProto);
        _popup.PopupEntity(Loc.GetString("shield-gas-regenerator-no-fuel", ("gas", gasLabel)), uid, uid, PopupType.SmallCaution);
    }

    private string FuelGasLabel(ProtoId<GasPrototype> fuelGasProto)
    {
        if (_prototype.TryIndex(fuelGasProto, out var proto))
            return Loc.GetString(proto.Name);

        return fuelGasProto.Id;
    }

    private bool TryResolveFuelGas(ProtoId<GasPrototype> protoId, out Gas gas)
    {
        gas = default;

        if (!_prototype.TryIndex(protoId, out _))
            return false;

        if (!int.TryParse(protoId.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
            || idx < 0
            || idx >= Atmospherics.TotalNumberOfGases)
        {
            return false;
        }

        gas = (Gas) idx;
        return true;
    }

    private void SetForcedOffline(EntityUid uid, bool forced)
    {
        if (Transform(uid).GridUid is not { } grid)
            return;

        if (!_shipShields.TryGetShieldEmitter(grid, out var emitterUid, out var emitterComp))
            return;

        if (emitterComp!.ForcedOffline == forced)
            return;

        emitterComp.ForcedOffline = forced;
        Dirty(emitterUid!.Value, emitterComp);
    }
}
