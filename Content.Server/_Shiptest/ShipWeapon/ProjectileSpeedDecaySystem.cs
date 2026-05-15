using System.Numerics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server._Shiptest.ShipWeapon;

public sealed class ProjectileSpeedDecaySystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProjectileSpeedDecayComponent, ProjectileComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var decay, out var projectile, out var physics))
        {
            if (projectile.ProjectileSpent)
                continue;

            var velocity = physics.LinearVelocity;
            var speed = velocity.Length();

            if (!decay.Initialized)
            {
                decay.Initialized = true;
                if (decay.Deceleration <= 0f && decay.StopAfterDistance > 0f)
                    decay.Deceleration = speed * speed / (2f * decay.StopAfterDistance);
            }

            if (speed <= decay.StopSpeedThreshold || decay.Deceleration <= 0f)
            {
                if (speed > 0f)
                    _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);

                continue;
            }

            var nextSpeed = MathF.Max(0f, speed - decay.Deceleration * frameTime);
            if (nextSpeed <= decay.StopSpeedThreshold)
            {
                _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
                continue;
            }

            _physics.SetLinearVelocity(uid, Vector2.Normalize(velocity) * nextSpeed, body: physics);
        }
    }
}
