using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class UnaryHandler : ExpressionHandler
{
    public UnaryHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Unary;

    public override LLVMValueRef Compile(Expr expr)
    {
        var unary = (Expr.Unary)expr;
        var right = ExpressionCompiler.Compile(unary.Right);
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        return unary.Operator.Type switch
        {
            TokenType.Minus => Session.ToI64(builder.BuildFNeg(Session.ToDouble(right), "negtmp")),
            TokenType.BangLog => Session.ToI64(builder.BuildXor(Session.ToI64(right), LLVMValueRef.CreateConstInt(ctx.Int64Type, 1), "nottmp")),
            _ => throw new CompileError($"Unsupported unary operator: {unary.Operator.Lexeme}")
        };
    }
}
