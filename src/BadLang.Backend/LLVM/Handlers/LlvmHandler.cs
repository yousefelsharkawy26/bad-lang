using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
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

public abstract class LlvmHandler
{
    protected readonly CompilationSession Session;

    protected LlvmHandler(CompilationSession session)
    {
        Session = session;
    }
}

public abstract class ExpressionHandler : LlvmHandler
{
    protected readonly IExpressionCompiler ExpressionCompiler;
    protected ExpressionHandler(CompilationSession session, IExpressionCompiler expressionCompiler) : base(session) 
    { 
        ExpressionCompiler = expressionCompiler;
    }
    public abstract bool CanHandle(Expr expr);
    public abstract LLVMValueRef Compile(Expr expr);
}

public abstract class StatementHandler : LlvmHandler
{
    protected readonly IStatementCompiler StatementCompiler;
    protected readonly IExpressionCompiler ExpressionCompiler;
    protected StatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) : base(session) 
    { 
        StatementCompiler = statementCompiler;
        ExpressionCompiler = expressionCompiler;
    }
    public abstract bool CanHandle(Stmt stmt);
    public abstract void Compile(Stmt stmt);
}
