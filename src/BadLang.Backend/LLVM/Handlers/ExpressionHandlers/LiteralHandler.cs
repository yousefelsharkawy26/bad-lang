using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class LiteralHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Literal;

    public override LLVMValueRef Compile(Expr expr)
    {
        var literal = (Expr.Literal)expr;
        var ctx = Session.Infrastructure.Context;
        var builder = Session.Infrastructure.Builder;

        if (literal.Value is double d)
        {
            Session.LastExpressionType = "number";
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, d));
        }
        if (literal.Value is string s)
        {
            Session.LastExpressionType = "string";
            var cstr = builder.BuildGlobalStringPtr(s, "str");
            
            var strNew = Session.Infrastructure.Module.GetNamedFunction("badlang_str_new");
            var strNewType = Session.Runtime.GetRuntimeType("badlang_str_new");
            var res = builder.BuildCall2(strNewType, strNew, new[] { cstr }, "str_obj");
            return Session.FromPtr(res);

        }
        if (literal.Value is bool b)
        {
            Session.LastExpressionType = "bool";
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, b ? 1.0 : 0.0));
        }

        Session.LastExpressionType = "null";
        return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0));
    }
}
