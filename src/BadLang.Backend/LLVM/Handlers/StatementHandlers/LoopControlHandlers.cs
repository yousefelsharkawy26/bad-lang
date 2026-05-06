using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class BreakStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Break;

    public override void Compile(Stmt stmt)
    {
        if (Session.BreakStack.Count == 0) throw new Exception("Break outside of loop");
        var builder = Session.Infrastructure.Builder;
        builder.BuildBr(Session.BreakStack.Peek());
    }
}

public class ContinueStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Continue;

    public override void Compile(Stmt stmt)
    {
        if (Session.ContinueStack.Count == 0) throw new Exception("Continue outside of loop");
        var builder = Session.Infrastructure.Builder;
        builder.BuildBr(Session.ContinueStack.Peek());
    }
}
