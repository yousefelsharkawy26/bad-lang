using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class CallHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Call;

    public override LLVMValueRef Compile(Expr expr)
    {
        var callExpr = (Expr.Call)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        if (callExpr.Callee is Expr.Variable v)
        {
            var funcName = v.Name.Lexeme;
            if (Session.Symbols.TryGetFunction(funcName, out var funcInfo))
            {
                var func = module.GetNamedFunction(funcName);
                var args = new LLVMValueRef[callExpr.Arguments.Count];
                for (int i = 0; i < callExpr.Arguments.Count; i++)
                    args[i] = ExpressionCompiler.Compile(callExpr.Arguments[i]);
                Session.LastExpressionType = funcInfo.ReturnTypeName;
                return Session.ToI64(builder.BuildCall2(funcInfo.Type, func, args, "call"));
            }
        }

        // Closure call
        var calleeVal = ExpressionCompiler.Compile(callExpr.Callee);
        var closureRaw = Session.ToPtr(calleeVal, Session.VoidPtrType);

        var closureType = LLVMTypeRef.CreateStruct(new[] { Session.VoidPtrType, Session.VoidPtrType }, false);
        var closurePtr = builder.BuildBitCast(closureRaw, LLVMTypeRef.CreatePointer(closureType, 0), "closure_ptr");

        var funcPtrField = builder.BuildStructGEP2(closureType, closurePtr, 0, "func_ptr_field");
        var funcPtrRaw = builder.BuildLoad2(Session.VoidPtrType, funcPtrField, "func_ptr_raw");

        var envPtrField = builder.BuildStructGEP2(closureType, closurePtr, 1, "env_ptr_field");
        var envPtr = builder.BuildLoad2(Session.VoidPtrType, envPtrField, "env_ptr");

        var retType = ctx.Int64Type;
        var paramTypes = new LLVMTypeRef[callExpr.Arguments.Count + 1];
        paramTypes[0] = Session.VoidPtrType;
        for (int i = 0; i < callExpr.Arguments.Count; i++)
            paramTypes[i + 1] = ctx.Int64Type;

        var funcType = LLVMTypeRef.CreateFunction(retType, paramTypes);
        var funcFunc = builder.BuildBitCast(funcPtrRaw, LLVMTypeRef.CreatePointer(funcType, 0), "func_cast");

        var argsClosure = new LLVMValueRef[callExpr.Arguments.Count + 1];
        argsClosure[0] = envPtr;
        for (int i = 0; i < callExpr.Arguments.Count; i++)
            argsClosure[i + 1] = ExpressionCompiler.Compile(callExpr.Arguments[i]);

        Session.LastExpressionType = null; // We don't know the return typeof a closure call statically easily here without more metadata
        return Session.ToI64(builder.BuildCall2(funcType, funcFunc, argsClosure, "closure_call"));
    }
}
