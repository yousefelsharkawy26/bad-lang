using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class IsNullHandler : ExpressionHandler
{
    public IsNullHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.IsNullExpr;

    public override LLVMValueRef Compile(Expr expr)
    {
        var isNullExpr = (Expr.IsNullExpr)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        var val = ExpressionCompiler.Compile(isNullExpr.Expr);
        var valType = Session.LastExpressionType;

        LLVMValueRef boolResult;
        if (valType == "null")
        {
            // Statically known to be null
            boolResult = LLVMValueRef.CreateConstInt(ctx.Int1Type, 1, false);
        }
        else if (valType == "number" || valType == "bool")
        {
            // Primitives are never null
            boolResult = LLVMValueRef.CreateConstInt(ctx.Int1Type, 0, false);
        }
        else
        {
            // Pointer types: compare to null
            var ptrVal = Session.ToPtr(val, Session.StringPtrType);
            var nullPtr = LLVMValueRef.CreateConstNull(Session.StringPtrType);
            boolResult = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, ptrVal, nullPtr, "is_null");
        }

        // Extend i1 to i64 to match BadLang bool representation
        var extended = builder.BuildZExt(boolResult, ctx.Int64Type, "bool_ext");
        Session.LastExpressionType = "bool";
        return extended;
    }
}
