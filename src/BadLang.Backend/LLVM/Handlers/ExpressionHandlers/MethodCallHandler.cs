using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class MethodCallHandler : ExpressionHandler
{
    public MethodCallHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Call call && call.Callee is Expr.Get;

    public override LLVMValueRef Compile(Expr expr)
    {
        var callExpr = (Expr.Call)expr;
        var getExpr = (Expr.Get)callExpr.Callee;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        if (getExpr.Object is Expr.Variable vNamespace)
        {
            var ns = vNamespace.Name.Lexeme;
            if (ns == "math" || ns == "Math" || ns == "String" || ns == "List" || ns == "Map" || ns == "File" || Session.Symbols.LoadedModules.Contains(ns))
            {
                return HandleNamespacedCall(ns, getExpr.Name.Lexeme, callExpr.Arguments, getExpr.Name);
            }
        }

        var objVal = ExpressionCompiler.Compile(getExpr.Object);
        string? objTypeName = Session.LastExpressionType;

        if (objTypeName != null && Session.Symbols.TryGetInterface(objTypeName, out var interfaceMethods))
        {
            int offset = Session.Symbols.GetMethodIndex(getExpr.Name.Lexeme);
            if (offset != -1)
            {
                var methodInfo = interfaceMethods.FirstOrDefault(m => m.Name == getExpr.Name.Lexeme);
                if (methodInfo.Name == null)
                    throw new CompileError($"Interface '{objTypeName}' does not have method '{getExpr.Name.Lexeme}'", getExpr.Name);

                var thisRaw = Session.ToPtr(objVal, Session.VoidPtrType);
                var vtablePtrPtr = builder.BuildBitCast(thisRaw, LLVMTypeRef.CreatePointer(Session.VoidPtrType, 0), "vtable_ptr_ptr");
                var vtablePtr = builder.BuildLoad2(Session.VoidPtrType, vtablePtrPtr, "vtable_ptr");

                var methodPtrPtr = builder.BuildGEP2(Session.VoidPtrType, vtablePtr, new[] { LLVMValueRef.CreateConstInt(ctx.Int64Type, (ulong)offset) }, "vmethod_ptr_ptr");
                var methodPtr = builder.BuildLoad2(Session.VoidPtrType, methodPtrPtr, "vmethod_ptr");

                var retType = ctx.Int64Type;
                var paramTypes = new LLVMTypeRef[callExpr.Arguments.Count + 1];
                paramTypes[0] = Session.VoidPtrType;
                for (int i = 0; i < callExpr.Arguments.Count; i++)
                    paramTypes[i + 1] = ctx.Int64Type;

                var methodType = LLVMTypeRef.CreateFunction(retType, paramTypes, false);
                var methodFunc = builder.BuildBitCast(methodPtr, LLVMTypeRef.CreatePointer(methodType, 0), "vmethod_func_cast");

                var methodArgs = new LLVMValueRef[callExpr.Arguments.Count + 1];
                methodArgs[0] = thisRaw;
                for (int i = 0; i < callExpr.Arguments.Count; i++)
                    methodArgs[i + 1] = ExpressionCompiler.Compile(callExpr.Arguments[i]);

                Session.LastExpressionType = methodInfo.ReturnType;
                return Session.ToI64(builder.BuildCall2(methodType, methodFunc, methodArgs, "vcall"));
            }
        }

        if (objTypeName != null && Session.Symbols.TryGetType(objTypeName, out var classInfo))
        {
            if (classInfo.VTableOffsets != null && classInfo.VTableOffsets.TryGetValue(getExpr.Name.Lexeme, out int offset))
            {
                var thisRaw = Session.ToPtr(objVal, Session.VoidPtrType);
                var objPtr = builder.BuildBitCast(thisRaw, LLVMTypeRef.CreatePointer(classInfo.Type, 0), "vcall_obj_ptr");
                var vtablePtrField = builder.BuildStructGEP2(classInfo.Type, objPtr, 0, "vtable_ptr_field");
                var vtablePtr = builder.BuildLoad2(Session.VoidPtrType, vtablePtrField, "vtable_ptr");

                var vtableFuncPtrType = Session.VoidPtrType;
                var vtableBase = builder.BuildBitCast(vtablePtr, LLVMTypeRef.CreatePointer(vtableFuncPtrType, 0), "vtable_base");
                var methodPtrPtr = builder.BuildGEP2(vtableFuncPtrType, vtableBase, new[] { LLVMValueRef.CreateConstInt(ctx.Int64Type, (ulong)offset) }, "vmethod_ptr_ptr");
                var methodPtr = builder.BuildLoad2(vtableFuncPtrType, methodPtrPtr, "vmethod_ptr");

                Abstractions.FunctionInfo methodFuncInfo = default;
                string? searchType = objTypeName;
                while (searchType != null && Session.Symbols.TryGetType(searchType, out var sc))
                {
                    var mName = $"{searchType}__{getExpr.Name.Lexeme}";
                    if (Session.Symbols.TryGetFunction(mName, out methodFuncInfo)) break;
                    searchType = sc.Parent;
                }

                if (methodFuncInfo.Type == null)
                    throw new CompileError($"Internal error: could not resolve function type for virtual method '{getExpr.Name.Lexeme}'", getExpr.Name);

                var methodFunc = builder.BuildBitCast(methodPtr, LLVMTypeRef.CreatePointer(methodFuncInfo.Type, 0), "vmethod_func_cast");

                var methodArgs = new LLVMValueRef[callExpr.Arguments.Count + 1];
                methodArgs[0] = thisRaw;
                for (int i = 0; i < callExpr.Arguments.Count; i++)
                    methodArgs[i + 1] = ExpressionCompiler.Compile(callExpr.Arguments[i]);

                Session.LastExpressionType = methodFuncInfo.ReturnTypeName;
                return Session.ToI64(builder.BuildCall2(methodFuncInfo.Type, methodFunc, methodArgs, "vcall"));
            }
            else
            {
                throw new CompileError($"Class '{objTypeName}' has no method '{getExpr.Name.Lexeme}'", getExpr.Name);
            }
        }
        throw new CompileError($"Cannot call method on non-class type '{objTypeName ?? "unknown"}'", getExpr.Name);
    }

    private LLVMValueRef HandleNamespacedCall(string ns, string method, IReadOnlyList<Expr> arguments, Token errorToken)
    {
        string fullName = $"{ns}.{method}";
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;
        
        if (Session.Symbols.LoadedModules.Contains(ns))
        {
            if (Session.Symbols.TryGetFunction(fullName, out var funcInfo))
            {
                var func = module.GetNamedFunction(fullName);
                var args = new LLVMValueRef[arguments.Count];
                for (int i = 0; i < arguments.Count; i++)
                    args[i] = ExpressionCompiler.Compile(arguments[i]);
                Session.LastExpressionType = funcInfo.ReturnTypeName;
                return Session.ToI64(builder.BuildCall2(funcInfo.Type, func, args, "modcall"));
            }
        }
        
        var mathMap = new Dictionary<string, string>
        {
            {"Math.abs","badlang_math_abs"}, {"Math.sqrt","badlang_math_sqrt"},
            {"Math.floor","badlang_math_floor"}, {"Math.ceil","badlang_math_ceil"},
            {"Math.round","badlang_math_round"}, {"Math.log","badlang_math_log"},
            {"Math.log2","badlang_math_log2"}, {"Math.sin","badlang_math_sin"},
            {"Math.cos","badlang_math_cos"}, {"Math.tan","badlang_math_tan"},
            {"Math.pow","badlang_math_pow"}, {"Math.min","badlang_math_min"},
            {"Math.max","badlang_math_max"},
            {"math.abs","badlang_math_abs"}, {"math.sqrt","badlang_math_sqrt"},
            {"math.floor","badlang_math_floor"}, {"math.ceil","badlang_math_ceil"},
            {"math.round","badlang_math_round"}, {"math.log","badlang_math_log"},
            {"math.log2","badlang_math_log2"}, {"math.sin","badlang_math_sin"},
            {"math.cos","badlang_math_cos"}, {"math.tan","badlang_math_tan"},
            {"math.pow","badlang_math_pow"}, {"math.min","badlang_math_min"},
            {"math.max","badlang_math_max"}
        };

        if (mathMap.TryGetValue(fullName, out var mathFnName))
        {
            var rtFn = module.GetNamedFunction(mathFnName);
            var compiledArgs = arguments.Select(a => Session.ToDouble(ExpressionCompiler.Compile(a))).ToArray();
            Session.LastExpressionType = "number";
            return Session.ToI64(builder.BuildCall2(Session.Runtime.GetRuntimeType(mathFnName), rtFn, compiledArgs, "mathtmp"));
        }

        if (fullName == "List.create")
        {
            var rtFn = module.GetNamedFunction("badlang_list_new");
            Session.LastExpressionType = "list";
            return Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_new"), rtFn, Array.Empty<LLVMValueRef>(), "list_new"));
        }
        if (fullName == "List.push")
        {
            var listVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var itemVal = ExpressionCompiler.Compile(arguments[1]);
            var rtFn = module.GetNamedFunction("badlang_list_push");
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_push"), rtFn, new[] { listVal, itemVal }, "");
            Session.LastExpressionType = "null";
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0));
        }
        if (fullName == "List.get")
        {
            var listVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var idx = Session.NumberToInt(ExpressionCompiler.Compile(arguments[1]));
            var rtFn = module.GetNamedFunction("badlang_list_get");
            Session.LastExpressionType = "number";
            return builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_get"), rtFn, new[] { listVal, idx }, "list_get");
        }
        if (fullName == "List.set")
        {
            var listVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var idx = Session.NumberToInt(ExpressionCompiler.Compile(arguments[1]));
            var item = ExpressionCompiler.Compile(arguments[2]);
            var rtFn = module.GetNamedFunction("badlang_list_set");
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_set"), rtFn, new[] { listVal, idx, item }, "");
            Session.LastExpressionType = "null";
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0));
        }
        if (fullName == "List.length")
        {
            var listVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var rtFn = module.GetNamedFunction("badlang_list_length");
            Session.LastExpressionType = "number";
            var rawLen = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_length"), rtFn, new[] { listVal }, "list_len");
            return Session.IntToNumber(rawLen);
        }
        if (fullName == "List.pop")
        {
            var listVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var rtFn = module.GetNamedFunction("badlang_list_pop");
            Session.LastExpressionType = "number";
            return builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_pop"), rtFn, new[] { listVal }, "list_pop");
        }

        if (fullName == "String.length")
        {
            var strVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.StringPtrType);
            var rtFn = module.GetNamedFunction("badlang_str_length");
            Session.LastExpressionType = "number";
            var rawLen = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_length"), rtFn, new[] { strVal }, "strlen");
            return Session.IntToNumber(rawLen);
        }
        if (fullName == "String.at")
        {
            var strVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.StringPtrType);
            var idx = Session.NumberToInt(ExpressionCompiler.Compile(arguments[1]));
            var rtFn = module.GetNamedFunction("badlang_str_at");
            Session.LastExpressionType = "string";
            return Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_at"), rtFn, new[] { strVal, idx }, "strat"));
        }

        if (fullName == "File.open")
        {
            var path = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.StringPtrType);
            var mode = arguments.Count > 1 ? Session.ToPtr(ExpressionCompiler.Compile(arguments[1]), Session.StringPtrType) : LLVMValueRef.CreateConstNull(Session.StringPtrType);
            var rtFn = module.GetNamedFunction("badlang_file_open");
            Session.LastExpressionType = "file";
            return Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_file_open"), rtFn, new[] { path, mode }, "file_open"));
        }
        if (fullName == "File.close")
        {
            var file = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var rtFn = module.GetNamedFunction("badlang_file_close");
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_file_close"), rtFn, new[] { file }, "");
            Session.LastExpressionType = "null";
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0));
        }
        if (fullName == "File.readLine")
        {
            var file = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var rtFn = module.GetNamedFunction("badlang_file_read_line");
            Session.LastExpressionType = "string";
            return Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_file_read_line"), rtFn, new[] { file }, "file_read"));
        }
        if (fullName == "File.write")
        {
            var file = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var str = Session.ToPtr(ExpressionCompiler.Compile(arguments[1]), Session.StringPtrType);
            var rtFn = module.GetNamedFunction("badlang_file_write");
            Session.LastExpressionType = "number";
            var rawRes = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_file_write"), rtFn, new[] { file, str }, "file_write");
            return Session.IntToNumber(rawRes);
        }
        if (fullName == "File.isEof")
        {
            var file = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var rtFn = module.GetNamedFunction("badlang_file_is_eof");
            Session.LastExpressionType = "number";
            var isEof = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_file_is_eof"), rtFn, new[] { file }, "file_eof");
            var doubleEof = builder.BuildUIToFP(isEof, ctx.DoubleType, "to_double");
            return Session.ToI64(doubleEof);
        }

        if (fullName == "Console.readLine")
        {
            LLVMValueRef prompt = LLVMValueRef.CreateConstPointerNull(Session.StringPtrType);
            if (arguments.Count > 0)
            {
                prompt = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.StringPtrType);
            }
            var rtFn = module.GetNamedFunction("badlang_console_read_line");
            Session.LastExpressionType = "string";
            return Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_console_read_line"), rtFn, new[] { prompt }, "console_read"));
        }

        if (fullName == "Map.create")
        {
            var rtFn = module.GetNamedFunction("badlang_map_new");
            Session.LastExpressionType = "map";
            return Session.FromPtr(builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_new"), rtFn, Array.Empty<LLVMValueRef>(), "map_new"));
        }
        if (fullName == "Map.set")
        {
            var mapVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var keyVal = Session.ToI64(ExpressionCompiler.Compile(arguments[1]));
            var valVal = Session.ToI64(ExpressionCompiler.Compile(arguments[2]));
            var rtFn = module.GetNamedFunction("badlang_map_set");
            builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_set"), rtFn, new[] { mapVal, keyVal, valVal }, "");
            Session.LastExpressionType = "null";
            return Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0));
        }
        if (fullName == "Map.get")
        {
            var mapVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var keyVal = Session.ToI64(ExpressionCompiler.Compile(arguments[1]));
            var rtFn = module.GetNamedFunction("badlang_map_get");
            Session.LastExpressionType = null;
            return builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_get"), rtFn, new[] { mapVal, keyVal }, "map_get");
        }
        if (fullName == "Map.size")
        {
            var mapVal = Session.ToPtr(ExpressionCompiler.Compile(arguments[0]), Session.VoidPtrType);
            var rtFn = module.GetNamedFunction("badlang_map_size");
            Session.LastExpressionType = "number";
            var rawSize = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_map_size"), rtFn, new[] { mapVal }, "map_size");
            return Session.IntToNumber(rawSize);
        }

        throw new CompileError($"Unknown static method '{method}' in namespace '{ns}'", errorToken);
    }
}
