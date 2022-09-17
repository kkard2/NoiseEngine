﻿namespace NoiseEngine.CodeGenerators.Shared.Interop.InteropMarshalling;

internal class ReadOnlySpanMarshal : InteropMarshal {

    public override string MarshallingType => "System.ReadOnlySpan";
    public override string UnmarshallingType => "NoiseEngine.Interop.InteropMarshalling.InteropReadOnlySpan";
    public override bool IsAdvanced => true;

    public override string Marshall(string unmarshalledParameterName, out string marshalledParameterName) {
        marshalledParameterName = CreateUniqueVariableName();
        string finalType = $"{UnmarshallingType}<{GenericRawString}>";
        string a = CreateUniqueVariableName();

        return @$"
            fixed ({GenericRawString}* {a} = {unmarshalledParameterName}) {{
                {finalType} {marshalledParameterName} = new {finalType}({a}, {unmarshalledParameterName}.Length);

                {MarshalContinuation}
            }}
        ";
    }

    public override string Unmarshall(string marshalledParameterName, out string unmarshalledParamterName) {
        unmarshalledParamterName = CreateUniqueVariableName();
        return $"{MarshallingType}<{GenericRawString}> {unmarshalledParamterName} " +
            $"= {marshalledParameterName}.AsSpan();";
    }

}
