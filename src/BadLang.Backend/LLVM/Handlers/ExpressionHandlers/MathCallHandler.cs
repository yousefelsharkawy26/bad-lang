using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Parser;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class MathCallHandler : ExpressionHandler
{
    private static readonly HashSet<string> MathFunctions = new()
    {
        "pow", "sqrt", "sin", "cos", "tan", "log", "exp", "floor", "ceil", "round", "abs"
    };

    public MathCallHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) 
    {
        return expr is Expr.Call call && call.Callee is Expr.Variable v && MathFunctions.Contains(v.Name.Lexeme);
    }

    public override LLVMValueRef Compile(Expr expr)
    {
        var callExpr = (Expr.Call)expr;
        var v = (Expr.Variable)callExpr.Callee;
        var name = v.Name.Lexeme;
        if (name == "abs") name = "fabs";

        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        if (name == "pow")
        {
            var @base = Session.ToDouble(ExpressionCompiler.Compile(callExpr.Arguments[0]));
            var exp = Session.ToDouble(ExpressionCompiler.Compile(callExpr.Arguments[1]));

            var powFunc = module.GetNamedFunction("pow");
            var powType = LLVMTypeRef.CreateFunction(ctx.DoubleType, new[] { ctx.DoubleType, ctx.DoubleType });
            if (powFunc.Handle == IntPtr.Zero) powFunc = module.AddFunction("pow", powType);

            Session.LastExpressionType = "number";
            return Session.ToI64(builder.BuildCall2(powType, powFunc, new[] { @base, exp }, "powtmp"));
        }
        else
        {
            var val = Session.ToDouble(ExpressionCompiler.Compile(callExpr.Arguments[0]));
            var func = module.GetNamedFunction(name);
            var type = LLVMTypeRef.CreateFunction(ctx.DoubleType, new[] { ctx.DoubleType });
            if (func.Handle == IntPtr.Zero) func = module.AddFunction(name, type);

            Session.LastExpressionType = "number";
            return Session.ToI64(builder.BuildCall2(type, func, new[] { val }, name + "tmp"));
        }
    }
}
