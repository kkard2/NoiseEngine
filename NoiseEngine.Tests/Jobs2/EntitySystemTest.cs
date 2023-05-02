﻿using NoiseEngine.Jobs2;
using NoiseEngine.Tests.Environments;
using NoiseEngine.Tests.Fixtures;
using System;
using System.Collections.Generic;

namespace NoiseEngine.Tests.Jobs2;

public class EntitySystemTest : ApplicationTestEnvironment {

    public EntitySystemTest(ApplicationFixture fixture) : base(fixture) {
    }

    [Fact]
    public void Filter() {
        using ScheduleTestSystemA system = new ScheduleTestSystemA() {
            Filter = new EntityFilter(new Type[] { typeof(MockComponentB) }, new Type[] { typeof(MockComponentC) })
        };
        EntityWorld.AddSystem(system);

        List<Entity> entities = new List<Entity> {
            EntityWorld.Spawn(MockComponentA.TestValueA),
            EntityWorld.Spawn(MockComponentA.TestValueA, MockComponentB.TestValueA),
            EntityWorld.Spawn(MockComponentB.TestValueA),
            EntityWorld.Spawn(MockComponentA.TestValueA, MockComponentB.TestValueA, MockComponentC.TestValueA),
            EntityWorld.Spawn(MockComponentC.TestValueA),
            EntityWorld.Spawn(MockComponentB.TestValueA, MockComponentC.TestValueA)
        };

        system.ExecuteAndWait();
        Assert.Equal(1, system.UpdateEntityCount);

        foreach (Entity entity in entities)
            entity.Despawn();
    }

}
