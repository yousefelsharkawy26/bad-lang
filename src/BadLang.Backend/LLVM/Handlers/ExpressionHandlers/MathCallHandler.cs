using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class MathCallHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    private static readonly HashSet<string> MathFunctions =
        ["pow", "sqrt", "sin", "cos", "tan", "log", "exp", "floor", "ceil", "round", "abs"];

    public override bool CanHandle(Expr expr) 
    {
        return expr is Expr.Call { Callee: Expr.Variable v } && MathFunctions.Contains(v.Name.Lexeme);
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
            var powType = LLVMTypeRef.CreateFunction(ctx.DoubleType, [ctx.DoubleType, ctx.DoubleType]);
            if (powFunc.Handle == IntPtr.Zero) powFunc = module.AddFunction("pow", powType);

            Session.LastExpressionType = "number";
            return Session.ToI64(builder.BuildCall2(powType, powFunc, new[] { @base, exp }, "powtmp"));
        }
        else
        {
            var val = Session.ToDouble(ExpressionCompiler.Compile(callExpr.Arguments[0]));
            var func = module.GetNamedFunction(name);
            var type = LLVMTypeRef.CreateFunction(ctx.DoubleType, [ctx.DoubleType]);
            if (func.Handle == IntPtr.Zero) func = module.AddFunction(name, type);

            Session.LastExpressionType = "number";
            return Session.ToI64(builder.BuildCall2(type, func, new[] { val }, name + "tmp"));
        }
    }
}
