using Robust.Shared.Prototypes;
using Content.Shared._Lavaland.Procedural.Prototypes;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._Shiptest.SpaceBiomes;

/// <summary>
/// Per-planet spawn configuration for static BSS-reachable planets.
/// </summary>
[Prototype("spaceTravelPlanet")]
public sealed partial class SpaceTravelPlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Planetary biome template used by this planet configuration.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> PlanetBiome = "Grasslands";

    /// <summary>
    /// Optional beacon marker prototype to spawn at planet center.
    /// </summary>
    [DataField]
    public EntProtoId BeaconPrototype = "FTLPoint";

    /// <summary>
    /// Whether travel to this destination requires a coordinates disk.
    /// Mirrors expedition destination behavior.
    /// </summary>
    [DataField]
    public bool RequireCoordinatesDisk = false;

    /// <summary>
    /// Atmosphere preset applied to the planet map.
    /// Reuses salvage atmosphere prototypes for convenient configuration.
    /// </summary>
    [DataField]
    public ProtoId<SalvageAirMod> Atmosphere = "Breathable";

    /// <summary>
    /// Atmosphere temperature in Kelvin.
    /// </summary>
    [DataField]
    public float AtmosphereTemperature = 293.15f;

    /// <summary>
    /// Optional lavaland-style ruin pool spawned on this planet map.
    /// </summary>
    [DataField]
    public ProtoId<LavalandRuinPoolPrototype>? RuinPool;

    /// <summary>
    /// If enabled, guarantees one nearby mapped (grid) ruin near the arrival point.
    /// </summary>
    [DataField]
    public bool GuaranteeNearbyGridRuin = true;

    /// <summary>
    /// Minimal distance from arrival center for guaranteed nearby mapped ruin.
    /// </summary>
    [DataField]
    public float GuaranteedGridRuinMinDistance = 80f;

    /// <summary>
    /// Maximum distance from arrival center for guaranteed nearby mapped ruin.
    /// </summary>
    [DataField]
    public float GuaranteedGridRuinMaxDistance = 140f;

    /// <summary>
    /// Max random placement attempts for the guaranteed nearby mapped ruin.
    /// </summary>
    [DataField]
    public int GuaranteedGridRuinAttempts = 24;

    /// <summary>
    /// Minimal offset from beacon center when searching a nearby arrival point.
    /// </summary>
    [DataField]
    public float ArrivalMinOffset = 96f;

    /// <summary>
    /// Maximum radial distance used to search a nearby free arrival point.
    /// </summary>
    [DataField]
    public float ArrivalSearchRadius = 384f;

    /// <summary>
    /// Step between tested arrival radii while searching.
    /// </summary>
    [DataField]
    public float ArrivalSearchStep = 32f;

    /// <summary>
    /// Circular collision boundary radius around the planet center.
    /// Used for the station-map beacon zone representation.
    /// </summary>
    [DataField]
    public float RestrictedRange = 900f;

    /// <summary>
    /// Actual playable boundary radius on the planet map itself.
    /// </summary>
    [DataField]
    public float MapBoundaryRange = 900f;

    /// <summary>
    /// Space biome source priority.
    /// </summary>
    [DataField]
    public int Priority = 10;
}

/// <summary>
/// Global settings for static BSS planet placement.
/// </summary>
[Prototype("spaceTravelPlanetSpawnSettings")]
public sealed partial class SpaceTravelPlanetSpawnSettingsPrototype : IPrototype
{
    public const string DefaultId = "SpaceTravelPlanets";

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Only positions at or beyond this distance from station center are valid.
    /// </summary>
    [DataField]
    public float MinDistanceFromStationCenter = 7500f;

    /// <summary>
    /// Upper distance bound for random placement.
    /// </summary>
    [DataField]
    public float MaxDistanceFromStationCenter = 24000f;

    /// <summary>
    /// Required existing biome id at placement position.
    /// </summary>
    [DataField]
    public string RequiredPlacementBiome = "DefaultSpace";

    /// <summary>
    /// Required spacing between spawned planets.
    /// </summary>
    [DataField]
    public float MinPlanetSeparation = 3000f;

    /// <summary>
    /// Placement retries per planet.
    /// </summary>
    [DataField]
    public int PlacementAttempts = 128;

    /// <summary>
    /// Ordered planet definitions to spawn each round.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<SpaceTravelPlanetPrototype>))]
    public List<string> Planets = new();
}
