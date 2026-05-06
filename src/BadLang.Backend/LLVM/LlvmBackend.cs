using BadLang.Backend.LLVM.Abstractions;
using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Handlers;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM;

public class LlvmBackend : IDisposable
{
    private readonly CompilationSession _session;
    private readonly LlvmStatementCompiler _statementCompiler;
    private readonly ICompilerInfrastructure _infrastructure;
    private string? _basePath;

    public LlvmBackend(string moduleName)
    {
        LLVMSharp.Interop.LLVM.InitializeNativeTarget();
        LLVMSharp.Interop.LLVM.InitializeNativeAsmPrinter();
        LLVMSharp.Interop.LLVM.InitializeNativeAsmParser();

        var context = LLVMContextRef.Create();
        var module = context.CreateModuleWithName(moduleName);
        var builder = context.CreateBuilder();

        _infrastructure = new LlvmInfrastructure(context, module, builder);
        var symbols = new LlvmSymbolTable();
        var runtime = new LlvmRuntimeProvider();

        _session = new CompilationSession(_infrastructure, symbols, runtime);
        var expressionCompiler = new LlvmExpressionCompiler(_session);
        _statementCompiler = new LlvmStatementCompiler(_session);

        expressionCompiler.SetupDefaultHandlers(_statementCompiler);
        _statementCompiler.SetupDefaultHandlers(expressionCompiler);

        runtime.DeclareRuntime(module, context);
    }

    public void SetBasePath(string basePath)
    {
        _basePath = basePath;
    }

    public void Compile(List<Stmt> statements, ModuleLoader? moduleLoader = null)
    {
        var declCompiler = new LlvmDeclarationCompiler(_session, moduleLoader ?? (_basePath != null ? new ModuleLoader(_basePath) : null));
        
        // Pass 1: Declarations
        declCompiler.DeclareStatements(statements);

        // Pass 2: Compilation of bodies
        foreach (var stmt in statements)
        {
            var s = stmt;
            if (s is Stmt.Export e) s = e.Declaration;

            if (s is Stmt.Function or Stmt.Class)
            {
                _statementCompiler.Compile(stmt);
            }
        }

        // Pass 3: Global statements into main()
        CompileMain(statements);
    }

    private void CompileMain(List<Stmt> statements)
    {
        var mainFunc = _infrastructure.Module.GetNamedFunction("main");
        if (mainFunc.Handle != IntPtr.Zero) return;

        var i32Type = _infrastructure.Context.Int32Type;
        var mainFuncType = LLVMTypeRef.CreateFunction(i32Type, Array.Empty<LLVMTypeRef>());
        mainFunc = _infrastructure.Module.AddFunction("main", mainFuncType);
        var entry = mainFunc.AppendBasicBlock("entry");
        _infrastructure.Builder.PositionAtEnd(entry);

        // GC Init
        var gcInitFn = _infrastructure.Module.GetNamedFunction("badlang_gc_init");
        var dummyStack = _infrastructure.Builder.BuildAlloca(_infrastructure.Context.Int8Type, "gc_root");
        _infrastructure.Builder.BuildCall2(_session.Runtime.GetRuntimeType("badlang_gc_init"), gcInitFn, [dummyStack]);

        foreach (var stmt in statements)
        {
            var s = stmt;
            if (s is Stmt.Export e) s = e.Declaration;

            if (s is not Stmt.Function && s is not Stmt.Struct
                                       && s is not Stmt.Enum && s is not Stmt.Class && s is not Stmt.Import
                                       && s is not Stmt.Interface)
            {
                _statementCompiler.Compile(stmt);
            }
        }

        if (_infrastructure.Builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _infrastructure.Builder.BuildRet(LLVMValueRef.CreateConstInt(i32Type, 0));
    }

    public void WriteToFile(string path)
    {
        _infrastructure.Module.WriteBitcodeToFile(path);
    }

    public void WriteAssemblyToFile(string path)
    {
        _infrastructure.Module.PrintToFile(path);
    }

    public void Dispose()
    {
        _infrastructure.Dispose();
    }

    public void Verify()
    {
        if (!_infrastructure.Module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string error))
        {
            Console.WriteLine($"Verification failed: {error}");
        }
    }

    public void Dump()
    {
        _infrastructure.Module.Dump();
    }
}
