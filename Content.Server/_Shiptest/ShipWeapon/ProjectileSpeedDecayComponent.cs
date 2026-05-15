using Robust.Shared.Physics.Components;

namespace Content.Server._Shiptest.ShipWeapon;

/// <summary>
/// Applies linear deceleration to projectile velocity until it stops.
/// Intended for ship munitions that should travel a limited distance then stall.
/// </summary>
[RegisterComponent]
[Access(typeof(ProjectileSpeedDecaySystem))]
public sealed partial class ProjectileSpeedDecayComponent : Component
{
    /// <summary>
    /// Desired travel distance before full stop, in meters.
    /// Used to auto-calculate <see cref="Deceleration"/> from initial speed.
    /// </summary>
    [DataField]
    public float StopAfterDistance = 300f;

    /// <summary>
    /// Linear deceleration in m/s^2. If zero or below, computed from initial speed and stop distance.
    /// </summary>
    [DataField]
    public float Deceleration;

    [DataField]
    public float StopSpeedThreshold = 0.5f;

    [ViewVariables]
    public bool Initialized;
}
