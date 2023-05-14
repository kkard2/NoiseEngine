﻿namespace NoiseEngine.Nesl.CompilerTools.Parsing.Tokens;

internal enum OperatorType {
    None = 0,
    Increment,
    Decrement,
    Addition = 100,
    Subtraction,
    Multiplication = 200,
    Division,
    Remainder,
    Exponentation = 300,
    LogicalAnd = 400,
    LogicalOr,
    LogicalXor,
    ConditionalAnd,
    ConditionalOr,
    Bitwise,
    LeftShift,
    RightShift,
    Ternary = 500,
    TernaryElse,
    Equality = 600,
    Inequality,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    Negation,
    NullCoalescing = 700,
    ExplicitCast = 800
}