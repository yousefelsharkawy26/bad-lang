using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Abstractions;

public struct VariableInfo
{
    public string Name;
    public LLVMValueRef Value;
    public string? TypeName;
    public bool IsConstant;
}

public struct FunctionInfo
{
    public string Name;
    public LLVMTypeRef Type;
    public string? ReturnTypeName;
}

public struct TypeInfo
{
    public string Name;
    public LLVMTypeRef Type;
    public string? Parent;
    public Dictionary<string, (int Index, string? TypeName)>? Fields;
    public Dictionary<string, int>? VTableOffsets;
    public LLVMValueRef? VTableGlobal;
}

public interface ISymbolTable
{
    HashSet<string> LoadedModules { get; }

    void PushScope();
    void PopScope();

    void DefineVariable(string name, LLVMValueRef value, string? typeName = null, bool isConst = false);
    VariableInfo GetVariable(string name);
    bool HasVariable(string name);

    void DefineFunction(string name, LLVMTypeRef type, string? returnTypeName = null);
    FunctionInfo GetFunction(string name);
    bool HasFunction(string name);
    bool TryGetFunction(string name, out FunctionInfo info);

    void DefineType(string name, LLVMTypeRef type, Dictionary<string, (int Index, string? TypeName)>? fields = null, string? parent = null, Dictionary<string, int>? vtableOffsets = null, LLVMValueRef? vtableGlobal = null);
    TypeInfo GetType(string name);
    bool HasType(string name);
    bool TryGetType(string name, out TypeInfo info);

    void DefineEnum(string name, List<string> members);
    bool TryGetEnum(string name, out List<string> members);

    void DefineInterface(string name, List<(string Name, string ReturnType)> methods);
    bool TryGetInterface(string name, out List<(string Name, string ReturnType)> methods);

    int GetMethodIndex(string methodName);
    IEnumerable<VariableInfo> GetAllVariables();
}
