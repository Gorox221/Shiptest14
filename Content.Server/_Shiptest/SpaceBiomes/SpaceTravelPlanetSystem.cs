using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Parallax;
using Content.Server.Salvage;
using Content.Shared._Lavaland.Procedural.Prototypes;
using Content.Shared.Atmos;
using Content.Shared._Shiptest.SpaceBiomes;
using Content.Shared._Shiptest.SpaceBiomes.Components;
using Content.Shared.GameTicking;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Shiptest.SpaceBiomes;

/// <summary>
/// Spawns configured static planets in empty space, reachable via normal BSS travel.
/// </summary>
public sealed class SpaceTravelPlanetSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpaceBiomeGridSystem _biomeGrid = default!;
    [Dependency] private readonly RestrictedRangeSystem _restrictedRange = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<EntityUid> _spawnedPlanetMaps = new();
    private readonly List<EntityUid> _spawnedPlanetBeacons = new();
    private readonly List<EntityUid> _spawnedPlanetBoundaries = new();
    private bool _spawnAttempted;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        // Biome grid is initialized by station startup; attempt once here.
        TrySpawnConfiguredPlanets();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        foreach (var uid in _spawnedPlanetMaps)
        {
            if (Exists(uid))
                QueueDel(uid);
        }

        foreach (var uid in _spawnedPlanetBeacons)
        {
            if (Exists(uid))
                QueueDel(uid);
        }

        foreach (var uid in _spawnedPlanetBoundaries)
        {
            if (Exists(uid))
                QueueDel(uid);
        }

        _spawnedPlanetMaps.Clear();
        _spawnedPlanetBeacons.Clear();
        _spawnedPlanetBoundaries.Clear();
        _spawnAttempted = false;
    }

    private void TrySpawnConfiguredPlanets()
    {
        if (_spawnAttempted)
            return;

        _spawnAttempted = true;

        if (!_proto.TryIndex<SpaceTravelPlanetSpawnSettingsPrototype>(
                SpaceTravelPlanetSpawnSettingsPrototype.DefaultId,
                out var settings))
        {
            return;
        }

        if (!_biomeGrid.TryGetWrapState(out var mapId, out var mapEntity, out var stationCenter))
            return;

        var placed = new List<Vector2>();
        foreach (var planetId in settings.Planets)
        {
            if (!_proto.TryIndex(new ProtoId<SpaceTravelPlanetPrototype>(planetId), out var planet))
                continue;

            if (!TryFindPlanetPosition(settings, stationCenter, placed, out var worldPos))
                continue;

            if (!_proto.HasIndex<EntityPrototype>(planet.BeaconPrototype))
                continue;

            if (!_proto.TryIndex(planet.PlanetBiome, out BiomeTemplatePrototype? biomeTemplate))
                continue;

            if (!_proto.TryIndex<SalvageAirMod>(planet.Atmosphere, out var atmosphere))
                continue;

            var planetMapUid = _map.CreateMap(out var planetMapId, runMapInit: false);
            _metaData.SetEntityName(planetMapUid, planet.ID);
            _biome.EnsurePlanet(planetMapUid, biomeTemplate);

            var destination = EnsureComp<FTLDestinationComponent>(planetMapUid);
            destination.BeaconsOnly = true;
            destination.RequireCoordinateDisk = planet.RequireCoordinatesDisk;
            destination.Enabled = true;
            Dirty(planetMapUid, destination);

            var mapBoundaryUid = _restrictedRange.CreateBoundary(
                new EntityCoordinates(planetMapUid, worldPos),
                planet.MapBoundaryRange);
            _spawnedPlanetBoundaries.Add(mapBoundaryUid);

            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            atmosphere.Gases.CopyTo(moles, 0);
            var mapAtmosphere = EnsureComp<MapAtmosphereComponent>(planetMapUid);
            _atmosphere.SetMapSpace(planetMapUid, atmosphere.Space, mapAtmosphere);
            _atmosphere.SetMapGasMixture(planetMapUid, new GasMixture(moles, planet.AtmosphereTemperature), mapAtmosphere);
            _map.InitializeMap(planetMapId);

            if (planet.RuinPool != null && _proto.TryIndex(planet.RuinPool.Value, out LavalandRuinPoolPrototype? pool))
            {
                SetupPlanetRuins(pool, planetMapUid, planetMapId, _random.Next(), worldPos, planet);
            }

            var beaconUid = Spawn(planet.BeaconPrototype, new MapCoordinates(worldPos, mapId));
            _metaData.SetEntityName(beaconUid, planet.ID);
            var planetBeacon = EnsureComp<SpaceTravelPlanetBeaconComponent>(beaconUid);
            planetBeacon.DestinationMap = planetMapUid;
            planetBeacon.ArrivalMinOffset = planet.ArrivalMinOffset;
            planetBeacon.ArrivalSearchRadius = planet.ArrivalSearchRadius;
            planetBeacon.ArrivalSearchStep = planet.ArrivalSearchStep;
            Dirty(beaconUid, planetBeacon);

            var boundaryUid = _restrictedRange.CreateBoundary(
                new EntityCoordinates(mapEntity, worldPos),
                planet.RestrictedRange);

            placed.Add(worldPos);
            _spawnedPlanetMaps.Add(planetMapUid);
            _spawnedPlanetBeacons.Add(beaconUid);
            _spawnedPlanetBoundaries.Add(boundaryUid);
        }
    }

    private void SetupPlanetRuins(
        LavalandRuinPoolPrototype pool,
        EntityUid planetMapUid,
        MapId mapId,
        int seed,
        Vector2 arrivalCenter,
        SpaceTravelPlanetPrototype planet)
    {
        var random = new Random(seed);
        var coords = GetRuinCoordinates(pool.RuinDistance, pool.MaxDistance, arrivalCenter);
        random.Shuffle(coords);

        // Keep center area free for arrivals.
        var center = arrivalCenter.Rounded();
        var usedSpace = new List<Box2>
        {
            Box2.FromDimensions(center - new Vector2(48f, 48f), new Vector2(96f, 96f))
        };

        var mutableGridRuins = new Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort>(pool.GridRuins);
        if (planet.GuaranteeNearbyGridRuin)
        {
            TrySpawnGuaranteedNearbyGridRuin(
                mutableGridRuins,
                planetMapUid,
                mapId,
                random,
                ref coords,
                usedSpace,
                arrivalCenter,
                planet.GuaranteedGridRuinMinDistance,
                planet.GuaranteedGridRuinMaxDistance,
                planet.GuaranteedGridRuinAttempts);
        }

        SetupGridRuins(mutableGridRuins, planetMapUid, mapId, random, ref coords, usedSpace);
        SetupDungeonRuins(pool.DungeonRuins, planetMapUid, random, ref coords, usedSpace);
    }

    private void TrySpawnGuaranteedNearbyGridRuin(
        Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort> ruins,
        EntityUid planetMapUid,
        MapId mapId,
        Random random,
        ref List<Vector2i> coords,
        List<Box2> usedSpace,
        Vector2 arrivalCenter,
        float minDistance,
        float maxDistance,
        int attempts)
    {
        if (ruins.Count == 0 || coords.Count == 0)
            return;

        minDistance = MathF.Max(0f, minDistance);
        maxDistance = MathF.Max(minDistance, maxDistance);
        attempts = Math.Max(1, attempts);

        var availableRuins = BuildWeightedGridRuinList(ruins);
        if (availableRuins.Count == 0)
            return;

        var center = new Vector2i((int)MathF.Round(arrivalCenter.X), (int)MathF.Round(arrivalCenter.Y));

        for (var i = 0; i < attempts; i++)
        {
            if (availableRuins.Count == 0)
                return;

            var ruin = random.Pick(availableRuins);
            var angle = random.NextFloat(0f, MathF.Tau);
            var radius = random.NextFloat(minDistance, maxDistance);
            var coord = center + new Vector2i(
                (int)MathF.Round(MathF.Cos(angle) * radius),
                (int)MathF.Round(MathF.Sin(angle) * radius));

            if (!coords.Contains(coord))
                continue;

            if (!TrySpawnGridRuinAtCoordinate(ruin, planetMapUid, mapId, coord, usedSpace))
                continue;

            coords.Remove(coord);

            if (ruins.TryGetValue(new ProtoId<LavalandGridRuinPrototype>(ruin.ID), out var count))
            {
                if (count <= 1)
                    ruins.Remove(new ProtoId<LavalandGridRuinPrototype>(ruin.ID));
                else
                    ruins[new ProtoId<LavalandGridRuinPrototype>(ruin.ID)] = (ushort)(count - 1);
            }

            return;
        }
    }

    private void SetupGridRuins(
        Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort> ruins,
        EntityUid planetMapUid,
        MapId mapId,
        Random random,
        ref List<Vector2i> coords,
        List<Box2> usedSpace)
    {
        var list = new List<LavalandGridRuinPrototype>();
        foreach (var (protoId, count) in ruins)
        {
            var proto = _proto.Index(protoId);
            for (var i = 0; i < count; i++)
            {
                list.Add(proto);
            }
        }

        list.Sort((x, y) => x.Priority.CompareTo(y.Priority));

        foreach (var ruin in list)
        {
            var attempts = 0;
            while (!TrySpawnGridRuin(ruin, planetMapUid, mapId, random, ref coords, usedSpace))
            {
                attempts++;
                if (attempts > ruin.SpawnAttempts)
                    break;
            }
        }
    }

    private bool TrySpawnGridRuin(
        LavalandGridRuinPrototype ruin,
        EntityUid planetMapUid,
        MapId mapId,
        Random random,
        ref List<Vector2i> coords,
        List<Box2> usedSpace)
    {
        if (coords.Count == 0)
            return false;

        var coord = random.Pick(coords);
        if (!TrySpawnGridRuinAtCoordinate(ruin, planetMapUid, mapId, coord, usedSpace))
            return false;

        coords.Remove(coord);
        return true;
    }

    private bool TrySpawnGridRuinAtCoordinate(
        LavalandGridRuinPrototype ruin,
        EntityUid planetMapUid,
        MapId mapId,
        Vector2i coord,
        List<Box2> usedSpace)
    {
        if (!_mapLoader.TryLoadGrid(mapId, ruin.Path, out var loaded))
            return false;

        var ruinGrid = loaded.Value;
        var ruinBox = ruinGrid.Comp.LocalAABB.Translated(coord);

        if (usedSpace.Any(used => used.Intersects(ruinBox)))
        {
            QueueDel(ruinGrid);
            return false;
        }

        _transform.SetCoordinates(ruinGrid, new EntityCoordinates(planetMapUid, coord));
        _metaData.SetEntityName(ruinGrid, Loc.GetString(ruin.Name));
        usedSpace.Add(ruinBox);
        ReserveBiomeArea(planetMapUid, ruinBox);
        return true;
    }

    private void SetupDungeonRuins(
        Dictionary<ProtoId<LavalandDungeonRuinPrototype>, ushort> ruins,
        EntityUid planetMapUid,
        Random random,
        ref List<Vector2i> coords,
        List<Box2> usedSpace)
    {
        var list = new List<LavalandDungeonRuinPrototype>();
        foreach (var (protoId, count) in ruins)
        {
            var proto = _proto.Index(protoId);
            for (var i = 0; i < count; i++)
            {
                list.Add(proto);
            }
        }

        list.Sort((x, y) => x.Priority.CompareTo(y.Priority));

        foreach (var ruin in list)
        {
            var attempts = 0;
            while (!TrySpawnDungeonMarker(ruin, planetMapUid, random, ref coords, usedSpace))
            {
                attempts++;
                if (attempts > ruin.SpawnAttempts)
                    break;
            }
        }
    }

    private bool TrySpawnDungeonMarker(
        LavalandDungeonRuinPrototype ruin,
        EntityUid planetMapUid,
        Random random,
        ref List<Vector2i> coords,
        List<Box2> usedSpace)
    {
        if (coords.Count == 0)
            return false;

        var coord = random.Pick(coords);
        var box = Box2.CentredAroundZero(ruin.Boundary);
        var ruinBox = box.Translated(coord);

        if (usedSpace.Any(used => used.Intersects(ruinBox)))
            return false;

        Spawn(ruin.SpawnedMarker, new EntityCoordinates(planetMapUid, coord));
        usedSpace.Add(ruinBox);
        ReserveBiomeArea(planetMapUid, ruinBox);
        coords.Remove(coord);
        return true;
    }

    private static List<Vector2i> GetRuinCoordinates(int distance, int maxDistance, Vector2 center)
    {
        var step = Math.Max(distance, 1);
        var coords = new List<Vector2i>();
        var moveVector = new Vector2i(maxDistance, maxDistance);
        var centerRounded = new Vector2i((int)MathF.Round(center.X), (int)MathF.Round(center.Y));

        while (moveVector.Y >= -maxDistance)
        {
            while (moveVector.X > -maxDistance)
            {
                coords.Add(moveVector + centerRounded);
                moveVector += new Vector2i(-step, 0);
            }

            coords.Add(moveVector + centerRounded);
            moveVector += new Vector2i(0, -step);

            while (moveVector.X < maxDistance)
            {
                coords.Add(moveVector + centerRounded);
                moveVector += new Vector2i(step, 0);
            }

            coords.Add(moveVector + centerRounded);
            moveVector += new Vector2i(0, -step);
        }

        return coords;
    }

    private List<LavalandGridRuinPrototype> BuildWeightedGridRuinList(
        Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort> ruins)
    {
        var list = new List<LavalandGridRuinPrototype>();
        foreach (var (protoId, count) in ruins)
        {
            var proto = _proto.Index(protoId);
            for (var i = 0; i < count; i++)
            {
                list.Add(proto);
            }
        }

        return list;
    }

    private void ReserveBiomeArea(EntityUid planetMapUid, Box2 area)
    {
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        _biome.ReserveTiles(planetMapUid, area, tiles);
    }

    private bool TryFindPlanetPosition(
        SpaceTravelPlanetSpawnSettingsPrototype settings,
        Vector2 stationCenter,
        List<Vector2> alreadyPlaced,
        out Vector2 worldPos)
    {
        for (var i = 0; i < settings.PlacementAttempts; i++)
        {
            var dir = _random.NextAngle().ToVec();
            var dist = _random.NextFloat(settings.MinDistanceFromStationCenter, settings.MaxDistanceFromStationCenter);
            var candidate = stationCenter + dir * dist;

            if (!string.Equals(
                    _biomeGrid.GetBiomeAt(candidate, stationCenter),
                    settings.RequiredPlacementBiome,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var tooClose = false;
            foreach (var other in alreadyPlaced)
            {
                if (Vector2.Distance(candidate, other) < settings.MinPlanetSeparation)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            worldPos = candidate;
            return true;
        }

        worldPos = default;
        return false;
    }

}
