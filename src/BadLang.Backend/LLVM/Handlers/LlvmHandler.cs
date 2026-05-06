using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers;

public interface IExpressionCompiler
{
    LLVMValueRef Compile(Expr expr);
}

public interface IStatementCompiler
{
    void Compile(Stmt stmt);
}

public abstract class LlvmHandler(CompilationSession session)
{
    protected readonly CompilationSession Session = session;
}

public abstract class ExpressionHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : LlvmHandler(session)
{
    protected readonly IExpressionCompiler ExpressionCompiler = expressionCompiler;
    public abstract bool CanHandle(Expr expr);
    public abstract LLVMValueRef Compile(Expr expr);
}

public abstract class StatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : LlvmHandler(session)
{
    protected readonly IStatementCompiler StatementCompiler = statementCompiler;
    protected readonly IExpressionCompiler ExpressionCompiler = expressionCompiler;
    public abstract bool CanHandle(Stmt stmt);
    public abstract void Compile(Stmt stmt);
}
