using System;
using System.Collections.Generic;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class TryStatementHandler : StatementHandler
{
    public TryStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.TryCatch;

    public override void Compile(Stmt stmt)
    {
        var tryCatchStmt = (Stmt.TryCatch)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        var curFunc = builder.InsertBlock.Parent;
        var hasFinally = tryCatchStmt.FinallyBlock != null;

        var catchBb = curFunc.AppendBasicBlock("try_catch");
        var finallyBb = curFunc.AppendBasicBlock("try_finally");
        var afterBb = curFunc.AppendBasicBlock("try_after");
        var normalBb = curFunc.AppendBasicBlock("try_body");
        var handlerBb = curFunc.AppendBasicBlock("finally_handler");

        var rethrowFlag = builder.BuildAlloca(ctx.Int1Type, "rethrow_flag");
        var pendingEx = builder.BuildAlloca(Session.VoidPtrType, "pending_ex");
        builder.BuildStore(LLVMValueRef.CreateConstInt(ctx.Int1Type, 0), rethrowFlag);
        builder.BuildStore(LLVMValueRef.CreateConstNull(Session.VoidPtrType), pendingEx);

        var setjmpType = LLVMTypeRef.CreateFunction(ctx.Int32Type, new[] { LLVMTypeRef.CreatePointer(ctx.Int32Type, 0) }, false);
        var setjmpFn = module.GetNamedFunction("_setjmp");
        if (setjmpFn.Handle == IntPtr.Zero) setjmpFn = module.AddFunction("_setjmp", setjmpType);

        var tryBegin = module.GetNamedFunction("badlang_try_begin");
        var tryEnd = module.GetNamedFunction("badlang_try_end");
        
        var fBuf = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_try_begin"), tryBegin, Array.Empty<LLVMValueRef>(), "f_buf");
        var fRes = builder.BuildCall2(setjmpType, setjmpFn, new[] { fBuf }, "f_res");
        var fIsException = builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, fRes, LLVMValueRef.CreateConstInt(ctx.Int32Type, 0), "f_is_ex");
        
        var outerTryBb = curFunc.AppendBasicBlock("outer_try");
        builder.BuildCondBr(fIsException, handlerBb, outerTryBb);

        // -- Outer Try (contains catch)
        builder.PositionAtEnd(outerTryBb);
        var cBuf = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_try_begin"), tryBegin, Array.Empty<LLVMValueRef>(), "c_buf");
        var cRes = builder.BuildCall2(setjmpType, setjmpFn, new[] { cBuf }, "c_res");
        var cIsException = builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, cRes, LLVMValueRef.CreateConstInt(ctx.Int32Type, 0), "c_is_ex");
        
        builder.BuildCondBr(cIsException, catchBb, normalBb);

        // -- Normal Try Body
        builder.PositionAtEnd(normalBb);
        Session.Symbols.PushScope();
        foreach (var s in tryCatchStmt.TryBlock) StatementCompiler.Compile(s);
        Session.Symbols.PopScope();
        
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
        {
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_try_end"), tryEnd, Array.Empty<LLVMValueRef>(), ""); 
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_try_end"), tryEnd, Array.Empty<LLVMValueRef>(), ""); 
            builder.BuildBr(finallyBb);
        }

        // -- Catch block
        builder.PositionAtEnd(catchBb);
        var currentExFn = module.GetNamedFunction("badlang_current_exception");
        var exObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_current_exception"), currentExFn, Array.Empty<LLVMValueRef>(), "curr_ex");

        Session.Symbols.PushScope();
        foreach (var clause in tryCatchStmt.CatchClauses)
        {
            if (clause.ExceptionName != null)
            {
                var exMsgFn = module.GetNamedFunction("badlang_exception_message");
                var exMsg = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_exception_message"), exMsgFn, new[] { exObj }, "ex_msg");
                var slot = builder.BuildAlloca(Session.VoidPtrType, clause.ExceptionName.Lexeme);
                builder.BuildStore(exMsg, slot);
                Session.Symbols.DefineVariable(clause.ExceptionName.Lexeme, slot);
            }
            foreach (var cs in clause.Body) StatementCompiler.Compile(cs);
        }
        Session.Symbols.PopScope();
        
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
        {
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_try_end"), tryEnd, Array.Empty<LLVMValueRef>(), ""); 
            builder.BuildBr(finallyBb);
        }

        // -- Finally Handler (for rethrows)
        builder.PositionAtEnd(handlerBb);
        var rethrowEx = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_current_exception"), currentExFn, Array.Empty<LLVMValueRef>(), "rethrow_ex");
        builder.BuildStore(LLVMValueRef.CreateConstInt(ctx.Int1Type, 1), rethrowFlag);
        builder.BuildStore(rethrowEx, pendingEx);
        builder.BuildBr(finallyBb);

        // -- Finally block
        builder.PositionAtEnd(finallyBb);
        if (tryCatchStmt.FinallyBlock != null)
        {
            Session.Symbols.PushScope();
            foreach (var s in tryCatchStmt.FinallyBlock) StatementCompiler.Compile(s);
            Session.Symbols.PopScope();
        }
        
        var isRethrow = builder.BuildLoad2(ctx.Int1Type, rethrowFlag, "is_rethrow");
        var rethrowBb = curFunc.AppendBasicBlock("rethrow");
        var finalAfterBb = curFunc.AppendBasicBlock("final_after");
        builder.BuildCondBr(isRethrow, rethrowBb, finalAfterBb);
        
        Session.Infrastructure.Builder.PositionAtEnd(rethrowBb);
        var throwFn = module.GetNamedFunction("badlang_throw");
        builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_throw"), throwFn, new[] { builder.BuildLoad2(Session.VoidPtrType, pendingEx, "ex_to_rethrow") }, "");
        builder.BuildUnreachable();

        builder.PositionAtEnd(finalAfterBb);
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            builder.BuildBr(afterBb);

        builder.PositionAtEnd(afterBb);
    }
}
