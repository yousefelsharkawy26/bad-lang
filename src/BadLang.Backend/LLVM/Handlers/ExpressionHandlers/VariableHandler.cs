using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class VariableHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Variable;

    public override LLVMValueRef Compile(Expr expr)
    {
        var variable = (Expr.Variable)expr;
        var name = variable.Name.Lexeme;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        
        var info = Session.Symbols.GetVariable(name);
        var val = info.Value;
        Session.LastExpressionType = info.TypeName ?? "any";
        
        return builder.BuildLoad2(ctx.Int64Type, val, name);
    }
}
