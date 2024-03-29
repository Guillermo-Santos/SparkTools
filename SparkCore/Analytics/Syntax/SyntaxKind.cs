﻿namespace SparkCore.Analytics.Syntax;

public enum SyntaxKind
{
    BadToken,

    // TRIVIA
    SkippedTextTrivia,
    LineBreakTrivia,
    WhiteSpaceTrivia,
    SingleLineCommentTrivia,
    MultiLineCommentTrivia,

    // TOKENS
    EndOfFileToken,
    NumberToken,
    StringToken,
    PlusToken,
    MinusToken,
    StarToken,
    SlashToken,
    OpenParentesisToken,
    CloseParentesisToken,
    CommaToken,
    ColonToken,
    OpenBraceToken,
    CloseBraceToken,
    IdentifierToken,
    BangToken,
    EqualsToken,
    TildeToken,
    HatToken,
    AmpersandToken,
    AmpersandAmpersandToken,
    PibeToken,
    PibePibeToken,
    BangEqualsToken,
    EqualsEqualsToken,
    LessToken,
    LessOrEqualsToken,
    GreaterToken,
    GreaterOrEqualsToken,

    //KEYWORDS
    BreakKeyword,
    ContinueKeyword,
    DoKeyword,
    ElseKeyword,
    FalseKeyword,
    ForKeyword,
    FunctionKeyword,
    IfKeyword,
    LetKeyword,
    ReturnKeyword,
    ToKeyword,
    TrueKeyword,
    VarKeyword,
    WhileKeyword,

    //NODES
    CompilationUnit,
    FunctionDeclaration,
    GlobalStatement,
    Parameter,
    TypeClause,
    ElseClause,

    //STATEMENTS
    BlockStatement,
    VariableDeclarationStatement,
    IfStatement,
    WhileStatement,
    DoWhileStatement,
    ForStatement,
    BreakStatement,
    ContinueStatement,
    ReturnStatement,
    ExpressionStatement,

    // EXPRESSIONS
    LiteralExpression,
    NameExpression,
    UnaryExpression,
    BinaryExpression,
    ParenthesizedExpression,
    AssignmentExpression,
    CallExpression,
}
