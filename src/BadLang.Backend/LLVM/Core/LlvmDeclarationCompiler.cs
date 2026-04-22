using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Core;

public class LlvmDeclarationCompiler
{
    private readonly CompilationSession _session;
    private readonly ModuleLoader? _moduleLoader;

    public LlvmDeclarationCompiler(CompilationSession session, ModuleLoader? moduleLoader)
    {
        _session = session;
        _moduleLoader = moduleLoader;
    }

    public void DeclareStatements(List<Stmt> statements, string prefix = "")
    {
        var classStmts = statements.OfType<Stmt.Class>().ToDictionary(c => c.Name.Lexeme);
        for (int i = 0; i < statements.Count; i++)
        {
            var stmt = statements[i];
            if (stmt is Stmt.Export exportStmt)
            {
                stmt = exportStmt.Declaration;
            }

            if (stmt is Stmt.Function funcStmt)
            {
                DeclareFunction(funcStmt, prefix);
            }
            else if (stmt is Stmt.Class classStmt)
            {
                DeclareClassRecursive(classStmt, classStmts, prefix);
            }
            else if (stmt is Stmt.Struct structStmt)
            {
                DeclareStruct(structStmt, prefix);
            }
            else if (stmt is Stmt.Enum enumStmt)
            {
                DeclareEnum(enumStmt, prefix);
            }
            else if (stmt is Stmt.Import importStmt)
            {
                DeclareImport(importStmt, statements);
            }
            else if (stmt is Stmt.Interface ifaceStmt)
            {
                DeclareInterface(ifaceStmt);
            }
        }
    }

    private void DeclareInterface(Stmt.Interface ifaceStmt)
    {
        var interfaceMethods = new List<(string Name, string ReturnType)>();
        foreach (var member in ifaceStmt.Signatures)
        {
            if (member is Stmt.Function f)
            {
                interfaceMethods.Add((f.Name.Lexeme, f.ReturnType?.ToString() ?? "any"));
            }
        }
        _session.Symbols.DefineInterface(ifaceStmt.Name.Lexeme, interfaceMethods);
    }

