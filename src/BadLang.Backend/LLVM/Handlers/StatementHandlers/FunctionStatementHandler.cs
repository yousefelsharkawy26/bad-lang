using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class FunctionStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Function;

    public override void Compile(Stmt stmt)
    {
        var funcStmt = (Stmt.Function)stmt;
        var fullName = string.IsNullOrEmpty(Session.CurrentNamespace) ? funcStmt.Name.Lexeme : $"{Session.CurrentNamespace}.{funcStmt.Name.Lexeme}";
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;
        
        var func = module.GetNamedFunction(fullName);
        if (func.Handle == IntPtr.Zero) return; // Should have been declared

        var entry = func.AppendBasicBlock("entry");
        var oldBlock = builder.InsertBlock;
        builder.PositionAtEnd(entry);

        Session.Symbols.PushScope();
        for (int i = 0; i < funcStmt.Params.Count; i++)
        {
            var param = funcStmt.Params[i];
            var val = func.GetParam((uint)i);
            var ptr = builder.BuildAlloca(ctx.Int64Type, param.Name.Lexeme);
            builder.BuildStore(val, ptr);
            Session.Symbols.DefineVariable(param.Name.Lexeme, ptr);
        }

        foreach (var s in funcStmt.Body)
        {
            StatementCompiler.Compile(s);
        }

        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
        {
            builder.BuildRet(LLVMValueRef.CreateConstInt(ctx.Int64Type, 0));
        }

        Session.Symbols.PopScope();
        if (oldBlock.Handle != IntPtr.Zero)
            builder.PositionAtEnd(oldBlock);
    }
}
