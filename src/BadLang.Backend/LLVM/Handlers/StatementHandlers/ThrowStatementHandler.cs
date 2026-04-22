using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ThrowStatementHandler : StatementHandler
{
    public ThrowStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Throw;

    public override void Compile(Stmt stmt)
    {
        var throwStmt = (Stmt.Throw)stmt;
        var builder = Session.Infrastructure.Builder;
        var module = Session.Infrastructure.Module;

        var val = ExpressionCompiler.Compile(throwStmt.Value);
        var exNew = module.GetNamedFunction("badlang_exception_new");
        var throwFn = module.GetNamedFunction("badlang_throw");

        LLVMValueRef exObj;
        if (Session.LastExpressionType == "string")
        {
            exObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_exception_new"), exNew,
                new[] { Session.ToPtr(val, Session.StringPtrType) }, "ex_obj");
        }
        else
        {
            var emptyStr = builder.BuildGlobalStringPtr("", "empty_msg");
            var emptyStrObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_new"),
                module.GetNamedFunction("badlang_str_new"), new[] { emptyStr }, "ex_msg");
            exObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_exception_new"), exNew,
                new[] { emptyStrObj }, "ex_obj");
        }
        
        builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_throw"), throwFn, new[] { exObj }, "");
        builder.BuildUnreachable();
        
        var curFunc = builder.InsertBlock.Parent;
        var afterThrow = curFunc.AppendBasicBlock("after_throw");
        builder.PositionAtEnd(afterThrow);
    }
}