    private void DeclareImport(Stmt.Import importStmt, List<Stmt> allStatements)
    {
        string modulePath = string.Join(".", importStmt.Path.Select(t => t.Lexeme));
        string moduleName = importStmt.Path.Last().Lexeme;
        string effectiveAlias = importStmt.Alias?.Lexeme ?? moduleName;
        
        string importKey = $"{modulePath} as {effectiveAlias}";
        if (importStmt.Symbols != null)
        {
            importKey = $"{modulePath} symbols {string.Join(",", importStmt.Symbols.Select(s => s.Lexeme))}";
        }

        if (_session.Symbols.LoadedModules.Contains(importKey)) return;
        _session.Symbols.LoadedModules.Add(importKey);

        if (_moduleLoader != null)
        {
            try
            {
                var moduleStatements = _moduleLoader.LoadModule(importStmt.Path);
                
                foreach (var s in moduleStatements)
                {
                    Stmt inner = s;
                    bool isExported = false;
                    if (s is Stmt.Export exportStmt)
                    {
                        inner = exportStmt.Declaration;
                        isExported = true;
                    }

                    string? name = null;
                    if (inner is Stmt.Function f) name = f.Name.Lexeme;
                    else if (inner is Stmt.Class c) name = c.Name.Lexeme;
                    else if (inner is Stmt.Const k) name = k.Name.Lexeme;
                    else if (inner is Stmt.Var v) name = v.Name.Lexeme;

                    if (name != null)
                    {
                        if (importStmt.Symbols != null && !importStmt.Symbols.Any(sym => sym.Lexeme == name))
                            continue;

                        if (importStmt.Symbols == null && !isExported && !(inner is Stmt.Import))
                            continue; 

                        var finalName = importStmt.Symbols != null ? name : $"{effectiveAlias}.{name}";
                        Token finalToken = null!;

                        if (inner is Stmt.Function fun) {
                            finalToken = new Token(fun.Name.Type, finalName, fun.Name.Literal, fun.Name.Line, fun.Name.Column, fun.Name.Offset);
                            allStatements.Add(new Stmt.Function(finalToken, fun.Params, fun.ReturnType, fun.Body));
                        }
                        else if (inner is Stmt.Class cls) {
                            finalToken = new Token(cls.Name.Type, finalName, cls.Name.Literal, cls.Name.Line, cls.Name.Column, cls.Name.Offset);
                            allStatements.Add(new Stmt.Class(finalToken, cls.Generics, cls.Parents, cls.Members));
                        }
                        else if (inner is Stmt.Const cst) {
                            finalToken = new Token(cst.Name.Type, finalName, cst.Name.Literal, cst.Name.Line, cst.Name.Column, cst.Name.Offset);
                            allStatements.Add(new Stmt.Const(finalToken, cst.Type, cst.Initializer));
                        }
                        else if (inner is Stmt.Var vr) {
                            finalToken = new Token(vr.Name.Type, finalName, vr.Name.Literal, vr.Name.Line, vr.Name.Column, vr.Name.Offset);
                            allStatements.Add(new Stmt.Var(finalToken, vr.Type, vr.Initializer));
                        }
                    }
                    else if (inner is Stmt.Import)
                    {
                        allStatements.Add(inner);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CompileError($"Failed to load module '{moduleName}': {ex.Message}", importStmt.Path.First());
            }
        }
    }

    private void DeclareStruct(Stmt.Struct structStmt, string prefix = "")
    {
        string fullName = string.IsNullOrEmpty(prefix) ? structStmt.Name.Lexeme : $"{prefix}.{structStmt.Name.Lexeme}";
        var fieldTypes = new LLVMTypeRef[structStmt.Fields.Count];
        var fieldInfo = new Dictionary<string, (int Index, string? TypeName)>();
        for (int i = 0; i < structStmt.Fields.Count; i++)
        {
            var field = structStmt.Fields[i];
            string typeName = field.Type switch
            {
                TypeNode.UserDefined u => u.Name.Lexeme,
                TypeNode.Primitive p => p.Token.Type == TokenType.StringType ? "string" : (p.Token.Type == TokenType.NumType ? "number" : "any"),
                _ => "any"
            };

            fieldTypes[i] = _session.Infrastructure.Context.Int64Type;
            fieldInfo[field.Name.Lexeme] = (i, typeName);
        }
        var structType = _session.Infrastructure.Context.CreateNamedStruct(fullName);
        structType.StructSetBody(fieldTypes, false);
        _session.Symbols.DefineType(fullName, structType, fieldInfo);
    }

    private void DeclareEnum(Stmt.Enum enumStmt, string prefix = "")
    {
        string fullName = string.IsNullOrEmpty(prefix) ? enumStmt.Name.Lexeme : $"{prefix}.{enumStmt.Name.Lexeme}";
        _session.Symbols.DefineEnum(fullName, enumStmt.Variants.Select(m => m.Lexeme).ToList());
    }

    private void DeclareClassRecursive(Stmt.Class classStmt, Dictionary<string, Stmt.Class> classStmts, string prefix = "")
    {
        string fullName = string.IsNullOrEmpty(prefix) ? classStmt.Name.Lexeme : $"{prefix}.{classStmt.Name.Lexeme}";
        if (_session.Symbols.HasType(fullName)) return;

        string? parentName = classStmt.Parents.Count > 0 ? classStmt.Parents[0].Lexeme : null;
        if (parentName != null)
        {
            if (classStmts.TryGetValue(parentName, out var parentStmt))
            {
                DeclareClassRecursive(parentStmt, classStmts, prefix);
            }
            
            if (!string.IsNullOrEmpty(prefix) && !_session.Symbols.HasType(parentName) && _session.Symbols.HasType($"{prefix}.{parentName}"))
            {
                parentName = $"{prefix}.{parentName}";
            }
        }

        var fieldInfo = new Dictionary<string, (int Index, string? TypeName)>();
        var fieldTypes = new List<LLVMTypeRef>();

        fieldTypes.Add(_session.VoidPtrType);

        if (parentName != null && _session.Symbols.TryGetType(parentName, out var parentClass))
        {
            if (parentClass.Fields != null)
            {
                foreach (var kvp in parentClass.Fields.OrderBy(x => x.Value.Index))
                {
                    fieldInfo[kvp.Key] = kvp.Value;
                    fieldTypes.Add(_session.Infrastructure.Context.DoubleType);
                }
            }
        }

        var localFieldStmts = classStmt.Members.OfType<Stmt.Var>().ToList();
        int baseIndex = fieldTypes.Count;
        for (int i = 0; i < localFieldStmts.Count; i++)
        {
            fieldTypes.Add(_session.Infrastructure.Context.DoubleType);
            string ft = localFieldStmts[i].Type switch
            {
                TypeNode.UserDefined u => u.Name.Lexeme,
                TypeNode.Primitive p => p.Token.Type == TokenType.StringType ? "string" : (p.Token.Type == TokenType.NumType ? "number" : "any"),
                _ => "any"
            };

            fieldInfo[localFieldStmts[i].Name.Lexeme] = (baseIndex + i, ft);
        }

        var structType = _session.Infrastructure.Context.CreateNamedStruct(fullName);
        structType.StructSetBody(fieldTypes.ToArray(), false);

        var vtableOffsets = new Dictionary<string, int>();
        var vtableImpls = new Dictionary<string, string>();

        if (parentName != null && _session.Symbols.TryGetType(parentName, out var pc))
        {
            if (pc.VTableOffsets != null)
            {
                foreach (var kvp in pc.VTableOffsets)
                {
                    vtableOffsets[kvp.Key] = kvp.Value;
                }
            }
        }

        foreach (var member in classStmt.Members.OfType<Stmt.Function>())
        {
            var methodName = member.Name.Lexeme;
            var mangledName = $"{fullName}__{methodName}";

            var paramTypes = new LLVMTypeRef[member.Params.Count + 1];
            paramTypes[0] = _session.VoidPtrType; 
            for (int i = 0; i < member.Params.Count; i++)
                paramTypes[i + 1] = _session.Infrastructure.Context.Int64Type;

            var methodType = LLVMTypeRef.CreateFunction(_session.Infrastructure.Context.Int64Type, paramTypes);
            string? retTypeName = GetTypeName(member.ReturnType);
            _session.Symbols.DefineFunction(mangledName, methodType, retTypeName);
            _session.Infrastructure.Module.AddFunction(mangledName, methodType);

            int globalIdx = _session.Symbols.GetMethodIndex(methodName);
            if (globalIdx != -1)
            {
                vtableOffsets[methodName] = globalIdx;
                vtableImpls[methodName] = mangledName;
            }
        }

        var vtableFuncPtrType = _session.VoidPtrType;
        int maxIndex = vtableOffsets.Values.DefaultIfEmpty(-1).Max();
        var vtableArrayType = LLVMTypeRef.CreateArray(vtableFuncPtrType, (uint)(maxIndex + 1));

        var vtableConsts = new LLVMValueRef[maxIndex + 1];
        for (int i = 0; i <= maxIndex; i++)
        {
            vtableConsts[i] = LLVMValueRef.CreateConstNull(vtableFuncPtrType);
        }

        foreach (var kvp in vtableOffsets)
        {
            if (vtableImpls.TryGetValue(kvp.Key, out var implName))
            {
                var func = _session.Infrastructure.Module.GetNamedFunction(implName);
                vtableConsts[kvp.Value] = LLVMValueRef.CreateConstBitCast(func, vtableFuncPtrType);
            }
        }

        var vtableValue = LLVMValueRef.CreateConstArray(vtableFuncPtrType, vtableConsts);
        var vtableGlobal = _session.Infrastructure.Module.AddGlobal(vtableArrayType, $"{fullName}_vtable");
        vtableGlobal.Initializer = vtableValue;
        vtableGlobal.IsGlobalConstant = true;

        _session.Symbols.DefineType(fullName, structType, fieldInfo, parentName, vtableOffsets, vtableGlobal);
    }

    private void DeclareFunction(Stmt.Function funcStmt, string prefix = "")
    {
        string fullName = string.IsNullOrEmpty(prefix) ? funcStmt.Name.Lexeme : $"{prefix}.{funcStmt.Name.Lexeme}";
        
        var paramTypes = new LLVMTypeRef[funcStmt.Params.Count];
        for (int i = 0; i < funcStmt.Params.Count; i++)
            paramTypes[i] = _session.Infrastructure.Context.Int64Type;

        var funcType = LLVMTypeRef.CreateFunction(_session.Infrastructure.Context.Int64Type, paramTypes);
        _session.Symbols.DefineFunction(fullName, funcType, GetTypeName(funcStmt.ReturnType));
        _session.Infrastructure.Module.AddFunction(fullName, funcType);
    }

    public void DeclareRuntime()
    {
        _session.Runtime.DeclareRuntime(_session.Infrastructure.Module, _session.Infrastructure.Context);
    }

    private string? GetTypeName(TypeNode? type)
    {
        if (type == null) return null;
        if (type is TypeNode.UserDefined u) return u.Name.Lexeme;
        if (type is TypeNode.Primitive p)
        {
            if (p.Token.Type == TokenType.StringType) return "string";
            if (p.Token.Type == TokenType.NumType) return "number";
        }
        return null;
    }
}
