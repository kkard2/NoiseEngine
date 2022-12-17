﻿using NoiseEngine.Nesl.CompilerTools.Architectures.SpirV.IlCompilation;
using NoiseEngine.Nesl.CompilerTools.Architectures.SpirV.Types;
using System;

namespace NoiseEngine.Nesl.CompilerTools.Architectures.SpirV;

internal class SpirVFunction {

    public SpirVCompiler Compiler { get; }
    public NeslMethod NeslMethod { get; }

    public SpirVGenerator SpirVGenerator { get; }

    public SpirVId Id { get; }

    public SpirVFunction(SpirVCompiler compiler, NeslMethod neslMethod) {
        Compiler = compiler;
        NeslMethod = neslMethod;

        SpirVGenerator = new SpirVGenerator(Compiler);
        Id = Compiler.GetNextId();

        BeginFunction();
    }

    internal void Construct(SpirVGenerator generator) {
        generator.Writer.WriteBytes(SpirVGenerator.Writer.AsSpan());
        generator.Emit(SpirVOpCode.OpFunctionEnd);
    }

    private void BeginFunction() {
        SpirVType returnType;

        if (Compiler.TryGetEntryPoint(NeslMethod, out NeslEntryPoint entryPoint)) {
            returnType = entryPoint.ExecutionModel switch {
                ExecutionModel.Fragment => BeginFunctionFragment(),
                ExecutionModel.GLCompute => Compiler.GetSpirVType(NeslMethod.ReturnType),
                _ => throw new NotImplementedException()
            };
        } else {
            returnType = Compiler.GetSpirVType(NeslMethod.ReturnType);
        }

        SpirVType functionType = Compiler.BuiltInTypes.GetOpTypeFunction(returnType);

        // TODO: implement function control.
        SpirVGenerator.Emit(SpirVOpCode.OpFunction, returnType.Id, Id, 0, functionType.Id);

        // Parameters.
        SpirVVariable[] parameters = new SpirVVariable[NeslMethod.ParameterTypes.Count];
        for (int i = 0; i < parameters.Length; i++) {
            SpirVId id = Compiler.GetNextId();
            NeslType neslType = NeslMethod.ParameterTypes[i];
            SpirVType type = Compiler.GetSpirVType(neslType);

            SpirVGenerator.Emit(SpirVOpCode.OpFunctionParameter, type.Id, id);
            parameters[i] = SpirVVariable.CreateFromParameter(Compiler, neslType, id);
        }

        // Label and code.
        SpirVGenerator.Emit(SpirVOpCode.OpLabel, Compiler.GetNextId());

        new IlCompiler(Compiler, NeslMethod.GetInstructions(), NeslMethod, SpirVGenerator, parameters).Compile();
    }

    private SpirVType BeginFunctionFragment() {
        if (NeslMethod.ReturnType is not null) {
            SpirVVariable variable = new SpirVVariable(
                Compiler, NeslMethod.ReturnType, StorageClass.Output, Compiler.TypesAndVariables
            );
            Compiler.AddVariable(variable);

            lock (Compiler.Annotations) {
                Compiler.Annotations.Emit(
                    SpirVOpCode.OpDecorate, variable.Id, (uint)Decoration.Location, 0u.ToSpirVLiteral()
                );
            }
        }

        uint location = 0;
        foreach (NeslType parameterType in NeslMethod.ParameterTypes) {
            SpirVVariable variable = new SpirVVariable(
                Compiler, parameterType, StorageClass.Input, Compiler.TypesAndVariables
            );
            Compiler.AddVariable(variable);

            lock (Compiler.Annotations) {
                Compiler.Annotations.Emit(
                    SpirVOpCode.OpDecorate, variable.Id, (uint)Decoration.Location,
                    location++.ToSpirVLiteral()
                );
            }
        }

        return Compiler.BuiltInTypes.GetOpTypeVoid();
    }

}
