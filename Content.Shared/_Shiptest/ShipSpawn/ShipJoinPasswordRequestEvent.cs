using Robust.Shared.Serialization;
using Robust.Shared.Network;

namespace Content.Shared._Shiptest.ShipSpawn;

/// <summary>
/// Client request to late-join a station/job with optional ship password.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShipJoinPasswordRequestEvent : EntityEventArgs
{
    public string JobId { get; }
    public NetEntity Station { get; }
    public string? Password { get; }

    public ShipJoinPasswordRequestEvent(string jobId, NetEntity station, string? password = null)
    {
        JobId = jobId;
        Station = station;
        Password = password;
    }
}
