using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server._Shiptest.Shuttles;

public sealed class PlasmaThrusterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanister = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly ThrusterSystem _thruster = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlasmaThrusterComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<PlasmaThrusterComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
    }

    private void OnAtmosUpdate(EntityUid uid, PlasmaThrusterComponent plasma, ref AtmosDeviceUpdateEvent args)
    {
        if (_nodeContainer.TryGetNode(uid, plasma.PortName, out PipeNode? pipeNode))
        {
            _atmosphere.React(plasma.Air, pipeNode);
            if (pipeNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
                _gasCanister.MixContainerWithPipeNet(plasma.Air, net.Air);
        }

        if (!TryComp<ThrusterComponent>(uid, out var thruster))
            return;

        if (!PlasmaThrusterFuel.TryResolveFuelGas(_prototype, plasma.FuelGas, out var fuelGas))
            return;

        if (thruster is { IsOn: true, Firing: true })
        {
            var used = plasma.FuelMolesPerSecondFiring * args.dt;
            var available = plasma.Air.GetMoles(fuelGas);
            var take = MathF.Min(used, available);
            if (take > 0f)
                plasma.Air.AdjustMoles(fuelGas, -take);
        }

        var moles = plasma.Air.GetMoles(fuelGas);

        if (thruster.IsOn && moles < plasma.MinimumFuelMoles)
        {
            _thruster.DisableThruster(uid, thruster);
            return;
        }

        if (!thruster.IsOn && thruster.Enabled && moles >= plasma.MinimumFuelMoles && _thruster.CanEnable(uid, thruster))
            _thruster.EnableThruster(uid, thruster);
    }

    private void OnGasAnalyzed(EntityUid uid, PlasmaThrusterComponent plasma, ref GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(uid), plasma.Air));
    }
}
