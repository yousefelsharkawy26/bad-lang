using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class ToNumberHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.ToNumberExpr;

    public override LLVMValueRef Compile(Expr expr)
    {
        var toNumExpr = (Expr.ToNumberExpr)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        var val = ExpressionCompiler.Compile(toNumExpr.Expr);
        var valType = Session.LastExpressionType;

        LLVMValueRef result;
        if (valType == "number")
        {
            result = Session.ToDouble(val);
        }
        else if (valType == "bool")
        {
            // true -> 1.0, false -> 0.0
            var truncated = builder.BuildTrunc(val, ctx.Int1Type, "bool_trunc");
            result = builder.BuildUIToFP(truncated, ctx.DoubleType, "bool_to_num");
        }
        else if (valType == "string")
        {
            // Call runtime: badlang_str_to_num(str) -> double
            var rtFn = module.GetNamedFunction("badlang_str_to_num");
            var strPtr = Session.ToPtr(val, Session.StringPtrType);
            result = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_to_num"), rtFn, new[] { strPtr }, "str_to_num");
        }
        else
        {
            // Fallback: return 0.0
            result = LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0);
        }

        Session.LastExpressionType = "number";
        return Session.FromDouble(result);
    }
}
