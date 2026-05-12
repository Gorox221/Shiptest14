// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Shuttles.Systems;
using Content.Shared.Shuttles.UI.MapObjects;
using Content.Shared.Timing;
using Content.Shared._Shiptest.SpaceBiomes;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// Handles BUI data for Map screen.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleMapInterfaceState
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState;

    /// <summary>
    /// When the current FTL state starts and ends.
    /// </summary>
    public StartEndTime FTLTime;

    public List<ShuttleBeaconObject> Destinations;

    public List<ShuttleExclusionObject> Exclusions;

    /// <summary>
    /// _Shiptest: Space biome zones displayed on the map.
    /// </summary>
    public List<BiomeZoneObject> BiomeZones;

    /// <summary>
    /// _Shiptest: If true, scanning is blocked (e.g., inside Nebula biome).
    /// Client should show "no signal" and not display grids.
    /// </summary>
    public bool ScanningBlocked = false;

    /// <summary>
    /// When true, shuttle map view center must stay within <see cref="MapPanClampMin"/>..<see cref="MapPanClampMax"/>
    /// for <see cref="MapPanClampMap"/> so the visible radar square does not leave the playable interior.
    /// </summary>
    public readonly bool MapPanClampActive;

    public readonly MapId MapPanClampMap;

    public readonly Vector2 MapPanClampMin;

    public readonly Vector2 MapPanClampMax;

    public ShuttleMapInterfaceState(
        FTLState ftlState,
        StartEndTime ftlTime,
        List<ShuttleBeaconObject> destinations,
        List<ShuttleExclusionObject> exclusions,
        List<BiomeZoneObject>? biomeZones = null,
        bool scanningBlocked = false,
        bool mapPanClampActive = false,
        MapId mapPanClampMap = default,
        Vector2 mapPanClampMin = default,
        Vector2 mapPanClampMax = default)
    {
        FTLState = ftlState;
        FTLTime = ftlTime;
        Destinations = destinations;
        Exclusions = exclusions;
        BiomeZones = biomeZones ?? new List<BiomeZoneObject>();
        ScanningBlocked = scanningBlocked;
        MapPanClampActive = mapPanClampActive;
        MapPanClampMap = mapPanClampMap;
        MapPanClampMin = mapPanClampMin;
        MapPanClampMax = mapPanClampMax;
    }
}