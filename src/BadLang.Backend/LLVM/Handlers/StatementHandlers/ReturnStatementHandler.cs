using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ReturnStatementHandler : StatementHandler
{
    public ReturnStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Return;

    public override void Compile(Stmt stmt)
    {
        var retStmt = (Stmt.Return)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        if (retStmt.Value != null)
        {
            var val = ExpressionCompiler.Compile(retStmt.Value);
            builder.BuildRet(val);
        }
        else
        {
            builder.BuildRet(LLVMValueRef.CreateConstInt(ctx.Int64Type, 0));
        }
    }
}
