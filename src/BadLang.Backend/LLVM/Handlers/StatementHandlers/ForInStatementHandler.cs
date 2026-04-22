using System;
using System.Linq;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class ForInStatementHandler : StatementHandler
{
    public ForInStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.ForIn;

    public override void Compile(Stmt stmt)
    {
        var forIn = (Stmt.ForIn)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        var func = builder.InsertBlock.Parent;
        var condBb = func.AppendBasicBlock("forincond");
        var bodyBb = func.AppendBasicBlock("forinbody");
        var endBb = func.AppendBasicBlock("forinend");

        // Evaluate collection
        var collectionVal = ExpressionCompiler.Compile(forIn.Iterable);
        var targetType = Session.LastExpressionType;

        // Determine length
        LLVMValueRef lengthInt;
        if (targetType == "string")
        {
            var rtFn = module.GetNamedFunction("badlang_str_length");
            lengthInt = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_length"), rtFn, new[] { Session.ToPtr(collectionVal, Session.StringPtrType) }, "strlen");
        }
        else if (targetType == "list")
        {
            var rtFn = module.GetNamedFunction("badlang_list_length");
            lengthInt = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_length"), rtFn, new[] { Session.ToPtr(collectionVal, Session.VoidPtrType) }, "listlen");
        }
        else if (targetType == "map")
        {
            var rtFn = module.GetNamedFunction("badlang_map_size");
            lengthInt = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_size"), rtFn, new[] { Session.ToPtr(collectionVal, Session.VoidPtrType) }, "maplen");
        }
        else
        {
            // Native Array
            var doublePtr = Session.ToPtr(collectionVal, LLVMTypeRef.CreatePointer(ctx.DoubleType, 0));
            var lengthDouble = builder.BuildLoad2(ctx.DoubleType, doublePtr, "lendbl");
            lengthInt = builder.BuildFPToSI(lengthDouble, ctx.Int64Type, "lenint");
        }

        // Allocate index (i64 for consistency)
        var indexPtr = builder.BuildAlloca(ctx.Int64Type, "forin_idx");
        builder.BuildStore(LLVMValueRef.CreateConstInt(ctx.Int64Type, 0), indexPtr);

        Session.BreakStack.Push(endBb);
        Session.ContinueStack.Push(condBb);

        builder.BuildBr(condBb);

        builder.PositionAtEnd(condBb);
        var currentIndex = builder.BuildLoad2(ctx.Int64Type, indexPtr, "curr_idx");
        var isLess = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, currentIndex, lengthInt, "forincmp");
        builder.BuildCondBr(isLess, bodyBb, endBb);

        builder.PositionAtEnd(bodyBb);

        // Push scope for the loop variable
        Session.Symbols.PushScope();

        // Load element based on type
        LLVMValueRef elementValue;
        if (targetType == "string")
        {
            var rtFn = module.GetNamedFunction("badlang_str_at");
            elementValue = Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_at"), rtFn, new[] { Session.ToPtr(collectionVal, Session.StringPtrType), currentIndex }, "strat"));
        }
        else if (targetType == "list")
        {
            var rtFn = module.GetNamedFunction("badlang_list_get");
            elementValue = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_get"), rtFn, new[] { Session.ToPtr(collectionVal, Session.VoidPtrType), currentIndex }, "lget");
        }
        else if (targetType == "map")
        {
            var rtFn = module.GetNamedFunction("badlang_map_key_at");
            elementValue = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_key_at"), rtFn, new[] { Session.ToPtr(collectionVal, Session.VoidPtrType), currentIndex }, "mkey");
        }
        else
        {
            // Native Array
            var doublePtr = Session.ToPtr(collectionVal, LLVMTypeRef.CreatePointer(ctx.DoubleType, 0));
            var actualIndex = builder.BuildAdd(currentIndex, LLVMValueRef.CreateConstInt(ctx.Int64Type, 1), "idx_plus_1");
            var elementPtr = builder.BuildGEP2(ctx.DoubleType, doublePtr, new[] { actualIndex }, "elemptr");
            elementValue = Session.ToI64(builder.BuildLoad2(ctx.DoubleType, elementPtr, "elemval"));
        }

        // Define variable in the loop scope
        var varPtr = builder.BuildAlloca(ctx.Int64Type, forIn.Variable.Lexeme);
        builder.BuildStore(elementValue, varPtr);
        
        string? elemTypeName = (targetType == "string") ? "string" : null;
        Session.Symbols.DefineVariable(forIn.Variable.Lexeme, varPtr, elemTypeName);

        StatementCompiler.Compile(forIn.Body);

        // Increment index
        var nextIndex = builder.BuildAdd(currentIndex, LLVMValueRef.CreateConstInt(ctx.Int64Type, 1), "next_idx");
        builder.BuildStore(nextIndex, indexPtr);

        // Pop scope
        Session.Symbols.PopScope();

        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) 
            builder.BuildBr(condBb);

        builder.PositionAtEnd(endBb);
        Session.BreakStack.Pop();
        Session.ContinueStack.Pop();
    }
}
