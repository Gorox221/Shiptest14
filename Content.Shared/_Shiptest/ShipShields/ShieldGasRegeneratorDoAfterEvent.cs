using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Shiptest.ShipShields;

[Serializable, NetSerializable]
public sealed partial class ShieldGasRegeneratorDoAfterEvent : SimpleDoAfterEvent;
