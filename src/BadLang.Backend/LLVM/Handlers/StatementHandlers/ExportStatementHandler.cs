using BadLang.Backend.LLVM.Core;
using BadLang.Parser;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ExportStatementHandler : StatementHandler
{
    public ExportStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Export;

    public override void Compile(Stmt stmt)
    {
        var exportStmt = (Stmt.Export)stmt;
        StatementCompiler.Compile(exportStmt.Declaration);
    }
}
