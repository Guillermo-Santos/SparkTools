﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using SparkCore.Analytics.Syntax.Lexic;
using SparkCore.Analytics.Syntax.Tree.Nodes;
using SparkCore.IO.Diagnostics;
using SparkCore.IO.Text;

namespace SparkCore.Analytics.Syntax.Tree;

public sealed class SyntaxTree
{
    private delegate void ParseHandler(SyntaxTree syntaxTree,
                                       out CompilationUnitSyntax root,
                                       out IEnumerable<Diagnostic> diagnostics);
    private SyntaxTree(SourceText text, ParseHandler handler)
    {
        Text = text;

        handler(this, out var root, out var diagnostics);

        Diagnostics = diagnostics;
        Root = root;
    }

    public SourceText Text
    {
        get;
    }
    public IEnumerable<Diagnostic> Diagnostics
    {
        get;
    }
    public CompilationUnitSyntax Root
    {
        get;
    }

    public static SyntaxTree Load(string fileName)
    {
        var text = File.ReadAllText(fileName);
        var sourceText = SourceText.From(text, fileName);
        return Parse(sourceText);
    }
    private static void Parse(SyntaxTree syntaxTree, out CompilationUnitSyntax root, out IEnumerable<Diagnostic> diagnostics)
    {
        var parser = new Parser(syntaxTree);
        root = parser.ParseCompilationUnit();
        diagnostics = parser.Diagnostics;
    }
    public static SyntaxTree Parse(string text)
    {
        var sourceText = SourceText.From(text);
        return Parse(sourceText);
    }
    public static SyntaxTree Parse(SourceText text)
    {
        return new SyntaxTree(text, Parse);
    }
    public static IEnumerable   <SyntaxToken> ParseTokens(string text, bool includeEndOfFile = false)
    {
        var sourceText = SourceText.From(text);
        return ParseTokens(sourceText, includeEndOfFile);
    }
    public static IEnumerable<SyntaxToken> ParseTokens(string text, out IEnumerable<Diagnostic> diagnostics, bool includeEndOfFile = false)
    {
        var sourceText = SourceText.From(text);
        return ParseTokens(sourceText, out diagnostics, includeEndOfFile);
    }
    public static IEnumerable<SyntaxToken> ParseTokens(SourceText text, bool includeEndOfFile = false)
    {
        return ParseTokens(text, out _, includeEndOfFile);
    }
    public static IEnumerable<SyntaxToken> ParseTokens(SourceText text, out IEnumerable<Diagnostic> diagnostics, bool includeEndOfFile = false)
    {
        var tokens = new List<SyntaxToken>();

        void ParseTokens(SyntaxTree st, out CompilationUnitSyntax root, out IEnumerable<Diagnostic> d)
        {

            var l = new LexicAnalyzer(st);
            while (true)
            {
                var token = l.Lex();
                if (token.Kind != SyntaxKind.EndOfFileToken || includeEndOfFile)
                {
                    tokens.Add(token);
                }

                if (token.Kind == SyntaxKind.EndOfFileToken)
                {
                    root = new CompilationUnitSyntax(st, Array.Empty<MemberSyntax>(), token);
                    break;
                }
            }
            d = l.Diagnostics;
        }

        var syntaxTree = new SyntaxTree(text, ParseTokens);

        diagnostics = syntaxTree.Diagnostics;
        return tokens;
    }
}
