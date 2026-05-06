using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class MapLiteralHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.MapLiteral;

    public override LLVMValueRef Compile(Expr expr)
    {
        var mapLiteral = (Expr.MapLiteral)expr;
        var rtNew = Session.Infrastructure.Module.GetNamedFunction("badlang_map_new");
        var mapVal = Session.Infrastructure.Builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_new"), rtNew, Array.Empty<LLVMValueRef>(), "map_new");
        var rtSet = Session.Infrastructure.Module.GetNamedFunction("badlang_map_set");
        foreach (var entry in mapLiteral.Entries)
        {
            var kVal = Session.ToI64(ExpressionCompiler.Compile(entry.Key));
            var vVal = Session.ToI64(ExpressionCompiler.Compile(entry.Value));
            Session.Infrastructure.Builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_set"), rtSet, [mapVal, kVal, vVal
            ]);
        }
        Session.LastExpressionType = "map";
        return Session.FromPtr(mapVal);
    }
}
