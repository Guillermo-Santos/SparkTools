﻿using SparkCore;
using SparkCore.Analytics.Symbols;
using SparkCore.Analytics.Syntax.Tree;
using SparkCore.IO;

namespace spi.Replies;

public sealed class SparkRepl : Repl
{
    private bool _loadingSubmission;
    private static readonly Compilation emptyCompilation = Compilation.CreateScript(null);
    private Compilation? _previous;
    private bool _showTree;
    private bool _showProgram;
    private readonly Dictionary<VariableSymbol, object> _variables = new();

    public SparkRepl()
    {
        LoadSubmissions();
    }

    protected override object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
    {
        //Console.WriteLine(DateTime.UtcNow);
        SyntaxTree syntaxTree;

        //if (state is null)
        {
            var text = string.Join(Environment.NewLine, lines);
            syntaxTree = SyntaxTree.Parse(text);
        }
        //else
        //{
        //    syntaxTree = (SyntaxTree)state;
        //}

        var lineSpan = syntaxTree.Text.Lines[lineIndex].Span;
        var classifierVisitor = new SyntaxHightLightVisitor
        {
            TextSpan = lineSpan
        };

        classifierVisitor.Visit(syntaxTree.Root);

        foreach (var classifiedSpan in classifierVisitor.ClassifiedSpans)
        {
            var classifiedText = syntaxTree.Text.ToString(classifiedSpan.Span);
            
            Console.ForegroundColor = classifiedSpan.Classification switch
            {
                Classification.Keyword => ConsoleColor.Blue,
                Classification.Identifier => ConsoleColor.Cyan,
                Classification.Number => ConsoleColor.DarkYellow,
                Classification.String => ConsoleColor.Magenta,
                Classification.Comment => ConsoleColor.DarkGreen,
                Classification.Operator => ConsoleColor.DarkGray,
                _ => ConsoleColor.White,
            };
            Console.Write(classifiedText);
            Console.ResetColor();
        }
        return state;
    }

    [MetaCommand("cls", "Clears the console")]
    private void EvaluateCls()
    {
        Console.Clear();
    }
    [MetaCommand("reset", "Clears all previous submissions")]
    private void EvaluateReset()
    {
        _previous = null;
        _variables.Clear();
        ClearSubmissions();
    }
    [MetaCommand("showTree", "Show the parse tree")]
    private void EvaluateShowTree()
    {
        _showTree = !_showTree;
        Console.WriteLine(_showTree ? "Showing parse trees." : "Not showing parse trees");
    }
    [MetaCommand("showProgram", "Show the bound tree")]
    private void EvaluateShowProgram()
    {
        _showProgram = !_showProgram;
        Console.WriteLine(_showProgram ? "Showing bound trees." : "Not showing bound trees");
    }
    [MetaCommand("load", "Load a script file")]
    private void EvaluateLoad(string path)
    {

        path = Path.GetFullPath(path);
        if (File.Exists(path))
        {
            var text = File.ReadAllText(path);
            EvaluateSubmission(text);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"error: file does not exist '{path}'");
            Console.ResetColor();
        }
    }
    [MetaCommand("ls", "List all symbols")]
    private void EvaluateLs()
    {
        var compilation = _previous ?? emptyCompilation;
        var symbols = compilation.GetSymbols().OrderBy(s => s.Kind).ThenBy(s => s.Name);
        foreach (var symbol in symbols)
        {
            symbol.WriteTo(Console.Out);
            Console.WriteLine();
        }
    }
    [MetaCommand("dump", "Shows bound tree of a given function")]
    private void EvaluateDump(string functionName)
    {
        var compilation = _previous ?? emptyCompilation;
        var function = compilation.GetSymbols().OfType<FunctionSymbol>().SingleOrDefault(f => f.Name == functionName);
        if (function == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"error: function '{functionName}' doesn't exists");
            Console.ResetColor();
            return;
        }

        compilation.EmitTree(function, Console.Out);
        Console.WriteLine();
    }

    protected override bool IsCompletedSubmission(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;

        var lastTwoLinesAreBlank = text.Split(Environment.NewLine)
                                       .Reverse()
                                       .TakeWhile(s => string.IsNullOrEmpty(s))
                                       .Take(2)
                                       .Count() == 2;
        if (lastTwoLinesAreBlank)
            return true;
        var syntaxTree = SyntaxTree.Parse(text);

        var lastMember = syntaxTree.Root.Members.LastOrDefault();
        if (lastMember == null || lastMember.GetLastToken().IsMissing)
            return false;

        return true;
    }
    protected override void EvaluateSubmission(string text)
    {
        var syntaxTree = SyntaxTree.Parse(text);

        var compilation = Compilation.CreateScript(_previous, syntaxTree);

        if (_showTree)
            syntaxTree.Root.WriteTo(Console.Out);

        if (_showProgram)
            compilation.EmitTree(Console.Out);

        var result = compilation.Evaluate(_variables);

        if (!result.Diagnostics.Any())
        {
            if (result.Value != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(result.Value);
                Console.ResetColor();
            }
            _previous = compilation;
            //SaveSubmission(text);

        }
        else
        {
            Console.Out.WriteDiagnostics(result.Diagnostics);
        }
    }

    private static string GetSubmissionsDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var submissionsDirectory = Path.Combine(localAppData, "Spark", "submissions");
        return submissionsDirectory;
    }
    private void LoadSubmissions()
    {
        var submissionsDirectory = GetSubmissionsDirectory();
        if (!Directory.Exists(submissionsDirectory))
        {
            return;
        }

        var files = Directory.GetFiles(submissionsDirectory).OrderBy(f => f).ToArray();

        if (files.Length == 0)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Loaded {files.Length} submission(s)");
        Console.ResetColor();

        _loadingSubmission = true;

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            EvaluateSubmission(text);
        }

        _loadingSubmission = false;
    }
    private static void ClearSubmissions()
    {
        var dir = GetSubmissionsDirectory();
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
    private void SaveSubmission(string text)
    {
        if (_loadingSubmission)
        {
            return;
        }

        var submissionsDirectory = GetSubmissionsDirectory();
        Directory.CreateDirectory(submissionsDirectory);

        var count = Directory.GetFiles(submissionsDirectory).Length;
        var name = $"submission{count:0000}.sp";
        var fileName = Path.Combine(submissionsDirectory, name);
        File.WriteAllText(fileName, text);
    }
}