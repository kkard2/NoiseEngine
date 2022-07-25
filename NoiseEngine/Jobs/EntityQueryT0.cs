﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace NoiseEngine.Jobs;

public class EntityQuery : EntityQueryBase, IEnumerable<Entity> {

    public EntityQuery(
        EntityWorld world, IReadOnlyList<Type>? writableComponents = null, IEntityFilter? filter = null
    ) :
        base(world, writableComponents, filter)
    {
    }

    /// <summary>
    /// Returns an enumerator that iterates through this <see cref="EntityQuery"/>.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through this <see cref="EntityQuery"/>.</returns>
    public IEnumerator<Entity> GetEnumerator() {
        foreach (Entity entity in Entities)
            yield return entity;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

}