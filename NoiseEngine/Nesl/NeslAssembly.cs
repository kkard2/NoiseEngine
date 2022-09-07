﻿using System.Collections.Generic;

namespace NoiseEngine.Nesl;

public abstract class NeslAssembly {

    public abstract IEnumerable<NeslAssembly> Dependencies { get; }
    public abstract IEnumerable<NeslType> Types { get; }

    public string Name { get; }

    protected NeslAssembly(string name) {
        Name = name;
    }

    /// <summary>
    /// Finds <see cref="NeslType"/> with given <paramref name="fullName"/> in this <see cref="NeslAssembly"/>.
    /// </summary>
    /// <param name="fullName">Full name of the searched <see cref="NeslType"/>.</param>
    /// <returns><see cref="NeslType"/> when type was found, <see langword="null"/> when not.</returns>
    public NeslType? GetType(string fullName) {
        foreach (NeslType type in Types) {
            if (type.FullName == fullName)
                return type;
        }

        return null;
    }

    internal abstract ulong GetLocalTypeId(NeslType type);
    internal abstract ulong GetLocalMethodId(NeslMethod method);
    internal abstract NeslType GetType(ulong localTypeId);
    internal abstract NeslMethod GetMethod(ulong localMethodId);

}
