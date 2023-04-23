﻿using NoiseEngine.Collections.Concurrent;
using NoiseEngine.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NoiseEngine.Jobs2;

public abstract class EntitySystem : IDisposable {

    #region NoiseEngineInternal

    [Obsolete("This struct is internal and is not part of the API. Do not use.")]
    protected struct NoiseEngineInternal_DoNotUse {

        public readonly record struct ExecutionData(nint RecordSize, nint StartIndex, nint EndIndex) {

            private readonly Dictionary<Type, nint> offsets;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ExecutionData(
                ArchetypeChunk chunk, nint startIndex, nint endIndex
            ) : this(chunk.RecordSize, startIndex, endIndex) {
                offsets = chunk.Offsets;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public nint GetOffset<T>() where T : IComponent {
                return offsets[typeof(T)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe ref T Get<T>(nint index) where T : IComponent {
                return ref Unsafe.AsRef<T>((byte*)index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Entity? GetInternalComponent(nint index) {
                return Get<EntityInternalComponent>(index).Entity;
            }

        }

        public readonly record struct ComponentUsage(Type Type, bool WriteAccess);

        public ComponentUsage[] UsedComponents { get; set; }
        public bool ComponentWriteAccess { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static ref T NullRef<T>() {
            return ref Unsafe.AsRef<T>((void*)null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void UpdateComponent<T>(in T oldValue, in T newValue) where T : IComponent {
            if (oldValue is IAffectiveComponent<T> affective) {
                if (affective.AffectiveEquals(newValue)) {
                    if (oldValue is IEquatable<T> equatable) {
                        if (equatable.Equals(newValue))
                            return;
                    } else if (oldValue.Equals(newValue)) {
                        return;
                    }
                }
            } else if (oldValue is IEquatable<T> equatable) {
                if (equatable.Equals(newValue))
                    return;
            } else if (oldValue.Equals(newValue)) {
                return;
            }
        }

    }

    [Obsolete("This field is internal and is not part of the API. Do not use.")]
    protected NoiseEngineInternal_DoNotUse NoiseEngineInternal_DoNotUse_Storage;

    [Obsolete("This method is internal and is not part of the API. Do not use.")]
    protected virtual void NoiseEngineInternal_DoNotUse_Initialize() {
        throw new InvalidOperationException("NoiseEngine.Generator did not generate the code.");
    }

    [Obsolete("This method is internal and is not part of the API. Do not use.")]
    protected virtual void NoiseEngineInternal_DoNotUse_SystemExecution(
        NoiseEngineInternal_DoNotUse.ExecutionData data, SystemCommands commands
    ) {
        throw new InvalidOperationException("NoiseEngine.Generator did not generate the code.");
    }

    #endregion

    internal readonly ConcurrentList<Archetype> archetypes = new ConcurrentList<Archetype>();

    internal long lastExecutionTime = DateTime.UtcNow.Ticks;
    internal long cycleTimeWithDelta;

    private readonly object workLocker = new object();
    private readonly ManualResetEventSlim workResetEvent = new ManualResetEventSlim(true);
    private readonly object scheduleLocker = new object();

    private EntityWorld? world;
    private uint ongoingWork;
    private AtomicBool isWorking;
    private AtomicBool isDisposed;
    private EntitySchedule? schedule;
    private double? cycleTime;
    private bool isDoneInitialize;

    public bool IsInitialized => world is not null;
    public bool IsWorking => isWorking;
    public bool IsDisposed => isDisposed;

    public EntityWorld World {
        get {
            if (world is not null)
                return world;

            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            else
                throw new InvalidOperationException("This system is not initialized.");
        }
    }

    public EntitySchedule? Schedule {
        get => schedule;
        set {
            lock (scheduleLocker) {
                if (schedule is not null && CycleTime.HasValue && isDoneInitialize)
                    schedule.Worker.UnregisterSystem(this);

                schedule = value;
                if (value is not null && CycleTime.HasValue && isDoneInitialize)
                    value.Worker.RegisterSystem(this);
            }
        }
    }

    public double? CycleTime {
        get => cycleTime;
        set {
            lock (scheduleLocker) {
                if (Schedule is null) {
                    cycleTime = value;
                    return;
                }

                if (value.HasValue) {
                    if (!cycleTime.HasValue && isDoneInitialize)
                        Schedule.Worker.RegisterSystem(this);
                } else if (cycleTime.HasValue && isDoneInitialize) {
                    Schedule.Worker.UnregisterSystem(this);
                }

                cycleTime = value;
            }
        }
    }

    protected double DeltaTime { get; private set; } = 1;
    protected float DeltaTimeF { get; private set; } = 1;

#pragma warning disable CS0618
    internal bool ComponentWriteAccess => NoiseEngineInternal_DoNotUse_Storage.ComponentWriteAccess;

    private IEnumerable<Type> UsedComponents => NoiseEngineInternal_DoNotUse_Storage.UsedComponents
        .Select(x => x.Type);
#pragma warning restore CS0618

    /// <summary>
    /// Disposes this <see cref="EntitySystem"/>.
    /// </summary>
    public void Dispose() {
        if (isDisposed.Exchange(true))
            return;

        Schedule = null;
        OnTerminate();

        World.RemoveSystem(this);
        world = null;
    }

    internal void InternalInitialize(EntityWorld world) {
        if (Interlocked.CompareExchange(ref this.world, world, null) is not null)
            throw new InvalidOperationException("System is already initialized.");

#pragma warning disable CS0618
        NoiseEngineInternal_DoNotUse_Initialize();
#pragma warning restore CS0618
        OnInitialize();

        lock (scheduleLocker) {
            isDoneInitialize = true;
            Schedule ??= world.DefaultSchedule;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SystemExecutionInternal(
        ArchetypeChunk chunk, nint startPointer, nint endPointer, SystemCommands systemCommands
    ) {
#pragma warning disable CS0618
        NoiseEngineInternal_DoNotUse_SystemExecution(new NoiseEngineInternal_DoNotUse.ExecutionData(
            chunk, startPointer, endPointer
        ), systemCommands);
#pragma warning restore CS0618
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalUpdate() {
        long executionTime = DateTime.UtcNow.Ticks;

        long difference = executionTime - lastExecutionTime;
        DeltaTime = difference / (double)TimeSpan.TicksPerSecond;
        DeltaTimeF = (float)DeltaTime;

        double? cycleTime = CycleTime;
        if (cycleTime.HasValue) {
            long cycleTimeInTicks = (long)(cycleTime.Value * TimeSpan.TicksPerMillisecond);
            cycleTimeWithDelta = cycleTimeInTicks - (difference - cycleTimeInTicks);
        }

        lastExecutionTime = executionTime;
        OnUpdate();
    }

    internal void RegisterArchetype(Archetype archetype) {
        foreach (Type type in UsedComponents) {
            if (!archetype.Offsets.ContainsKey(type))
                return;
        }

        lock (archetypes) {
            if (!archetypes.Contains(archetype))
                archetypes.Add(archetype);
        }
    }

    internal void OrderWork() {
        if (Interlocked.Increment(ref ongoingWork) != 1)
            return;

        lock (workLocker) {
            isWorking = true;
            workResetEvent.Reset();
        }
    }

    internal void ReleaseWork() {
        if (Interlocked.Decrement(ref ongoingWork) != 0)
            return;

        OnLateUpdate();
        lock (workLocker) {
            isWorking = false;
            workResetEvent.Set();
        }
    }

    internal bool TryOrderWork() {
        if (isWorking.Exchange(true))
            return false;

        OrderWork();
        return true;
    }

    /// <summary>
    /// This method is executed when this system is initializing.
    /// </summary>
    protected virtual void OnInitialize() {
    }

    /// <summary>
    /// This method is executed when this system is enabling.
    /// </summary>
    protected virtual void OnStart() {
    }

    /// <summary>
    /// This method is executed on begin of every cycle of this system.
    /// </summary>
    protected virtual void OnUpdate() {
    }

    /// <summary>
    /// This method is executed on end of every cycle of this system.
    /// </summary>
    protected virtual void OnLateUpdate() {
    }

    /// <summary>
    /// This method is executed when this system is disabling.
    /// </summary>
    protected virtual void OnStop() {
    }

    /// <summary>
    /// This method is executed when this system is destroying.
    /// </summary>
    protected virtual void OnTerminate() {
    }

}
