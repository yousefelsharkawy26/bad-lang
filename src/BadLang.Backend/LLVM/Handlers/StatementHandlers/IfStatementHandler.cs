using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class IfStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.If;

    public override void Compile(Stmt stmt)
    {
        var ifStmt = (Stmt.If)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        var cond = Session.ToDouble(ExpressionCompiler.Compile(ifStmt.Condition));
        var isTrue = builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond, LLVMValueRef.CreateConstReal(ctx.DoubleType, 0), "ifcond");

        var func = builder.InsertBlock.Parent;
        var thenBb = func.AppendBasicBlock("then");
        var elseBb = func.AppendBasicBlock("else");
        var mergeBb = func.AppendBasicBlock("ifcont");

        builder.BuildCondBr(isTrue, thenBb, elseBb);

        // Then branch
        builder.PositionAtEnd(thenBb);
        StatementCompiler.Compile(ifStmt.ThenBranch);
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) builder.BuildBr(mergeBb);

        // Else branch
        builder.PositionAtEnd(elseBb);
        if (ifStmt.ElseBranch != null) StatementCompiler.Compile(ifStmt.ElseBranch);
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) builder.BuildBr(mergeBb);

        // Merge
        builder.PositionAtEnd(mergeBb);
    }
}
