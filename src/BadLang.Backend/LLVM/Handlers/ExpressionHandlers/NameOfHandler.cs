using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class NameOfHandler : ExpressionHandler
{
    public NameOfHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.NameOf;

    public override LLVMValueRef Compile(Expr expr)
    {
        var nameOfExpr = (Expr.NameOf)expr;
        string name = string.Empty;

        if (nameOfExpr.Expr is Expr.Variable varExpr)
        {
            name = varExpr.Name.Lexeme;
        }
        else if (nameOfExpr.Expr is Expr.Get getExpr)
        {
            name = getExpr.Name.Lexeme;
        }
        else
        {
            throw new CompileError("nameof requires a variable or property access expression.", nameOfExpr.Keyword);
        }

        var builder = Session.Infrastructure.Builder;
        var module = Session.Infrastructure.Module;

        var cstr = builder.BuildGlobalStringPtr(name, "name_str");
        var strNew = module.GetNamedFunction("badlang_str_new");
        var strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_new"), strNew, new[] { cstr }, "name_str_obj");

        Session.LastExpressionType = "string";
        return Session.FromPtr(strObj);
    }
}
