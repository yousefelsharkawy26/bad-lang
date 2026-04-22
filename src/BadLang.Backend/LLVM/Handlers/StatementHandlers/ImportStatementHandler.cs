using BadLang.Backend.LLVM.Core;
using BadLang.Parser;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ImportStatementHandler : StatementHandler
{
    public ImportStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Import;

    public override void Compile(Stmt stmt)
    {
        // Imports are handled by the Declaration pass.
    }
}
