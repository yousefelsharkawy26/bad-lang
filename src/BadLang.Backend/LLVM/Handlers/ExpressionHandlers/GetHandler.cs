using System;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class GetHandler : ExpressionHandler
{
    public GetHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Get;

    public override LLVMValueRef Compile(Expr expr)
    {
        var getExpr = (Expr.Get)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        // Handle namespaces
        if (getExpr.Object is Expr.Variable vNamespace)
        {
            var ns = vNamespace.Name.Lexeme;
            if (ns == "Math" || ns == "String" || ns == "List" || ns == "Map" || ns == "File" || Session.Symbols.LoadedModules.Contains(ns))
            {
                return HandleNamespacedGet(ns, getExpr.Name.Lexeme, getExpr.Name);
            }
        }

        // Handle Enums
        if (getExpr.Object is Expr.Variable vObjEnum && Session.Symbols.TryGetEnum(vObjEnum.Name.Lexeme, out var variants))
        {
            var variantName = getExpr.Name.Lexeme;
            var index = variants.IndexOf(variantName);
            if (index == -1)
                throw new CompileError($"Enum {vObjEnum.Name.Lexeme} does not have variant {variantName}", getExpr.Name);
            Session.LastExpressionType = null;
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, index));
        }

        var objVal = ExpressionCompiler.Compile(getExpr.Object);
        string? typeName = Session.LastExpressionType;

        if (typeName != null)
        {
            if (Session.Symbols.TryGetType(typeName, out var typeInfo))
            {
                if (typeInfo.Fields != null && typeInfo.Fields.TryGetValue(getExpr.Name.Lexeme, out var fieldInfo))
                {
                    var structPtr = Session.ToPtr(objVal, LLVMTypeRef.CreatePointer(typeInfo.Type, 0));
                    // Check if it's a class (has vtable at index 0) or struct
                    int offset = typeInfo.VTableGlobal != null ? 1 : 0;
                    var fieldPtr = builder.BuildStructGEP2(typeInfo.Type, structPtr, (uint)(fieldInfo.Index + offset), getExpr.Name.Lexeme);
                    Session.LastExpressionType = fieldInfo.TypeName;
                    return builder.BuildLoad2(ctx.Int64Type, fieldPtr, "field_load");
                }
            }
        }
        Session.LastExpressionType = null;
        throw new CompileError($"Cannot access field '{getExpr.Name.Lexeme}' on object of type '{typeName ?? "unknown"}'", getExpr.Name);
    }

    private LLVMValueRef HandleNamespacedGet(string ns, string property, Token errorToken)
    {
        var builder = Session.Infrastructure.Builder;
        var module = Session.Infrastructure.Module;
        var ctx = Session.Infrastructure.Context;

        if (ns == "Math" && property == "pi")
        {
            var rtFn = module.GetNamedFunction("badlang_math_pi");
            Session.LastExpressionType = "number";
            return Session.ToI64(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_math_pi"), rtFn, Array.Empty<LLVMValueRef>(), "pitmp"));
        }

        if (Session.Symbols.LoadedModules.Contains(ns))
        {
            string fullName = $"{ns}.{property}";
            
            if (Session.Symbols.HasVariable(fullName))
            {
                var info = Session.Symbols.GetVariable(fullName);
                Session.LastExpressionType = info.TypeName;
                return builder.BuildLoad2(ctx.Int64Type, info.Value, fullName);
            }

            if (Session.Symbols.HasType(fullName))
            {
                 throw new CompileError($"Cannot use type '{fullName}' as a value.", errorToken);
            }
        }

        throw new CompileError($"Unknown property '{property}' in namespace '{ns}'", errorToken);
    }
}
