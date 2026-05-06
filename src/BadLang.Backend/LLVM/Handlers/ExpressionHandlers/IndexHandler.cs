using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class IndexHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Index;

    public override LLVMValueRef Compile(Expr expr)
    {
        var indexExpr = (Expr.Index)expr;
        var target = ExpressionCompiler.Compile(indexExpr.Target);
        var targetType = Session.LastExpressionType;
        var indexVal = ExpressionCompiler.Compile(indexExpr.IndexValue);

        if (targetType == "string")
        {
            var rtFn = Session.Infrastructure.Module.GetNamedFunction("badlang_str_at");
            Session.LastExpressionType = "string";
            return Session.FromPtr(Session.Infrastructure.Builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_at"), rtFn, new[] { Session.ToPtr(target, Session.StringPtrType), Session.ToIndex(indexVal) }, "strat"));
        }
        else if (targetType == "list")
        {
            var rtFn = Session.Infrastructure.Module.GetNamedFunction("badlang_list_get");
            Session.LastExpressionType = "any"; 
            return Session.Infrastructure.Builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_get"), rtFn, new[] { Session.ToPtr(target, Session.VoidPtrType), Session.ToIndex(indexVal) }, "lget");
        }
        else if (targetType == "map")
        {
            var rtFn = Session.Infrastructure.Module.GetNamedFunction("badlang_map_get");
            Session.LastExpressionType = "any";
            return Session.Infrastructure.Builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_get"), rtFn, new[] { Session.ToPtr(target, Session.VoidPtrType), Session.ToI64(indexVal) }, "mget");
        }
        else
        {
            var targetPtr = Session.ToPtr(target, LLVMTypeRef.CreatePointer(Session.Infrastructure.Context.DoubleType, 0));
            var indexInt = Session.ToIndex(indexVal);
            var actualIndex = Session.Infrastructure.Builder.BuildAdd(indexInt, LLVMValueRef.CreateConstInt(Session.Infrastructure.Context.Int64Type, 1), "idx_offset");
            var elementPtr = Session.Infrastructure.Builder.BuildGEP2(Session.Infrastructure.Context.DoubleType, targetPtr, new[] { actualIndex }, "ptrtmp");
            return Session.ToI64(Session.Infrastructure.Builder.BuildLoad2(Session.Infrastructure.Context.DoubleType, elementPtr, "elementtmp"));
        }
    }
}
