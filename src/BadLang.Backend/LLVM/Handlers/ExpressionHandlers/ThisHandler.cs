using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class ThisHandler : ExpressionHandler
{
    public ThisHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.This;

    public override LLVMValueRef Compile(Expr expr)
    {
        var thisExpr = (Expr.This)expr;
        if (Session.CurrentThisPtr == null)
            throw new CompileError("'this' used outside a class method", thisExpr.Keyword);
            
        Session.LastExpressionType = Session.CurrentClassName;
        return Session.FromPtr(Session.CurrentThisPtr.Value);
    }
}
