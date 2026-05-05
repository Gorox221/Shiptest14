using System.Globalization;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Shiptest.Shuttles;

public static class PlasmaThrusterFuel
{
    public static bool TryResolveFuelGas(IPrototypeManager prototypes, ProtoId<GasPrototype> protoId, out Gas gas)
    {
        gas = default;

        if (!prototypes.TryIndex(protoId, out _))
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
}
