using System;
using System.Collections.Generic;
using LLVMSharp.Interop;
using BadLang.Backend.LLVM.Abstractions;

namespace BadLang.Backend.LLVM.Infrastructure;

public class LlvmRuntimeProvider : IRuntimeProvider
{
    private readonly Dictionary<string, LLVMTypeRef> _runtimeTypes = new();
    public LLVMTypeRef PrintfType { get; private set; }

    public void DeclareRuntime(LLVMModuleRef module, LLVMContextRef context)
    {
        var i8Ptr = LLVMTypeRef.CreatePointer(context.Int8Type, 0);
        var voidPtr = i8Ptr;
        var i64 = context.Int64Type;
        var @double = context.DoubleType;
        var i1 = context.Int1Type;
        var i32 = context.Int32Type;

        // printf
        PrintfType = LLVMTypeRef.CreateFunction(i32, new[] { i8Ptr }, true);
        module.AddFunction("printf", PrintfType);

        // badlang_gc_alloc
        var gcMallocType = LLVMTypeRef.CreateFunction(i8Ptr, new[] { i64 }, false);
        module.AddFunction("badlang_gc_alloc", gcMallocType);
        _runtimeTypes["gc_malloc"] = gcMallocType;

        // badlang_gc_collect
        var gcCollectType = LLVMTypeRef.CreateFunction(context.VoidType, Array.Empty<LLVMTypeRef>());
        module.AddFunction("badlang_gc_collect", gcCollectType);
        _runtimeTypes["gc_collect"] = gcCollectType;

        // badlang_gc_init
        var gcInitType = LLVMTypeRef.CreateFunction(context.VoidType, new[] { i8Ptr });
        module.AddFunction("badlang_gc_init", gcInitType);
        _runtimeTypes["badlang_gc_init"] = gcInitType;

        // badlang_str_new
        var strNewType = LLVMTypeRef.CreateFunction(voidPtr, new[] { i8Ptr }, false);
        module.AddFunction("badlang_str_new", strNewType);
        _runtimeTypes["badlang_str_new"] = strNewType;

        // badlang_str_concat
        var strConcatType = LLVMTypeRef.CreateFunction(voidPtr, new[] { voidPtr, voidPtr }, false);
        module.AddFunction("badlang_str_concat", strConcatType);
        _runtimeTypes["badlang_str_concat"] = strConcatType;

        // badlang_str_print
        var strPrintType = LLVMTypeRef.CreateFunction(context.VoidType, new[] { voidPtr }, false);
        module.AddFunction("badlang_str_print", strPrintType);
        _runtimeTypes["badlang_str_print"] = strPrintType;

        // badlang_num_to_str
        var numToStrType = LLVMTypeRef.CreateFunction(voidPtr, new[] { @double }, false);
        module.AddFunction("badlang_num_to_str", numToStrType);
        _runtimeTypes["badlang_num_to_str"] = numToStrType;

        // badlang_str_eq
        var strEqType = LLVMTypeRef.CreateFunction(i1, new[] { voidPtr, voidPtr }, false);
        module.AddFunction("badlang_str_eq", strEqType);
        _runtimeTypes["badlang_str_eq"] = strEqType;

        // badlang_str_length
        var strLenType = LLVMTypeRef.CreateFunction(i64, new[] { voidPtr }, false);
        module.AddFunction("badlang_str_length", strLenType);
        _runtimeTypes["badlang_str_length"] = strLenType;

        // badlang_str_at
        var strAtType = LLVMTypeRef.CreateFunction(voidPtr, new[] { voidPtr, i64 }, false);
        module.AddFunction("badlang_str_at", strAtType);
        _runtimeTypes["badlang_str_at"] = strAtType;

        // badlang_console_read_line
        var consoleReadType = LLVMTypeRef.CreateFunction(voidPtr, new[] { voidPtr }, false);
        module.AddFunction("badlang_console_read_line", consoleReadType);
        _runtimeTypes["badlang_console_read_line"] = consoleReadType;

        // badlang_print_value
        var printValueType = LLVMTypeRef.CreateFunction(context.VoidType, new[] { i64 }, false);
        module.AddFunction("badlang_print_value", printValueType);
        _runtimeTypes["badlang_print_value"] = printValueType;

        // badlang_closure_alloc
        var closureAllocType = LLVMTypeRef.CreateFunction(voidPtr, new[] { voidPtr, i32 }, false);
        module.AddFunction("badlang_closure_alloc", closureAllocType);
        _runtimeTypes["badlang_closure_alloc"] = closureAllocType;

        // Exception helpers
        var exNewType = LLVMTypeRef.CreateFunction(voidPtr, new[] { voidPtr }, false);
        module.AddFunction("badlang_exception_new", exNewType);
        _runtimeTypes["badlang_exception_new"] = exNewType;

        var exMsgType = LLVMTypeRef.CreateFunction(voidPtr, new[] { voidPtr }, false);
        module.AddFunction("badlang_exception_message", exMsgType);
        _runtimeTypes["badlang_exception_message"] = exMsgType;

        var currentExType = LLVMTypeRef.CreateFunction(voidPtr, Array.Empty<LLVMTypeRef>());
        module.AddFunction("badlang_current_exception", currentExType);
        _runtimeTypes["badlang_current_exception"] = currentExType;

        var throwType = LLVMTypeRef.CreateFunction(context.VoidType, new[] { voidPtr });
        module.AddFunction("badlang_throw", throwType);
        _runtimeTypes["badlang_throw"] = throwType;

        var tryBeginType = LLVMTypeRef.CreateFunction(voidPtr, Array.Empty<LLVMTypeRef>());
        module.AddFunction("badlang_try_begin", tryBeginType);
        _runtimeTypes["badlang_try_begin"] = tryBeginType;

        var tryEndType = LLVMTypeRef.CreateFunction(context.VoidType, Array.Empty<LLVMTypeRef>());
        module.AddFunction("badlang_try_end", tryEndType);
        _runtimeTypes["badlang_try_end"] = tryEndType;

        // List stdlib
        var listPtrT = voidPtr;
        var listNewT = LLVMTypeRef.CreateFunction(listPtrT, Array.Empty<LLVMTypeRef>());
        module.AddFunction("badlang_list_new", listNewT);
        _runtimeTypes["badlang_list_new"] = listNewT;

        var listPushT = LLVMTypeRef.CreateFunction(context.VoidType, new[] { listPtrT, i64 });
        module.AddFunction("badlang_list_push", listPushT);
        _runtimeTypes["badlang_list_push"] = listPushT;

        var listGetT = LLVMTypeRef.CreateFunction(i64, new[] { listPtrT, i64 });
        module.AddFunction("badlang_list_get", listGetT);
        _runtimeTypes["badlang_list_get"] = listGetT;

        var listSetT = LLVMTypeRef.CreateFunction(context.VoidType, new[] { listPtrT, i64, i64 });
        module.AddFunction("badlang_list_set", listSetT);
        _runtimeTypes["badlang_list_set"] = listSetT;

        var listLenT = LLVMTypeRef.CreateFunction(i64, new[] { listPtrT });
        module.AddFunction("badlang_list_length", listLenT);
        _runtimeTypes["badlang_list_length"] = listLenT;

        // Map stdlib
        var mapPtrT = voidPtr;
        var mapNewT = LLVMTypeRef.CreateFunction(mapPtrT, Array.Empty<LLVMTypeRef>());
        module.AddFunction("badlang_map_new", mapNewT);
        _runtimeTypes["badlang_map_new"] = mapNewT;

        var mapSetT = LLVMTypeRef.CreateFunction(context.VoidType, new[] { mapPtrT, i64, i64 });
        module.AddFunction("badlang_map_set", mapSetT);
        _runtimeTypes["badlang_map_set"] = mapSetT;

        var mapGetT = LLVMTypeRef.CreateFunction(i64, new[] { mapPtrT, i64 });
        module.AddFunction("badlang_map_get", mapGetT);
        _runtimeTypes["badlang_map_get"] = mapGetT;

        // Math stdlib
        var d2d = LLVMTypeRef.CreateFunction(@double, new[] { @double });
        var d2d2 = LLVMTypeRef.CreateFunction(@double, new[] { @double, @double });
        foreach (var name in new[] { "abs", "sqrt", "floor", "ceil", "round", "log", "log2", "sin", "cos", "tan" })
        {
            var fullName = $"badlang_math_{name}";
            module.AddFunction(fullName, d2d);
            _runtimeTypes[fullName] = d2d;
        }
        module.AddFunction("badlang_math_pow", d2d2);
        _runtimeTypes["badlang_math_pow"] = d2d2;
    }

    public LLVMTypeRef GetRuntimeType(string name)
    {
        if (_runtimeTypes.TryGetValue(name, out var type)) return type;
        throw new Exception($"Unknown runtime function: {name}");
    }

    public bool HasRuntimeType(string name)
    {
        return _runtimeTypes.ContainsKey(name);
    }
}
