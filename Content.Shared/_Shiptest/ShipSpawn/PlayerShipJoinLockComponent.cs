using System;
using System.Collections.Generic;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shiptest.ShipSpawn;

/// <summary>
/// Runtime lock state for player-created ships that require a password to join.
/// </summary>
[RegisterComponent]
public sealed partial class PlayerShipJoinLockComponent : Component
{
    /// <summary>
    /// Whether this ship currently requires a password to late-join.
    /// </summary>
    public bool IsClosed;

    /// <summary>
    /// SHA256 hash of "salt:password" for password checks.
    /// </summary>
    public string PasswordHash = string.Empty;

    /// <summary>
    /// Random salt used for password hashing.
    /// </summary>
    public string PasswordSalt = string.Empty;

    /// <summary>
    /// Captain of the ship for direct notifications.
    /// </summary>
    public NetUserId CaptainUserId;

    /// <summary>
    /// Job considered captain and excluded from threshold calculations.
    /// </summary>
    public ProtoId<JobPrototype> CaptainJob;

    /// <summary>
    /// Minimal number of non-captain crew required before timeout to keep lock.
    /// </summary>
    public int RequiredNonCaptainCrew;

    /// <summary>
    /// How many unique non-captain players have joined this ship.
    /// </summary>
    public HashSet<NetUserId> JoinedNonCaptainCrew = new();

    /// <summary>
    /// Time when lock should be evaluated for automatic unlock.
    /// </summary>
    public TimeSpan AutoUnlockAt;

    /// <summary>
    /// If true, auto-unlock has already been evaluated and should not run again.
    /// </summary>
    public bool AutoUnlockEvaluated;
}
