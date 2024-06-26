﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SparkCore.IO.Text;

namespace SparkCore.Analytics.Syntax.Tree;

public abstract class SyntaxNode
{
    protected SyntaxNode(SyntaxTree syntaxTree)
    {
        SyntaxTree = syntaxTree;
    }
    public SyntaxTree SyntaxTree
    {
        get;
    }
    public abstract SyntaxKind Kind
    {
        get;
    }
    public virtual TextSpan Span
    {
        get
        {
            var visitor = new TextSpanVisitor();
            return visitor.Visit(this);
            //var first = GetChildren().First().Span;
            //var last = GetChildren().Last().Span;
            //return TextSpan.FromBounds(first.Start, last.End);
        }
    }
    public virtual TextSpan FullSpan
    {
        get
        {
            var visitor = new FullTextSpanVisitor();
            return visitor.Visit(this);
            //var first = GetChildren().First().FullSpan;
            //var last = GetChildren().Last().FullSpan;
            //return TextSpan.FromBounds(first.Start, last.End);
        }
    }

    public TextLocation Location => new(SyntaxTree.Text, Span);

    public SyntaxToken GetLastToken()
    {
        if (this is SyntaxToken token)
        {
            return token;
        }

        return GetChildren().Last().GetLastToken();
    }
    public abstract IEnumerable<SyntaxNode> GetChildren();

    /// <summary>
    /// Calls the appropiate Visit[<see cref="SyntaxNode"/>] method of the <paramref name="visitor"/>
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void Accept(SyntaxNodeVisitor visitor)
    {
        visitor.DefaultVisit(this);
    }

    /// <summary>
    /// Calls the appropiate Visit[<see cref="SyntaxNode"/>] method of the <paramref name="visitor"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="visitor"></param>
    /// <returns>A <typeparamref name="T"/> object or null</returns>
    public virtual T? Accept<T>(SyntaxNodeVisitor<T> visitor)
    {
        return visitor.DefaultVisit(this);
    }
    public void WriteTo(TextWriter writter, bool withTrivia = true)
    {
        PrettyPrint(writter, this, withTrivia);
    }
    private static void PrettyPrint(TextWriter writter, SyntaxNode node, bool withTrivia = true, string indent = "", bool isLast = true)
    {
        if (node == null)
            return;

        var isToConsole = writter == Console.Out;
        var token = node as SyntaxToken;

        if (withTrivia && token != null)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                writter.Write(indent);
                writter.Write("├──");
                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;

                writter.WriteLine($"L: {trivia.Kind}");
            }
        }

        var hasTrailingTrivia = token != null && token.TrailingTrivia.Any();
        var tokenMarker = !hasTrailingTrivia && isLast ? "└──" : "├──";

        if (isToConsole)
            Console.ForegroundColor = ConsoleColor.DarkGray;

        writter.Write(indent);
        writter.Write(tokenMarker);

        if (isToConsole)
            Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

        writter.Write(node.Kind);

        if (token != null && token.Text != null)
        {
            writter.Write(" text -> \'");
            writter.Write(token.Text);
            writter.Write("\'");
        }

        if (token != null && token.Value != null)
        {
            writter.Write(" value -> \'");
            writter.Write(token.Value);
            writter.Write("\'");
        }

        writter.WriteLine();

        if (withTrivia && token != null)
        {
            foreach (var trivia in token.TrailingTrivia)
            {
                var isLastTrailingTrivia = trivia == token.TrailingTrivia.Last();
                var triviaMarker = isLast && isLastTrailingTrivia ? "└──" : "├──";

                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                writter.Write(indent);
                writter.Write(triviaMarker);
                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;

                writter.WriteLine($"T: {trivia.Kind}");
            }
        }
        if (isToConsole)
            Console.ResetColor();

        indent += isLast ? "   " : "│  ";
        var lastChild = node.GetChildren().LastOrDefault();
        foreach (var child in node.GetChildren())
            PrettyPrint(writter, child, withTrivia, indent, child == lastChild);
    }

    public override string ToString()
    {
        return ToString(withTrivia: false);
    }
    public string ToString(bool withTrivia = true)
    {
        using (var writter = new StringWriter())
        {
            WriteTo(writter, withTrivia);
            return writter.ToString();
        }
    }


}
