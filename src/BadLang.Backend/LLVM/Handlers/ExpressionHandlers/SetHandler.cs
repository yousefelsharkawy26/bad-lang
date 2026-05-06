using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class SetHandler(CompilationSession session, IExpressionCompiler expressionCompiler)
    : ExpressionHandler(session, expressionCompiler)
{
    public override bool CanHandle(Expr expr) => expr is Expr.Set;

    public override LLVMValueRef Compile(Expr expr)
    {
        var setExpr = (Expr.Set)expr;
        var builder = Session.Infrastructure.Builder;

        var objVal = ExpressionCompiler.Compile(setExpr.Object);
        string? typeName = Session.LastExpressionType;
        var val = Session.ToI64(ExpressionCompiler.Compile(setExpr.Value));

        if (typeName != null && Session.Symbols.TryGetType(typeName, out var typeInfo))
        {
            if (typeInfo.Fields != null && typeInfo.Fields.TryGetValue(setExpr.Name.Lexeme, out var fieldInfo))
            {
                var ptr = Session.ToPtr(objVal, LLVMTypeRef.CreatePointer(typeInfo.Type, 0));
                // Classes have vtable at index 0, structs don't.
                int offset = typeInfo.VTableGlobal != null ? 1 : 0;
                var fieldPtr = builder.BuildStructGEP2(typeInfo.Type, ptr, (uint)(fieldInfo.Index + offset), setExpr.Name.Lexeme);
                builder.BuildStore(val, fieldPtr);
                Session.LastExpressionType = fieldInfo.TypeName;
                return val;
            }
        }
        Session.LastExpressionType = null;
        throw new CompileError($"Cannot set field '{setExpr.Name.Lexeme}' on object of type '{typeName ?? "unknown"}'", setExpr.Name);
    }
}
