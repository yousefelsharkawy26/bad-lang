using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ExpressionStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Expression;

    public override void Compile(Stmt stmt)
    {
        var exprStmt = (Stmt.Expression)stmt;
        ExpressionCompiler.Compile(exprStmt.Expr);
    }
}
