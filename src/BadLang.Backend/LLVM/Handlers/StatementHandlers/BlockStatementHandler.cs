using BadLang.Backend.LLVM.Core;
using BadLang.Parser;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class BlockStatementHandler : StatementHandler
{
    public BlockStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Block;

    public override void Compile(Stmt stmt)
    {
        var block = (Stmt.Block)stmt;
        Session.Symbols.PushScope();
        foreach (var s in block.Statements)
        {
            StatementCompiler.Compile(s);
        }
        Session.Symbols.PopScope();
    }
}
