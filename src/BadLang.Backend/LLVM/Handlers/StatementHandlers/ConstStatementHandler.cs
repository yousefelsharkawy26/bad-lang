using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ConstStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Const;

    public override void Compile(Stmt stmt)
    {
        var constStmt = (Stmt.Const)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        var initialVal = Session.ToI64(ExpressionCompiler.Compile(constStmt.Initializer));
        var ptr = builder.BuildAlloca(ctx.Int64Type, constStmt.Name.Lexeme);
        builder.BuildStore(initialVal, ptr);
        
        Session.Symbols.DefineVariable(constStmt.Name.Lexeme, ptr, Session.LastExpressionType, true);
    }
}
