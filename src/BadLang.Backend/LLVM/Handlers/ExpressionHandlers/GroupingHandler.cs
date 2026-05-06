using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class GroupingHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Grouping;

    public override LLVMValueRef Compile(Expr expr)
    {
        var grouping = (Expr.Grouping)expr;
        return ExpressionCompiler.Compile(grouping.Expr);
    }
}
