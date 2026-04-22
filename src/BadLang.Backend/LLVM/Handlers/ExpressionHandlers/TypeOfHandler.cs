using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class TypeOfHandler : ExpressionHandler
{
    public TypeOfHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.TypeOf;

    public override LLVMValueRef Compile(Expr expr)
    {
        var typeOfExpr = (Expr.TypeOf)expr;
        
        // Compile the target expression to get its type populated in the session
        ExpressionCompiler.Compile(typeOfExpr.Expr);
        var exprType = Session.LastExpressionType ?? "unknown";

        var builder = Session.Infrastructure.Builder;
        var module = Session.Infrastructure.Module;

        // Build string representation of the type
        var cstr = builder.BuildGlobalStringPtr(exprType, "type_str");
        var strNew = module.GetNamedFunction("badlang_str_new");
        var strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_new"), strNew, new[] { cstr }, "type_str_obj");

        Session.LastExpressionType = "string";
        return Session.FromPtr(strObj);
    }
}
