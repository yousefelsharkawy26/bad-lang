using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class LogicalHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Logical;

    public override LLVMValueRef Compile(Expr expr)
    {
        var logical = (Expr.Logical)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        var left = ExpressionCompiler.Compile(logical.Left);
        var isLeftTrue = builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, Session.ToDouble(left), LLVMValueRef.CreateConstReal(ctx.DoubleType, 0), "logcond");

        var func = builder.InsertBlock.Parent;
        var rightBb = func.AppendBasicBlock("log_right");
        var mergeBb = func.AppendBasicBlock("log_merge");

        if (logical.Operator.Type == TokenType.Or)
        {
            builder.BuildCondBr(isLeftTrue, mergeBb, rightBb);
        }
        else // TokenType.And
        {
            builder.BuildCondBr(isLeftTrue, rightBb, mergeBb);
        }

        var leftBb = builder.InsertBlock;
        builder.PositionAtEnd(rightBb);
        var right = ExpressionCompiler.Compile(logical.Right);
        builder.BuildBr(mergeBb);
        var rightFinalBb = builder.InsertBlock;

        builder.PositionAtEnd(mergeBb);
        var phi = builder.BuildPhi(ctx.Int64Type, "logphi");
        phi.AddIncoming(new[] { left, right }, new[] { leftBb, rightFinalBb }, 2);

        Session.LastExpressionType = "bool";
        return phi;
    }
}
