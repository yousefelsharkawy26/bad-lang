using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using BadLang.Core;
using BadLang.Parser;
using BadLang.Backend.Interpreter.Handlers;
using BadLang.Backend.Interpreter.Handlers.ControlFlow;
using BadLang.Backend.Interpreter.Handlers.Variables;
using BadLang.Backend.Interpreter.Handlers.Objects;
using BadLang.Backend.Interpreter.Handlers.Definitions;
using BadLang.Backend.Interpreter.Handlers.Advanced;
using BadLang.Backend.Interpreter.Runtime;
using BadLang.IR;
using BadLang.IR.Optimization;

namespace BadLang.Backend.Interpreter;

public class Interpreter
{
    private const int MaxCallDepth = 512;

    private readonly Environment _globals;
    private Environment _environment;
    private readonly HttpClient _httpClient = new();
    private ModuleLoader? _moduleLoader;
    private int _callDepth = 0;
    private readonly Dictionary<string, Dictionary<object, object?>> _moduleCache = new();
    private readonly TextWriter _out;
    private readonly ExecutionEngine _engine;

    private readonly List<string> _exports = new();
    public Environment Globals => _globals;
    public List<string> Exports => _exports;

    public Interpreter(string? basePath = null, TextWriter? output = null)
    {
        _out = output ?? Console.Out;
        _globals = new Environment();
        _environment = _globals;
        if (basePath != null) _moduleLoader = new ModuleLoader(basePath);
        
        BuiltinRegistry.DefineBuiltins(_globals, _out, _httpClient);
        _engine = new ExecutionEngine(new HandlerRegistry(GetDefaultHandlers()));
    }

    private IEnumerable<IIrNodeHandler> GetDefaultHandlers()
    {
        return new List<IIrNodeHandler>
        {
            new BinaryHandler(),
            new JumpHandler(),
            new CondJumpHandler(),
            new LabelHandler(),
            new ReturnHandler(),
            new ScopeHandlers(),
            new ExceptionHandlers(),
            new ValueHandlers(),
            new FunctionHandler(),
            
            // Variable Handlers
            new DefineHandler(),
            new StoreHandler(),
            new LoadHandler(),
            new AssignHandler(),

            // Object Handlers
            new PropertyGetHandler(),
            new PropertySetHandler(),
            new MethodCallHandler(),
            new SuperPropertyGetHandler(),
            new SuperMethodCallHandler(),
            new NewHandler(),

            // Definition Handlers
            new FunctionDefHandler(),
            new ClassDefHandler(),

            // Advanced Handlers
            new ImportHandler(),
            new ExportHandler(),
            new AssertHandler(),

            new PanicHandler(),
            new LambdaHandler(),
            new StructDefHandler(),
            new EnumDefHandler()
        };
    }

    public void SetBasePath(string path)
    {
        _moduleLoader = new ModuleLoader(path);
    }

    internal object? GetValueInternal(IrValue value, Environment env)
    {
        if (value is IrConst c) return c.Value;
        if (value is IrVar v) return env.Get(v.Name);
        return null;
    }

    public void Interpret(IReadOnlyList<IrNode> irNodes)
    {
        try
        {
            var optimizer = new IROptimizer(new IOptimizationPass[]
            {
                new ConstantPropagationPass(),
                new CopyPropagationPass(),
                new ConstantFoldingPass(),
                new StrengthReductionPass(),
                new LoopOptimizationPass(),
                new DeadCodeEliminationPass()
            });
            var optimized = optimizer.Optimize(irNodes);
            
            ExecuteIR(optimized, _globals);
        }
        catch (RuntimeException error)
        {
            var msg = error.Token != null && error.Token.Lexeme != null 
                ? $"[Runtime Error] {error.Message} near '{error.Token.Lexeme}'"
                : $"[Runtime Error] {error.Message}";
            Console.Error.WriteLine(msg);
            _out.WriteLine(msg);
        }
    }

    public object? ExecuteIR(IReadOnlyList<IrNode> irNodes, Environment env)
    {
        var labels = new Dictionary<string, int>();
        for (int i = 0; i < irNodes.Count; i++)
        {
            if (irNodes[i] is IrLabel lbl)
            {
                labels[lbl.Name] = i;
            }
        }

        var context = new ExecutionContext(this, env, labels);
        return _engine.Execute(irNodes, context);
    }

    internal void HandleCall(string target, IBadLangCallable callable, List<object?> args, Environment env)
    {
        if (_callDepth >= MaxCallDepth) throw new Exception("Stack overflow.");
        _callDepth++;
        try { env.Define(target, callable.Call(this, args)); }
        finally { _callDepth--; }
    }

    internal void HandleImport(IrImport importNode, Environment env)
    {
        if (!_moduleCache.TryGetValue(importNode.Path, out var moduleDict))
        {
            var path = _moduleLoader?.Resolve(importNode.Path);
            if (path == null) throw new Exception($"Module '{importNode.Path}' not found.");
            var source = File.ReadAllText(path);
            var lexer = new BadLang.Lexer.Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new BadLang.Parser.Parser(tokens);
            var ast = parser.Parse();
            var builder = new IRBuilder();
            var ir = builder.Build(ast);
            var moduleInterpreter = new Interpreter(_moduleLoader?.BasePath, _out);
            moduleInterpreter.ExecuteIR(ir, moduleInterpreter.Globals);
            moduleDict = new Dictionary<object, object?>();
            
            var symbolsToExport = moduleInterpreter.Exports.Count > 0 
                ? (IEnumerable<string>)moduleInterpreter.Exports 
                : moduleInterpreter.Globals.GetValues().Select(v => v.Key);

            foreach (var sym in symbolsToExport)
            {
                moduleDict[sym] = moduleInterpreter.Globals.Get(sym);
            }
            _moduleCache[importNode.Path] = moduleDict;
        }

        if (importNode.Symbols != null)
        {
            foreach (var sym in importNode.Symbols)
            {
                if (moduleDict.TryGetValue(sym, out var val))
                {
                    env.Define(sym, val);
                }
                else 
                {
                    var available = string.Join(", ", moduleDict.Keys);
                    throw new Exception($"Symbol '{sym}' not found in module '{importNode.Path}'. Available symbols: {available}");
                }
            }
        }

        if (importNode.Alias != null)
        {
            env.Define(importNode.Alias, moduleDict);
        }
        else if (importNode.Symbols == null)
        {
            foreach (var kvp in moduleDict)
            {
                if (kvp.Key is string symName)
                {
                    env.Define(symName, kvp.Value);
                }
            }
        }
    }
    
    internal void HandleExport(IrExport exportNode)
    {
        if (!_exports.Contains(exportNode.Name))
        {
            _exports.Add(exportNode.Name);
        }
    }
}
