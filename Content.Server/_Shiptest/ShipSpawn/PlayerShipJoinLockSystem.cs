using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._Shiptest.ShipSpawn;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Server._Shiptest.ShipSpawn;

/// <summary>
/// Manages password-protected player ship joins and timed automatic unlock.
/// </summary>
public sealed class PlayerShipJoinLockSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        var query = EntityQueryEnumerator<PlayerShipJoinLockComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemComp<PlayerShipJoinLockComponent>(uid);
        }

        BroadcastLockStatus();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PlayerShipJoinLockComponent>();
        while (query.MoveNext(out var station, out var lockComp))
        {
            if (lockComp.AutoUnlockEvaluated || now < lockComp.AutoUnlockAt)
                continue;

            lockComp.AutoUnlockEvaluated = true;

            if (!lockComp.IsClosed || lockComp.JoinedNonCaptainCrew.Count >= lockComp.RequiredNonCaptainCrew)
            {
                Dirty(station, lockComp);
                continue;
            }

            lockComp.IsClosed = false;
            Dirty(station, lockComp);

            if (_players.TryGetSessionById(lockComp.CaptainUserId, out var captain))
            {
                _chat.DispatchServerMessage(captain, Loc.GetString("player-ship-lock-auto-opened-captain"));
            }

            BroadcastLockStatus();
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!TryComp<PlayerShipJoinLockComponent>(ev.Station, out var lockComp))
            return;

        if (ev.JobId is null || ev.Player.UserId == lockComp.CaptainUserId || ev.JobId == lockComp.CaptainJob)
            return;

        if (lockComp.JoinedNonCaptainCrew.Add(ev.Player.UserId))
            Dirty(ev.Station, lockComp);
    }

    public void SetupLock(
        EntityUid station,
        NetUserId captainUser,
        ProtoId<JobPrototype> captainJob,
        IReadOnlyDictionary<ProtoId<JobPrototype>, int[]> jobs,
        bool closed,
        string? password)
    {
        if (!closed || string.IsNullOrWhiteSpace(password))
        {
            if (TryComp<PlayerShipJoinLockComponent>(station, out _))
                RemComp<PlayerShipJoinLockComponent>(station);
            return;
        }

        var lockComp = EnsureComp<PlayerShipJoinLockComponent>(station);
        lockComp.IsClosed = true;
        lockComp.CaptainUserId = captainUser;
        lockComp.CaptainJob = captainJob;
        lockComp.AutoUnlockAt = _timing.CurTime + LockTimeout;
        lockComp.AutoUnlockEvaluated = false;
        lockComp.JoinedNonCaptainCrew.Clear();
        lockComp.RequiredNonCaptainCrew = CalculateRequiredCrew(jobs, captainJob);

        lockComp.PasswordSalt = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        lockComp.PasswordHash = HashPassword(lockComp.PasswordSalt, password.Trim());
        Dirty(station, lockComp);
        BroadcastLockStatus();
    }

    public bool IsStationLocked(EntityUid station)
    {
        return TryComp<PlayerShipJoinLockComponent>(station, out var lockComp) && lockComp.IsClosed;
    }

    public bool TryValidateJoinPassword(EntityUid station, string? password)
    {
        if (!TryComp<PlayerShipJoinLockComponent>(station, out var lockComp) || !lockComp.IsClosed)
            return true;

        if (string.IsNullOrWhiteSpace(password))
            return false;

        var candidate = HashPassword(lockComp.PasswordSalt, password.Trim());
        return string.Equals(candidate, lockComp.PasswordHash, StringComparison.Ordinal);
    }

    private static int CalculateRequiredCrew(IReadOnlyDictionary<ProtoId<JobPrototype>, int[]> jobs, ProtoId<JobPrototype> captainJob)
    {
        var totalNonCaptain = jobs
            .Where(job => job.Key != captainJob)
            .Sum(job => job.Value.Length > 1 ? Math.Max(job.Value[1], 0) : 0);

        return (int)Math.Ceiling(totalNonCaptain * 0.30f);
    }

    private static string HashPassword(string salt, string password)
    {
        var bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        RaiseNetworkEvent(BuildLockStatusEvent(), ev.PlayerSession.Channel);
    }

    private void BroadcastLockStatus()
    {
        RaiseNetworkEvent(BuildLockStatusEvent(), Filter.Broadcast());
    }

    private PlayerShipJoinLockStatusEvent BuildLockStatusEvent()
    {
        var locked = new Dictionary<NetEntity, bool>();
        var query = EntityQueryEnumerator<PlayerShipJoinLockComponent>();
        while (query.MoveNext(out var station, out var lockComp))
        {
            locked[GetNetEntity(station)] = lockComp.IsClosed;
        }

        return new PlayerShipJoinLockStatusEvent(locked);
    }
}
