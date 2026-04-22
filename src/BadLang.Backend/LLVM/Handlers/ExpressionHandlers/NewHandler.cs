using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class NewHandler : ExpressionHandler
{
    public NewHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.New;

    public override LLVMValueRef Compile(Expr expr)
    {
        var newExpr = (Expr.New)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;
        
        if (newExpr.Callee is Expr.Variable v)
        {
            var typeName = v.Name.Lexeme;
            if (Session.Symbols.TryGetType(typeName, out var typeInfo))
            {
                var rtFn = module.GetNamedFunction("badlang_gc_alloc");
                var size = typeInfo.Type.SizeOf;
                
                if (typeInfo.VTableGlobal != null) // Class
                {
                    var objRaw = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_gc_alloc"), rtFn, new[] { size }, "new_obj_raw");
                    var objPtr = builder.BuildBitCast(objRaw, LLVMTypeRef.CreatePointer(typeInfo.Type, 0), "new_obj_ptr");

                    // Initialize vtable
                    var vtableField = builder.BuildStructGEP2(typeInfo.Type, objPtr, 0, "vtable_field");
                    builder.BuildStore(typeInfo.VTableGlobal.Value, vtableField);

                    // Call constructor if exists
                    var ctorName = $"{typeName}__init";
                    if (Session.Symbols.TryGetFunction(ctorName, out var ctorInfo))
                    {
                        var ctorFunc = module.GetNamedFunction(ctorName);
                        var args = new LLVMValueRef[newExpr.Arguments.Count + 1];
                        args[0] = objRaw;
                        for (int i = 0; i < newExpr.Arguments.Count; i++)
                            args[i + 1] = ExpressionCompiler.Compile(newExpr.Arguments[i]);
                        builder.BuildCall2(ctorInfo.Type, ctorFunc, args, "");
                    }

                    Session.LastExpressionType = typeName;
                    return Session.FromPtr(objRaw);
                }
                else // Struct
                {
                    var objRaw = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_gc_alloc"), rtFn, new[] { size }, "new_struct_raw");
                    
                    Session.LastExpressionType = typeName;
                    return Session.FromPtr(objRaw);
                }
            }
        }

        throw new CompileError("Invalid 'new' target", newExpr.Callee is Expr.Variable v2 ? v2.Name : null!);
    }
}
