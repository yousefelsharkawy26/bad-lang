using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class BreakStatementHandler : StatementHandler
{
    public BreakStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Break;

    public override void Compile(Stmt stmt)
    {
        if (Session.BreakStack.Count == 0) throw new System.Exception("Break outside of loop");
        var builder = Session.Infrastructure.Builder;
        builder.BuildBr(Session.BreakStack.Peek());
    }
}

public class ContinueStatementHandler : StatementHandler
{
    public ContinueStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Continue;

    public override void Compile(Stmt stmt)
    {
        if (Session.ContinueStack.Count == 0) throw new System.Exception("Continue outside of loop");
        var builder = Session.Infrastructure.Builder;
        builder.BuildBr(Session.ContinueStack.Peek());
    }
}
