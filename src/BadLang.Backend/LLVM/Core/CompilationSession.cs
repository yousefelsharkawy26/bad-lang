using LLVMSharp.Interop;
using BadLang.Backend.LLVM.Abstractions;

namespace BadLang.Backend.LLVM.Core;

/// <summary>
/// Orchestrates the compilation session by coordinating infrastructure, symbols, and runtime providers.
/// </summary>
public class CompilationSession(
    ICompilerInfrastructure infrastructure,
    ISymbolTable symbols,
    IRuntimeProvider runtime)
{
    public ICompilerInfrastructure Infrastructure { get; } = infrastructure;
    public ISymbolTable Symbols { get; } = symbols;
    public IRuntimeProvider Runtime { get; } = runtime;

    // Session-specific state
    public string CurrentNamespace { get; set; } = "";
    public string OptLevel { get; set; } = "0";
    public bool Verbose { get; set; } = false;
    
    public string? LastExpressionType { get; set; }
    public LLVMValueRef? CurrentThisPtr { get; set; }
    public string? CurrentClassName { get; set; }

    public Stack<LLVMBasicBlockRef> BreakStack { get; } = new();
    public Stack<LLVMBasicBlockRef> ContinueStack { get; } = new();

    public LLVMTypeRef VoidPtrType => LLVMTypeRef.CreatePointer(Infrastructure.Context.Int8Type, 0);
    public LLVMTypeRef StringPtrType => LLVMTypeRef.CreatePointer(Infrastructure.Context.Int8Type, 0);
    public LLVMTypeRef ExceptionPtrType => LLVMTypeRef.CreatePointer(Infrastructure.Context.Int8Type, 0);

    // Method indexing for virtual dispatch
    private readonly Dictionary<string, int> _globalMethodIndices = new();
    private int _nextMethodIndex;

    public int GetGlobalMethodIndex(string name)
    {
        if (name == "_init") return -1;
        if (!_globalMethodIndices.TryGetValue(name, out int idx))
        {
            idx = _nextMethodIndex++;
            _globalMethodIndices[name] = idx;
        }
        return idx;
    }
    
    public int GetInterfaceMethodIndex(string methodName)
    {
        // This logic might need to be refined based on how interfaces are stored in the symbol table
        // For now, mirroring the legacy logic
        return -1; // Placeholder for now, will implement correctly if needed
    }

    // Helper methods for LLVM type conversions
    public LLVMValueRef ToDouble(LLVMValueRef i64Value)
    {
        return Infrastructure.Builder.BuildBitCast(i64Value, Infrastructure.Context.DoubleType, "todouble");
    }

    public LLVMValueRef FromDouble(LLVMValueRef doubleValue)
    {
        return Infrastructure.Builder.BuildBitCast(doubleValue, Infrastructure.Context.Int64Type, "fromdouble");
    }

    public LLVMValueRef ToPtr(LLVMValueRef i64Value, LLVMTypeRef pointerType)
    {
        return Infrastructure.Builder.BuildIntToPtr(i64Value, pointerType, "toptr");
    }

    public LLVMValueRef FromPtr(LLVMValueRef ptrValue)
    {
        return Infrastructure.Builder.BuildPtrToInt(ptrValue, Infrastructure.Context.Int64Type, "fromptr");
    }

    public LLVMValueRef ToI64(LLVMValueRef value)
    {
        if (value.TypeOf == Infrastructure.Context.Int64Type) return value;
        if (value.TypeOf == Infrastructure.Context.DoubleType) return FromDouble(value);
        if (value.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind) return FromPtr(value);
        if (value.TypeOf == Infrastructure.Context.Int1Type) return Infrastructure.Builder.BuildZExt(value, Infrastructure.Context.Int64Type, "i1toi64");
        throw new Exception($"Cannot convert {value.TypeOf} to i64");
    }

    public LLVMValueRef ToIndex(LLVMValueRef value)
    {
        return Infrastructure.Builder.BuildFPToSI(ToDouble(value), Infrastructure.Context.Int64Type, "toindex");
    }

    public LLVMValueRef NumberToInt(LLVMValueRef value)
    {
        return Infrastructure.Builder.BuildFPToSI(ToDouble(value), Infrastructure.Context.Int32Type, "numtoint");
    }

    public LLVMValueRef IntToNumber(LLVMValueRef value)
    {
        return ToI64(Infrastructure.Builder.BuildSIToFP(value, Infrastructure.Context.DoubleType, "itofp"));
    }
}
