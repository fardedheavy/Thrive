﻿namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles slowing down cells that are currently moving through slime (and don't have slime jets themselves)
/// </summary>
[With(typeof(MicrobeControl))]
[With(typeof(Health))]
[Without(typeof(AttachedToEntity))]
[RunsAfter(typeof(OrganelleComponentFetchSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RuntimeCost(7)]
public sealed class MucocystSystem : AEntitySetSystem<float>
{
    public MucocystSystem(World world, IParallelRunner runner) :
        base(world, runner)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        // Handles invurnebility from mucocyst. Other buffs/debuffs from mucocyst are in related systems
        ref var control = ref entity.Get<MicrobeControl>();

        if (control.State == MicrobeState.MucocystShield)
        {
            entity.Get<Health>().Invulnerable = true;
        }
        else
        {
            entity.Get<Health>().Invulnerable = CheatManager.GodMode;
        }
    }
}
