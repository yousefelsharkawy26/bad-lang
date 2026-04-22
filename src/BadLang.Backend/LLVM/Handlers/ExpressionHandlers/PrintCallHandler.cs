using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Parser;
using LLVMSharp.Interop;
using System;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class PrintCallHandler : ExpressionHandler
{
    public PrintCallHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) 
    {
        return expr is Expr.Call call && call.Callee is Expr.Variable v && (v.Name.Lexeme == "print" || v.Name.Lexeme == "println");
    }

    public override LLVMValueRef Compile(Expr expr)
    {
        var callExpr = (Expr.Call)expr;
        var v = (Expr.Variable)callExpr.Callee;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        var printfType = Session.Runtime.PrintfType;
        var printf = module.GetNamedFunction("printf");

        LLVMValueRef lastArg = LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0);

        for (int i = 0; i < callExpr.Arguments.Count; i++)
        {
            var argExpr = callExpr.Arguments[i];
            var isInterpolated = argExpr is Expr.InterpolatedString;
            var arg = ExpressionCompiler.Compile(argExpr);
            lastArg = arg;

            if (isInterpolated || Session.LastExpressionType == "null") continue; 

            if (Session.LastExpressionType == "string")
            {
                var strPrint = module.GetNamedFunction("badlang_str_print");
                var strPrintType = Session.Runtime.GetRuntimeType("badlang_str_print");
                builder.BuildCall2(strPrintType, strPrint, new[] { Session.ToI64(Session.ToPtr(arg, Session.StringPtrType)) }, "");
                
                if (i < callExpr.Arguments.Count - 1)
                {
                    var formatStr = builder.BuildGlobalStringPtr(" ", "space");
                    builder.BuildCall2(printfType, printf, new[] { formatStr }, "");
                }
            }
            else if (Session.LastExpressionType == "any" || Session.LastExpressionType == null)
            {
                var printVal = module.GetNamedFunction("badlang_print_value");
                var printValType = Session.Runtime.GetRuntimeType("badlang_print_value");
                builder.BuildCall2(printValType, printVal, new[] { arg }, "");
                
                if (i < callExpr.Arguments.Count - 1)
                {
                    var formatStr = builder.BuildGlobalStringPtr(" ", "space");
                    builder.BuildCall2(printfType, printf, new[] { formatStr }, "");
                }
            }
            else
            {
                var formatStr = builder.BuildGlobalStringPtr(i < callExpr.Arguments.Count - 1 ? "%g " : "%g", "formatStr");
                builder.BuildCall2(printfType, printf, new[] { formatStr, Session.ToDouble(arg) }, "");
            }
        }

        if (v.Name.Lexeme == "println")
        {
            var nlFormat = builder.BuildGlobalStringPtr("\n", "fmt_nl");
            builder.BuildCall2(printfType, printf, new[] { nlFormat }, "");
        }

        return lastArg;
    }
}
