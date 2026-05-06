using BadLang.Backend.LLVM.Core;
using BadLang.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;


namespace BadLang.Backend.LLVM.Handlers;

public class LlvmExpressionCompiler(CompilationSession session) : IExpressionCompiler
{
    private readonly List<ExpressionHandler> _handlers = new();

    private void RegisterHandler(ExpressionHandler handler)
    {
        _handlers.Add(handler);
    }

    public void SetupDefaultHandlers(IStatementCompiler statementCompiler)
    {
        RegisterHandler(new ExpressionHandlers.LiteralHandler(session, this));
        RegisterHandler(new ExpressionHandlers.UnaryHandler(session, this));
        RegisterHandler(new ExpressionHandlers.GroupingHandler(session, this));
        RegisterHandler(new ExpressionHandlers.BinaryHandler(session, this));
        RegisterHandler(new ExpressionHandlers.VariableHandler(session, this));
        RegisterHandler(new ExpressionHandlers.LogicalHandler(session, this));
        RegisterHandler(new ExpressionHandlers.AssignHandler(session, this));
        RegisterHandler(new ExpressionHandlers.TernaryHandler(session, this));
        RegisterHandler(new ExpressionHandlers.IndexHandler(session, this));
        RegisterHandler(new ExpressionHandlers.ThisHandler(session, this));
        RegisterHandler(new ExpressionHandlers.LambdaHandler(session, this, statementCompiler));
        RegisterHandler(new ExpressionHandlers.InterpolatedStringHandler(session, this));
        RegisterHandler(new ExpressionHandlers.TypeOfHandler(session, this));
        RegisterHandler(new ExpressionHandlers.NameOfHandler(session, this));
        RegisterHandler(new ExpressionHandlers.ToStringHandler(session, this));
        RegisterHandler(new ExpressionHandlers.ToNumberHandler(session, this));
        RegisterHandler(new ExpressionHandlers.IsNullHandler(session, this));
        RegisterHandler(new ExpressionHandlers.AssertHandler(session, this));
        RegisterHandler(new ExpressionHandlers.PanicHandler(session, this));
        RegisterHandler(new ExpressionHandlers.ArrayLiteralHandler(session, this));
        RegisterHandler(new ExpressionHandlers.MapLiteralHandler(session, this));
        RegisterHandler(new ExpressionHandlers.GetHandler(session, this));
        RegisterHandler(new ExpressionHandlers.SetHandler(session, this));
        RegisterHandler(new ExpressionHandlers.MethodCallHandler(session, this));
        RegisterHandler(new ExpressionHandlers.SuperCallHandler(session, this));
        RegisterHandler(new ExpressionHandlers.PrintCallHandler(session, this));
        RegisterHandler(new ExpressionHandlers.CallHandler(session, this));
        RegisterHandler(new ExpressionHandlers.TypeCastHandler(session, this));
        RegisterHandler(new ExpressionHandlers.NewHandler(session, this));
        RegisterHandler(new ExpressionHandlers.NullCoalesceHandler(session, this));
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

public class LlvmStatementCompiler(CompilationSession session) : IStatementCompiler
{
    private readonly List<StatementHandler> _handlers = new();

    private void RegisterHandler(StatementHandler handler)
    {
        _handlers.Add(handler);
    }

    public void SetupDefaultHandlers(IExpressionCompiler expressionCompiler)
    {
        RegisterHandler(new StatementHandlers.ExpressionStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.BlockStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ReturnStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.VarStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.IfStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.WhileStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.DoWhileStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.BreakStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ContinueStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ForInStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.SwitchStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ThrowStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ConstStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.FunctionStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ClassStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.TryStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ExportStatementHandler(session, this, expressionCompiler));
        RegisterHandler(new StatementHandlers.ImportStatementHandler(session, this, expressionCompiler));
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
