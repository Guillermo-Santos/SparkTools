﻿using SparkCore.Analytics.Symbols;
using SparkCore.Analytics.Syntax;

namespace SparkCore.Analytics.Binding;

internal sealed class BoundUnaryOperator
{
    private BoundUnaryOperator(SyntaxKind syntaxtype, BoundUnaryOperatorKind kind, TypeSymbol operandType) :
        this(syntaxtype, kind, operandType, operandType)
    {
    }
    private BoundUnaryOperator(SyntaxKind syntaxtype, BoundUnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
    {
        SyntaxKind = syntaxtype;
        Kind = kind;
        OperandType = operandType;
        Type = resultType;
    }

    public SyntaxKind SyntaxKind
    {
        get;
    }
    public BoundUnaryOperatorKind Kind
    {
        get;
    }
    public TypeSymbol OperandType
    {
        get;
    }
    public TypeSymbol Type
    {
        get;
    }

    private static readonly BoundUnaryOperator[] _operators =
    {
        new BoundUnaryOperator(SyntaxKind.BangToken, BoundUnaryOperatorKind.LogicalNegation, TypeSymbol.Bool),

        new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.Int),
        new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.Int),
        new BoundUnaryOperator(SyntaxKind.TildeToken, BoundUnaryOperatorKind.OnesComplement, TypeSymbol.Int),
    };

    public static BoundUnaryOperator? Bind(SyntaxKind SyntaxType, TypeSymbol operandtype)
    {
        foreach (var op in _operators)
        {
            if (op.SyntaxKind == SyntaxType && op.OperandType == operandtype)
                return op;
        }
        return null;
    }
}

