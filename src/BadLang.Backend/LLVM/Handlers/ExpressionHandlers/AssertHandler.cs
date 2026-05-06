using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class AssertHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.AssertExpr;

    public override LLVMValueRef Compile(Expr expr)
    {
        var assertExpr = (Expr.AssertExpr)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;
        var function = builder.InsertBlock.Parent;

        // Evaluate condition
        var condVal = ExpressionCompiler.Compile(assertExpr.Condition);
        var condBool = builder.BuildTrunc(condVal, ctx.Int1Type, "assert_cond");

        // Create blocks: fail and pass
        var failBlock = function.AppendBasicBlock("assert_fail");
        var passBlock = function.AppendBasicBlock("assert_pass");

        builder.BuildCondBr(condBool, passBlock, failBlock);

        // Fail block: print message and abort
        builder.PositionAtEnd(failBlock);
        LLVMValueRef msgStr;
        if (assertExpr.Message != null)
        {
            var msgVal = ExpressionCompiler.Compile(assertExpr.Message);
            var msgPtr = Session.ToPtr(msgVal, Session.StringPtrType);
            var rtFn = module.GetNamedFunction("badlang_str_get_cstr");
            var cstr = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_get_cstr"), rtFn, new[] { msgPtr }, "assert_msg");
            msgStr = cstr;
        }
        else
        {
            msgStr = builder.BuildGlobalStringPtr("Assertion failed", "assert_default_msg");
        }
        // Call badlang_panic(cstr)
        var panicFn = module.GetNamedFunction("badlang_panic");
        builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_panic"), panicFn, [msgStr]);
        builder.BuildUnreachable();

        // Continue in pass block
        builder.PositionAtEnd(passBlock);

        Session.LastExpressionType = "void";
        return LLVMValueRef.CreateConstInt(ctx.Int64Type, 0);
    }
}
