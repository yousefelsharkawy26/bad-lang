using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class LambdaHandler(
    CompilationSession session,
    IExpressionCompiler expressionCompiler,
    IStatementCompiler statementCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Lambda;

    public override LLVMValueRef Compile(Expr expr)
    {
        var lambda = (Expr.Lambda)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;
        
        var capturedVars = new List<(string Name, LLVMValueRef Ptr)>();
        
        foreach (var info in Session.Symbols.GetAllVariables())
        {
            capturedVars.Add((info.Name, info.Value));
        }

        var funcName = $"anon_fn_{Guid.NewGuid():N}";
        var paramTypes = new LLVMTypeRef[lambda.Parameters.Count + 1];
        paramTypes[0] = Session.VoidPtrType; // env pointer
        for (int i = 0; i < lambda.Parameters.Count; i++)
        {
            paramTypes[i + 1] = ctx.Int64Type;
        }
        var funcType = LLVMTypeRef.CreateFunction(ctx.Int64Type, paramTypes);
        var func = module.AddFunction(funcName, funcType);

        var entry = func.AppendBasicBlock("entry");
        var savedBlock = builder.InsertBlock;
        builder.PositionAtEnd(entry);

        Session.Symbols.PushScope();

        var envParam = func.GetParam(0);
        var closureStructType = LLVMTypeRef.CreateStruct(new[] { Session.VoidPtrType, ctx.Int32Type, LLVMTypeRef.CreateArray(Session.VoidPtrType, 0) }, false);
        var closurePtrType = LLVMTypeRef.CreatePointer(closureStructType, 0);
        var closurePtr = builder.BuildBitCast(envParam, closurePtrType, "closure");

        for (int i = 0; i < capturedVars.Count; i++)
        {
            var name = capturedVars[i].Name;
            var indices = new[] { 
                LLVMValueRef.CreateConstInt(ctx.Int32Type, 0), 
                LLVMValueRef.CreateConstInt(ctx.Int32Type, 2), 
                LLVMValueRef.CreateConstInt(ctx.Int32Type, (ulong)i) 
            };
            var capturePtr = builder.BuildGEP2(closureStructType, closurePtr, indices, $"cap_ptr_{name}");
            
            Session.Symbols.DefineVariable(name, capturePtr, "any");
        }

        for (int i = 0; i < lambda.Parameters.Count; i++)
        {
            var param = lambda.Parameters[i];
            var paramName = param.Name.Lexeme;
            var paramVal = func.GetParam((uint)(i + 1));

            var ptr = builder.BuildAlloca(ctx.Int64Type, paramName);
            builder.BuildStore(paramVal, ptr);
            Session.Symbols.DefineVariable(paramName, ptr, "any");
        }

        if (lambda.ExpressionBody != null)
        {
            var retVal = ExpressionCompiler.Compile(lambda.ExpressionBody);
            builder.BuildRet(retVal);
        }
        else
        {
            foreach (var stmt in lambda.Body)
            {
                statementCompiler.Compile(stmt);
            }
            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                builder.BuildRet(LLVMValueRef.CreateConstInt(ctx.Int64Type, 0));
            }
        }

        Session.Symbols.PopScope();
        builder.PositionAtEnd(savedBlock);

        var badlangClosureAlloc = module.GetNamedFunction("badlang_closure_alloc");
        var funcPtrAsVoid = builder.BuildBitCast(func, Session.VoidPtrType, "func_ptr");
        var countVal = LLVMValueRef.CreateConstInt(ctx.Int32Type, (ulong)capturedVars.Count);
        var allocatedClosure = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_closure_alloc"), badlangClosureAlloc, new[] { funcPtrAsVoid, countVal }, "closure_alloc");
        var typedAllocClosure = builder.BuildBitCast(allocatedClosure, closurePtrType, "typed_closure");

        for (int i = 0; i < capturedVars.Count; i++)
        {
            var val = builder.BuildLoad2(ctx.Int64Type, capturedVars[i].Ptr, "cap_val");
            var valAsVoidPtr = builder.BuildIntToPtr(val, Session.VoidPtrType, "val_as_ptr");
            var indices = new[] { 
                LLVMValueRef.CreateConstInt(ctx.Int32Type, 0), 
                LLVMValueRef.CreateConstInt(ctx.Int32Type, 2), 
                LLVMValueRef.CreateConstInt(ctx.Int32Type, (ulong)i) 
            };
            var capturePtr = builder.BuildGEP2(closureStructType, typedAllocClosure, indices, "cap_dest");
            builder.BuildStore(valAsVoidPtr, capturePtr);
        }

        Session.LastExpressionType = "function";
        return Session.FromPtr(allocatedClosure);
    }
}
