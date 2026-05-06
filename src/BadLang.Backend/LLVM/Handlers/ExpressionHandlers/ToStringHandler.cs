using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class ToStringHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.ToStringExpr;

    public override LLVMValueRef Compile(Expr expr)
    {
        var toStringExpr = (Expr.ToStringExpr)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        var val = ExpressionCompiler.Compile(toStringExpr.Expr);
        var valType = Session.LastExpressionType;

        LLVMValueRef strObj;
        if (valType == "string")
        {
            strObj = Session.ToPtr(val, Session.StringPtrType);
        }
        else if (valType == "number")
        {
            var rtFn = module.GetNamedFunction("badlang_num_to_str");
            strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_num_to_str"), rtFn, new[] { Session.ToDouble(val) }, "numstr");
        }
        else if (valType == "bool")
        {
            var rtFn = module.GetNamedFunction("badlang_bool_to_str");
            strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_bool_to_str"), rtFn, new[] { builder.BuildTrunc(val, ctx.Int1Type, "bool_trunc") }, "boolstr");
        }
        else
        {
            var rtFn = module.GetNamedFunction("badlang_any_to_str");
            strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_any_to_str"), rtFn, new[] { val }, "anystr");
        }

        Session.LastExpressionType = "string";
        return Session.FromPtr(strObj);
    }
}
