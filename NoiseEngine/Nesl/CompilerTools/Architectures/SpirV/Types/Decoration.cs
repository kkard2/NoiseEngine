﻿namespace NoiseEngine.Nesl.CompilerTools.Architectures.SpirV.Types;

/// <summary>
/// https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html#Decoration
/// </summary>
internal enum Decoration : uint {
    Block = 2,
    BufferBlock = 3,
    ArrayStride = 6,
    Location = 30,
    Binding = 33,
    DescriptorSet = 34,
    Offset = 35,
    FPRoundingMode = 39
}
