using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class SuperCallHandler : ExpressionHandler
{
    public SuperCallHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Call call && call.Callee is Expr.Super;

    public override LLVMValueRef Compile(Expr expr)
    {
        var callExpr = (Expr.Call)expr;
        var superExpr = (Expr.Super)callExpr.Callee;
        var builder = Session.Infrastructure.Builder;
        var module = Session.Infrastructure.Module;

        if (Session.CurrentClassName == null)
            throw new CompileError("'super' can only be used inside a class method", superExpr.Keyword);

        if (!Session.Symbols.TryGetType(Session.CurrentClassName, out var classInfo))
             throw new CompileError($"Internal error: Current class '{Session.CurrentClassName}' not found in symbol table", superExpr.Keyword);

        if (classInfo.Parent == null)
            throw new CompileError($"Class '{Session.CurrentClassName}' has no parent class", superExpr.Keyword);

        string parentName = classInfo.Parent;
        string methodName = superExpr.Method.Lexeme;
        string mangledName = $"{parentName}__{methodName}";

        if (!Session.Symbols.TryGetFunction(mangledName, out var funcInfo))
            throw new CompileError($"Parent class '{parentName}' has no method '{methodName}'", superExpr.Method);

        var func = module.GetNamedFunction(mangledName);
        var thisVal = Session.Symbols.GetVariable("this").Value;
        var thisRaw = Session.ToPtr(builder.BuildLoad2(Session.Infrastructure.Context.Int64Type, thisVal, "this_load"), Session.VoidPtrType);

        var args = new LLVMValueRef[callExpr.Arguments.Count + 1];
        args[0] = thisRaw;
        for (int i = 0; i < callExpr.Arguments.Count; i++)
            args[i + 1] = ExpressionCompiler.Compile(callExpr.Arguments[i]);

        Session.LastExpressionType = funcInfo.ReturnTypeName;
        return Session.ToI64(builder.BuildCall2(funcInfo.Type, func, args, "super_call"));
    }
}
