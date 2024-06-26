﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using SparkCore.Analytics;
using SparkCore.Analytics.Binding;
using SparkCore.Analytics.Binding.Tree;
using SparkCore.Analytics.Symbols;
using SparkCore.Analytics.Syntax.Tree;
using SparkCore.Emit;
using SparkCore.IO.Diagnostics;

namespace SparkCore;

public class Compilation
{
    private BoundGlobalScope _globalScope;
    private Compilation(bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
    {
        IsScript = isScript;
        Previous = previous;
        SyntaxTrees = syntaxTrees.ToImmutableArray();
    }

    public static Compilation Create(params SyntaxTree[] syntaxTrees)
    {
        return new Compilation(isScript: false, null, syntaxTrees);
    }
    public static Compilation CreateScript(Compilation? previous, params SyntaxTree[] syntaxTrees)
    {
        return new Compilation(isScript: true, previous, syntaxTrees);
    }

    public bool IsScript
    {
        get;
    }
    public Compilation? Previous
    {
        get;
    }
    public IEnumerable<SyntaxTree> SyntaxTrees
    {
        get;
    }
    public FunctionSymbol? MainFunction => GlobalScope.MainFunction;
    public IEnumerable<FunctionSymbol> Functions => GlobalScope.Functions;
    public IEnumerable<VariableSymbol> Variables => GlobalScope.Variables;

    internal BoundGlobalScope GlobalScope
    {
        get
        {
            if (_globalScope == null)
            {
                var globalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees);
                Interlocked.CompareExchange(ref _globalScope, globalScope, null);
            }
            return _globalScope;
        }
    }

    public IEnumerable<Symbol> GetSymbols()
    {
        var submission = this;
        var seenSymbolNames = new HashSet<string>();

        var builtinFunction = BuiltinFunctions.GetAll();

        while (submission != null)
        {
            foreach (var function in submission.Functions)
            {
                if (seenSymbolNames.Add(function.Name))
                {
                    yield return function;
                }
            }

            foreach (var variable in submission.Variables)
            {
                if (seenSymbolNames.Add(variable.Name))
                {
                    yield return variable;
                }
            }

            foreach (var builtin in builtinFunction)
            {
                if (seenSymbolNames.Add(builtin.Name))
                {
                    yield return builtin;
                }
            }
            submission = submission.Previous;
        }
    }

    public Compilation ContinueWith(SyntaxTree syntaxTree)
    {
        return CreateScript(this, syntaxTree);
    }

    private BoundProgram GetProgram()
    {
        var previous = Previous?.GetProgram();
        return Binder.BindProgram(IsScript, previous, GlobalScope);
    }
    public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
    {
        if (GlobalScope.Diagnostics.Any())
        {
            return new EvaluationResult(GlobalScope.Diagnostics, null);
        }

        var program = GetProgram();

        // TODO: Sacar la impresion a una funcion. Crear directorio \Temp y logica de limpiado con cada cierre de la app.
        // Control Flow evaluation
        //foreach (var function in program.Functions)
        //{
        //    var appPath = Environment.GetCommandLineArgs()[0];
        //    var appDirectory = Path.GetDirectoryName(appPath);
        //    var cfgPath = Path.Combine(appDirectory, $"{function.Key.Name}.dot");
        //    var cfgStatement = function.Value;
        //    var cfg = ControlFlowGraph.Create(cfgStatement);
        //    using (var streamWriter = new StreamWriter(cfgPath))
        //        cfg.WriteTo(streamWriter);
        //}
        // =========================

        if (program.Diagnostics.Any())
        {
            return new EvaluationResult(program.Diagnostics, default);
        }

        var evaluator = new Evaluator(program, variables);
        var value = evaluator.Evaluate();
        return new EvaluationResult(Array.Empty<Diagnostic>(), value);
    }
    /// <summary>
    /// Run an empty evaluation that just run the analyzers.
    /// </summary>
    /// <returns>
    /// An empty <see cref="EvaluationResult"/> which contains the <see cref="Diagnostic"/>/s of the program.
    /// </returns>
    public EvaluationResult Evaluate()
    {
        if (GlobalScope.Diagnostics.Any())
        {
            return new EvaluationResult(GlobalScope.Diagnostics, null);
        }

        var program = GetProgram();
        return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);
    }

    public void EmitTree(TextWriter writer)
    {
        if (GlobalScope.MainFunction != null)
        {
            EmitTree(GlobalScope.MainFunction, writer);
        }
        else if (GlobalScope.ScriptFunction != null)
        {
            EmitTree(GlobalScope.ScriptFunction, writer);
        }

        foreach (var function in GlobalScope.Functions.Where(f => f != GlobalScope.MainFunction && f != GlobalScope.ScriptFunction))
        {
            EmitTree(function, writer);
        }
    }

    public void EmitTree(FunctionSymbol function, TextWriter writer)
    {
        var program = GetProgram();
        function.WriteTo(writer);
        writer.WriteLine();
        if (!program.Functions.TryGetValue(function, out var body))
        {
            return;
        }

        body.WriteTo(writer);
    }
    public IEnumerable<Diagnostic> Emit(string moduleName, string[] references, string outputPath)
    {
        var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);

        var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics);
        if (diagnostics.Any())
        {
            return diagnostics;
        }

        var program = GetProgram();
        return Emitter.Emit(program, moduleName, references, outputPath);
    }
}