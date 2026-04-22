using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class NullCoalesceHandler : ExpressionHandler
{
    public NullCoalesceHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.NullCoalesce;

    public override LLVMValueRef Compile(Expr expr)
    {
        var ncExpr = (Expr.NullCoalesce)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        
        var left = ExpressionCompiler.Compile(ncExpr.Left);
        var leftType = Session.LastExpressionType;
        
        var isNotNull = builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, Session.ToDouble(left), LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0), "is_not_null");

        var function = builder.InsertBlock.Parent;
        var thenBB = function.AppendBasicBlock("nc_not_null");
        var elseBB = function.AppendBasicBlock("nc_null");
        var mergeBB = function.AppendBasicBlock("nc_merge");

        builder.BuildCondBr(isNotNull, thenBB, elseBB);

        builder.PositionAtEnd(thenBB);
        var thenVal = left;
        builder.BuildBr(mergeBB);
        thenBB = builder.InsertBlock;

        builder.PositionAtEnd(elseBB);
        var elseVal = ExpressionCompiler.Compile(ncExpr.Right);
        var rightType = Session.LastExpressionType;
        builder.BuildBr(mergeBB);
        elseBB = builder.InsertBlock;

        builder.PositionAtEnd(mergeBB);
        var phi = builder.BuildPhi(ctx.Int64Type, "nc_phi");
        phi.AddIncoming(new[] { thenVal, elseVal }, new[] { thenBB, elseBB }, 2);

        Session.LastExpressionType = leftType ?? rightType;
        return phi;
    }
}
