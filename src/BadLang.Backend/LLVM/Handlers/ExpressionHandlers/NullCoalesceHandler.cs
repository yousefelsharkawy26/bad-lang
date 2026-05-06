using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class NullCoalesceHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
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
        var thenBb = function.AppendBasicBlock("nc_not_null");
        var elseBb = function.AppendBasicBlock("nc_null");
        var mergeBb = function.AppendBasicBlock("nc_merge");

        builder.BuildCondBr(isNotNull, thenBb, elseBb);

        builder.PositionAtEnd(thenBb);
        var thenVal = left;
        builder.BuildBr(mergeBb);
        thenBb = builder.InsertBlock;

        builder.PositionAtEnd(elseBb);
        var elseVal = ExpressionCompiler.Compile(ncExpr.Right);
        var rightType = Session.LastExpressionType;
        builder.BuildBr(mergeBb);
        elseBb = builder.InsertBlock;

        builder.PositionAtEnd(mergeBb);
        var phi = builder.BuildPhi(ctx.Int64Type, "nc_phi");
        phi.AddIncoming(new[] { thenVal, elseVal }, new[] { thenBb, elseBb }, 2);

        Session.LastExpressionType = leftType ?? rightType;
        return phi;
    }
}
