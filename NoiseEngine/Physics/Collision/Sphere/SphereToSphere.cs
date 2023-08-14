﻿using NoiseEngine.Jobs;
using NoiseEngine.Mathematics;
using System;
using System.Runtime.CompilerServices;

namespace NoiseEngine.Physics.Collision.Sphere;

internal static class SphereToSphere {

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Collide(
        SystemCommands commands, ContactPointsBuffer buffer, in SphereCollider current,
        float currentRestitutionPlusOneNegative, in ColliderTransform currentTransform, Entity currentEntity,
        SphereCollider other, float otherRestitutionPlusOneNegative, in ColliderTransform otherTransform,
        Entity otherEntity
    ) {
        float currentRadius = current.ScaledRadius(currentTransform.Scale);
        float c = currentRadius + other.ScaledRadius(otherTransform.Scale);
        float squaredDistance = currentTransform.Position.DistanceSquared(otherTransform.Position);
        if (squaredDistance > c * c)
            return;

        float depth = c - MathF.Sqrt(squaredDistance);
        Vector3<float> normal = (otherTransform.Position - currentTransform.Position).Normalize();
        Vector3<float> contactPoint;
        float jB;

        if (otherTransform.IsMovable) {
            depth *= 0.5f;
            contactPoint = normal * (currentRadius - depth) + currentTransform.Position;

            Vector3<float> rb = contactPoint - otherTransform.WorldCenterOfMass;
            jB = otherTransform.InverseMass + normal.Dot(
                (otherTransform.InverseInertiaTensorMatrix * normal.Cross(rb)).Cross(rb)
            );
        } else {
            contactPoint = normal * (currentRadius - (depth * 0.5f)) + currentTransform.Position;
            jB = 0;

            if (otherTransform.IsRigidBody)
                commands.GetEntity(otherEntity).Insert(new RigidBodySleepComponent(true));
        }

        buffer.Add(currentEntity, new ContactPoint(
            contactPoint,
            normal,
            depth,
            otherTransform.LinearVelocity,
            currentTransform.InverseMass,
            jB,
            MathF.Max(currentRestitutionPlusOneNegative, otherRestitutionPlusOneNegative)
        ));
    }

}
