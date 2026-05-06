using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class ArrayLiteralHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.ArrayLiteral;

    public override LLVMValueRef Compile(Expr expr)
    {
        var arrayLiteral = (Expr.ArrayLiteral)expr;
        var elementCount = (uint)arrayLiteral.Elements.Count;
        const uint elementSize = 8;
        var totalSize = LLVMValueRef.CreateConstInt(Session.Infrastructure.Context.Int64Type, (elementCount + 1) * elementSize);

        var gcMalloc = Session.Infrastructure.Module.GetNamedFunction("badlang_gc_alloc");
        var gcMallocType = Session.Runtime.GetRuntimeType("badlang_gc_alloc");
        var rawPtr = Session.Infrastructure.Builder.BuildCall2(gcMallocType, gcMalloc, new[] { totalSize }, "gc_arr");
        var doublePtr = Session.Infrastructure.Builder.BuildBitCast(rawPtr, LLVMTypeRef.CreatePointer(Session.Infrastructure.Context.DoubleType, 0), "array_ptr");

        Session.Infrastructure.Builder.BuildStore(LLVMValueRef.CreateConstReal(Session.Infrastructure.Context.DoubleType, elementCount), doublePtr);

        for (int i = 0; i < arrayLiteral.Elements.Count; i++)
        {
            var elementVal = Session.ToDouble(ExpressionCompiler.Compile(arrayLiteral.Elements[i]));
            var index = LLVMValueRef.CreateConstInt(Session.Infrastructure.Context.Int64Type, (ulong)(i + 1));
            var elementPtr = Session.Infrastructure.Builder.BuildGEP2(Session.Infrastructure.Context.DoubleType, doublePtr, new[] { index }, $"elem_{i}_ptr");
            Session.Infrastructure.Builder.BuildStore(elementVal, elementPtr);
        }

        Session.LastExpressionType = "list";
        return Session.FromPtr(doublePtr);
    }
}
