using BadLang.Backend.LLVM.Core;
using BadLang.Core;
using BadLang.Parser;
using LLVMSharp.Interop;


namespace BadLang.Backend.LLVM.Handlers;

public class LlvmExpressionCompiler : IExpressionCompiler
{
    private readonly List<ExpressionHandler> _handlers = new();
    private readonly CompilationSession _session;

    public LlvmExpressionCompiler(CompilationSession session)
    {
        _session = session;
    }

    public void RegisterHandler(ExpressionHandler handler)
    {
        _handlers.Add(handler);
    }

    public void SetupDefaultHandlers(IStatementCompiler statementCompiler)
    {
        RegisterHandler(new ExpressionHandlers.LiteralHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.UnaryHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.GroupingHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.BinaryHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.VariableHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.LogicalHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.AssignHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.TernaryHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.IndexHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.ThisHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.LambdaHandler(_session, this, statementCompiler));
        RegisterHandler(new ExpressionHandlers.InterpolatedStringHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.TypeOfHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.NameOfHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.ToStringHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.ToNumberHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.IsNullHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.AssertHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.PanicHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.ArrayLiteralHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.MapLiteralHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.GetHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.SetHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.MethodCallHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.SuperCallHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.PrintCallHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.CallHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.TypeCastHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.NewHandler(_session, this));
        RegisterHandler(new ExpressionHandlers.NullCoalesceHandler(_session, this));
    }

    public LLVMValueRef Compile(Expr expr)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(expr));
        if (handler != null)
        {
            return handler.Compile(expr);
        }

        throw new CompileError($"No handler found for expression type: {expr.GetType().Name}");
    }
}

public class LlvmStatementCompiler : IStatementCompiler
{
    private readonly List<StatementHandler> _handlers = new();
    private readonly CompilationSession _session;

    public LlvmStatementCompiler(CompilationSession session)
    {
        _session = session;
    }

    public void RegisterHandler(StatementHandler handler)
    {
        _handlers.Add(handler);
    }

    public void SetupDefaultHandlers(IExpressionCompiler expressionCompiler)
    {
        RegisterHandler(new StatementHandlers.ExpressionStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.BlockStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ReturnStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.VarStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.IfStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.WhileStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.DoWhileStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.BreakStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ContinueStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ForInStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.SwitchStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ThrowStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ConstStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.FunctionStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ClassStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.TryStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ExportStatementHandler(_session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ImportStatementHandler(_session, this, expressionCompiler));
    }

    public void Compile(Stmt stmt)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(stmt));
        if (handler != null)
        {
            handler.Compile(stmt);
            return;
        }

        throw new CompileError($"No handler found for statement type: {stmt.GetType().Name}");
    }
}
