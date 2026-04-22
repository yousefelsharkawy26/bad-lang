using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class PanicHandler : ExpressionHandler
{
    public PanicHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.PanicExpr;

    public override LLVMValueRef Compile(Expr expr)
    {
        var panicExpr = (Expr.PanicExpr)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        // Evaluate the message
        var msgVal = ExpressionCompiler.Compile(panicExpr.Message);
        var msgPtr = Session.ToPtr(msgVal, Session.StringPtrType);

        // Get the C string from the BadLang string
        var getCstrFn = module.GetNamedFunction("badlang_str_get_cstr");
        var cstr = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_get_cstr"), getCstrFn, new[] { msgPtr }, "panic_msg");

        // Call badlang_panic(cstr)
        var panicFn = module.GetNamedFunction("badlang_panic");
        builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_panic"), panicFn, new[] { cstr }, "");
        builder.BuildUnreachable();

        Session.LastExpressionType = "void";
        return LLVMValueRef.CreateConstInt(ctx.Int64Type, 0, false);
    }
}
