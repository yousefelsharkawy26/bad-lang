using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class TypeCastHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.TypeCast;

    public override LLVMValueRef Compile(Expr expr)
    {
        var castExpr = (Expr.TypeCast)expr;
        var val = ExpressionCompiler.Compile(castExpr.Expr);
        
        // In this simple LLVM backend, we mostly handle casts at runtime or, they are i64 already.
        // For now, we'll just pass it through and update the type metadata.
        // A more advanced implementation would verify the cast or perform conversions.
        
        Session.LastExpressionType = GetTypeName(castExpr.TargetType);
        return val;
    }

    private string GetTypeName(TypeNode node)
    {
        return node switch
        {
            TypeNode.Primitive p => p.Token.Lexeme,
            TypeNode.UserDefined u => u.Name.Lexeme,
            TypeNode.Array a => GetTypeName(a.BaseType) + "[]",
            _ => "unknown"
        };
    }
}
