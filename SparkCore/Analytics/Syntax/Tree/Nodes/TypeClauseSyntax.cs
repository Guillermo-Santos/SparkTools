﻿namespace SparkCore.Analytics.Syntax.Tree.Nodes;

public sealed partial class TypeClauseSyntax : SyntaxNode
{
    public TypeClauseSyntax(SyntaxTree syntaxTree, SyntaxToken colonToken, SyntaxToken identifier) : base(syntaxTree)
    {
        ColonToken = colonToken;
        Identifier = identifier;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeClause;
    public SyntaxToken ColonToken
    {
        get;
    }
    public SyntaxToken Identifier
    {
        get;
    }
}

