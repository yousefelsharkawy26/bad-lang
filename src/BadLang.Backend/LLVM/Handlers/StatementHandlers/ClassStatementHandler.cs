using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ClassStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Class;

    public override void Compile(Stmt stmt)
    {
        var classStmt = (Stmt.Class)stmt;
        var className = string.IsNullOrEmpty(Session.CurrentNamespace) ? classStmt.Name.Lexeme : $"{Session.CurrentNamespace}.{classStmt.Name.Lexeme}";
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        var oldClass = Session.CurrentClassName;
        Session.CurrentClassName = className;

        foreach (var member in classStmt.Members.OfType<Stmt.Function>())
        {
            var methodName = member.Name.Lexeme;
            var mangledName = $"{className}__{methodName}";
            
            var func = module.GetNamedFunction(mangledName);
            if (func.Handle == IntPtr.Zero) continue;

            var entry = func.AppendBasicBlock("entry");
            var oldBlock = builder.InsertBlock;
            builder.PositionAtEnd(entry);

            Session.Symbols.PushScope();
            
            // 'this' pointer is param 0
            var thisPtr = func.GetParam(0);
            Session.CurrentThisPtr = thisPtr;

            for (int i = 0; i < member.Params.Count; i++)
            {
                var param = member.Params[i];
                var val = func.GetParam((uint)i + 1);
                var ptr = builder.BuildAlloca(ctx.Int64Type, param.Name.Lexeme);
                builder.BuildStore(val, ptr);
                Session.Symbols.DefineVariable(param.Name.Lexeme, ptr);
            }

            foreach (var s in member.Body)
            {
                StatementCompiler.Compile(s);
            }

            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                builder.BuildRet(LLVMValueRef.CreateConstInt(ctx.Int64Type, 0));
            }

            Session.Symbols.PopScope();
            Session.CurrentThisPtr = null;
            if (oldBlock.Handle != IntPtr.Zero)
                builder.PositionAtEnd(oldBlock);
        }

        Session.CurrentClassName = oldClass;
    }
}
