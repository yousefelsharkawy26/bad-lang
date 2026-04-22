using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class TernaryHandler : ExpressionHandler
{
    public TernaryHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Ternary;

    public override LLVMValueRef Compile(Expr expr)
    {
        var ternary = (Expr.Ternary)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        var cond = Session.ToDouble(ExpressionCompiler.Compile(ternary.Condition));
        var isTrue = builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond, LLVMValueRef.CreateConstReal(ctx.DoubleType, 0), "ternarycond");

        var func = builder.InsertBlock.Parent;
        var thenBb = func.AppendBasicBlock("ternary_then");
        var elseBb = func.AppendBasicBlock("ternary_else");
        var mergeBb = func.AppendBasicBlock("ternary_merge");

        builder.BuildCondBr(isTrue, thenBb, elseBb);

        builder.PositionAtEnd(thenBb);
        var thenVal = ExpressionCompiler.Compile(ternary.ThenBranch);
        var thenType = Session.LastExpressionType;
        var thenFinalBb = builder.InsertBlock;
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) 
            builder.BuildBr(mergeBb);

        builder.PositionAtEnd(elseBb);
        var elseVal = ExpressionCompiler.Compile(ternary.ElseBranch);
        var elseType = Session.LastExpressionType;
        var elseFinalBb = builder.InsertBlock;
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) 
            builder.BuildBr(mergeBb);

        builder.PositionAtEnd(mergeBb);
        var phi = builder.BuildPhi(ctx.Int64Type, "ternary_res");
        phi.AddIncoming(new[] { thenVal, elseVal }, new[] { thenFinalBb, elseFinalBb }, 2);
        Session.LastExpressionType = (thenType == elseType) ? thenType : null;
        return phi;
    }
}
