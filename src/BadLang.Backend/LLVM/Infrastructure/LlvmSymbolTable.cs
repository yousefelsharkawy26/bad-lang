using System;
using System.Collections.Generic;
using BadLang.Backend.LLVM.Abstractions;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Infrastructure;

public class LlvmSymbolTable : ISymbolTable
{
    private readonly Stack<Dictionary<string, VariableInfo>> _scopes = new();
    private readonly Dictionary<string, FunctionInfo> _functions = new();
    private readonly Dictionary<string, TypeInfo> _types = new();
    private readonly Dictionary<string, List<string>> _enums = new();
    private readonly Dictionary<string, List<(string Name, string ReturnType)>> _interfaces = new();
    
    public HashSet<string> LoadedModules { get; } = new();

    private readonly Dictionary<string, int> _globalMethodIndices = new();
    private int _nextMethodIndex = 0;

    public LlvmSymbolTable()
    {
        _scopes.Push(new Dictionary<string, VariableInfo>());
    }

    public void PushScope() => _scopes.Push(new Dictionary<string, VariableInfo>());
    public void PopScope() => _scopes.Pop();

    public void DefineVariable(string name, LLVMValueRef value, string? typeName = null, bool isConst = false)
    {
        _scopes.Peek()[name] = new VariableInfo { Name = name, Value = value, TypeName = typeName, IsConstant = isConst };
    }

    public VariableInfo GetVariable(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var info)) return info;
        }
        throw new Exception($"Undefined variable: {name}");
    }

    public bool HasVariable(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name)) return true;
        }
        return false;
    }

    public void DefineFunction(string name, LLVMTypeRef type, string? returnTypeName = null)
    {
        _functions[name] = new FunctionInfo { Name = name, Type = type, ReturnTypeName = returnTypeName };
    }

    public FunctionInfo GetFunction(string name)
    {
        if (_functions.TryGetValue(name, out var info)) return info;
        throw new Exception($"Undefined function: {name}");
    }

    public bool HasFunction(string name) => _functions.ContainsKey(name);

    public bool TryGetFunction(string name, out FunctionInfo info)
    {
        return _functions.TryGetValue(name, out info);
    }

    public void DefineType(string name, LLVMTypeRef type, Dictionary<string, (int Index, string? TypeName)>? fields = null, string? parent = null, Dictionary<string, int>? vtableOffsets = null, LLVMValueRef? vtableGlobal = null)
    {
        _types[name] = new TypeInfo 
        { 
            Name = name, 
            Type = type, 
            Fields = fields, 
            Parent = parent,
            VTableOffsets = vtableOffsets,
            VTableGlobal = vtableGlobal
        };
    }

    public TypeInfo GetType(string name)
    {
        if (_types.TryGetValue(name, out var info)) return info;
        throw new Exception($"Undefined type: {name}");
    }

    public bool HasType(string name) => _types.ContainsKey(name);

    public bool TryGetType(string name, out TypeInfo info)
    {
        return _types.TryGetValue(name, out info);
    }

    public void DefineEnum(string name, List<string> members)
    {
        _enums[name] = members;
    }

    public bool TryGetEnum(string name, out List<string> members)
    {
        return _enums.TryGetValue(name, out members!);
    }

    public void DefineInterface(string name, List<(string Name, string ReturnType)> methods)
    {
        _interfaces[name] = methods;
    }

    public bool TryGetInterface(string name, out List<(string Name, string ReturnType)> methods)
    {
        return _interfaces.TryGetValue(name, out methods!);
    }

    public int GetMethodIndex(string methodName)
    {
        if (methodName == "_init") return -1;
        if (!_globalMethodIndices.TryGetValue(methodName, out int idx))
        {
            idx = _nextMethodIndex++;
            _globalMethodIndices[methodName] = idx;
        }
        return idx;
    }

    public IEnumerable<VariableInfo> GetAllVariables()
    {
        var seen = new HashSet<string>();
        foreach (var scope in _scopes)
        {
            foreach (var kvp in scope)
            {
                if (seen.Add(kvp.Key))
                {
                    yield return kvp.Value;
                }
            }
        }
    }
}
