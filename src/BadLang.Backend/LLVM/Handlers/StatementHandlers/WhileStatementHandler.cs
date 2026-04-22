using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class WhileStatementHandler : StatementHandler
{
    public WhileStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.While;

    public override void Compile(Stmt stmt)
    {
        var whileStmt = (Stmt.While)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        var func = builder.InsertBlock.Parent;
        var condBb = func.AppendBasicBlock("whilecond");
        var bodyBb = func.AppendBasicBlock("whilebody");
        var endBb = func.AppendBasicBlock("whileend");

        Session.BreakStack.Push(endBb);
        Session.ContinueStack.Push(condBb);

        builder.BuildBr(condBb);

        builder.PositionAtEnd(condBb);
        var cond = Session.ToDouble(ExpressionCompiler.Compile(whileStmt.Condition));
        var isTrue = builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond, LLVMValueRef.CreateConstReal(ctx.DoubleType, 0), "whilecondtmp");
        builder.BuildCondBr(isTrue, bodyBb, endBb);

        builder.PositionAtEnd(bodyBb);
        StatementCompiler.Compile(whileStmt.Body);
        
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
        {
            builder.BuildBr(condBb);
        }

        builder.PositionAtEnd(endBb);
        Session.BreakStack.Pop();
        Session.ContinueStack.Pop();
    }
}
