using System;
using System.Collections.Generic;
using System.Linq;

using BadLang.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.Semantic;

public class TypeChecker
{
    private class SymbolInfo
    {
        public string Type { get; set; } = "";
        public bool IsFunction { get; set; }
        public List<string> ParamTypes { get; set; } = new();
        public string ReturnType { get; set; } = "any";
    }

    private class ClassInfo
    {
        public string Name { get; set; } = "";
        public string? Parent { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new();
        public Dictionary<string, SymbolInfo> Methods { get; set; } = new();
    }

    private readonly List<Dictionary<string, SymbolInfo>> _scopes = new();
    private readonly Dictionary<string, ClassInfo> _classes = new();
    private readonly Dictionary<string, List<string>> _structs = new();
    private readonly Dictionary<string, List<string>> _enums = new();
    private readonly HashSet<string> _importedModules = new();
    private readonly HashSet<string> _checkedModules = new();
    private ModuleLoader? _moduleLoader;

    private string? _currentClassName;
    private string? _currentFunctionReturnType;
    private string? _currentModule;
    private readonly Dictionary<string, HashSet<string>> _moduleExportedNames = new();

    public TypeChecker()
    {
        // Global scope
        _scopes.Add(new Dictionary<string, SymbolInfo>());

        // Built-ins
        DefineBuiltin("print", new[] { "any" }, "void");
        DefineBuiltin("println", new[] { "any" }, "void");
        DefineBuiltin("clock", Array.Empty<string>(), "number");
        DefineBuiltin("input", new[] { "any" }, "string");
        DefineBuiltin("ReadLine", Array.Empty<string>(), "string");
        DefineBuiltin("str", new[] { "any" }, "string");
        DefineBuiltin("num", new[] { "any" }, "number");
        DefineBuiltin("len", new[] { "any" }, "number");
        DefineBuiltin("gc", Array.Empty<string>(), "void");

        // Math Primitives
        DefineBuiltin("math_sin", new[] { "number" }, "number");
        DefineBuiltin("math_cos", new[] { "number" }, "number");
        DefineBuiltin("math_tan", new[] { "number" }, "number");
        DefineBuiltin("math_sqrt", new[] { "number" }, "number");
        DefineBuiltin("math_pow", new[] { "number", "number" }, "number");
        DefineBuiltin("math_floor", new[] { "number" }, "number");
        DefineBuiltin("math_ceil", new[] { "number" }, "number");
        DefineBuiltin("math_round", new[] { "number" }, "number");
        DefineBuiltin("math_abs", new[] { "number" }, "number");

        // String Primitives
        DefineBuiltin("str_len", new[] { "string" }, "number");
        DefineBuiltin("str_lower", new[] { "string" }, "string");
        DefineBuiltin("str_upper", new[] { "string" }, "string");
        DefineBuiltin("str_substring", new[] { "string", "number", "number" }, "string");
        DefineBuiltin("str_replace", new[] { "string", "string", "string" }, "string");
        DefineBuiltin("str_split", new[] { "string", "string" }, "any[]");

        // List Primitives
        DefineBuiltin("list_push", new[] { "any[]", "any" }, "void");
        DefineBuiltin("list_pop", new[] { "any[]" }, "any");
        DefineBuiltin("list_remove_at", new[] { "any[]", "number" }, "void");
        DefineBuiltin("list_clear", new[] { "any[]" }, "void");
        DefineBuiltin("list_length", new[] { "any[]" }, "number");
        DefineBuiltin("list_contains", new[] { "any[]", "any" }, "bool");
        DefineBuiltin("list_index_of", new[] { "any[]", "any" }, "number");
        DefineBuiltin("list_reverse", new[] { "any[]" }, "void");

        // IO Primitives
        DefineBuiltin("io_read_file", new[] { "string" }, "string");
        DefineBuiltin("io_write_file", new[] { "string", "string" }, "void");
        DefineBuiltin("io_append_file", new[] { "string", "string" }, "void");
        DefineBuiltin("io_file_exists", new[] { "string" }, "bool");
        DefineBuiltin("io_delete_file", new[] { "string" }, "void");
        DefineBuiltin("io_read_lines", new[] { "string" }, "any[]");

        // Time Primitives
        DefineBuiltin("time_now", Array.Empty<string>(), "number");
        DefineBuiltin("time_sleep", new[] { "number" }, "void");

        // Map Primitives
        DefineBuiltin("map_new", Array.Empty<string>(), "any");
        DefineBuiltin("map_set", new[] { "any", "any", "any" }, "void");
        DefineBuiltin("map_get", new[] { "any", "any" }, "any");
        DefineBuiltin("map_has", new[] { "any", "any" }, "bool");
        DefineBuiltin("map_remove", new[] { "any", "any" }, "void");
        DefineBuiltin("map_keys", new[] { "any" }, "any[]");
        DefineBuiltin("map_values", new[] { "any" }, "any[]");
        DefineBuiltin("map_size", new[] { "any" }, "number");

        // Type Utilities
        DefineBuiltin("typeof", new[] { "any" }, "string");
        DefineBuiltin("toInt", new[] { "any" }, "number");
        DefineBuiltin("toFloat", new[] { "any" }, "number");
        DefineBuiltin("toString", new[] { "any" }, "string");
        DefineBuiltin("assert", new[] { "any" }, "void");

        // OS Primitives
        DefineBuiltin("os_getenv", new[] { "string" }, "string");
        DefineBuiltin("os_exit", new[] { "number" }, "void");
        DefineBuiltin("os_platform", Array.Empty<string>(), "string");
        DefineBuiltin("os_args", Array.Empty<string>(), "any[]");

        // Net Primitives
        DefineBuiltin("net_http_get", new[] { "string" }, "string");
    }

    public void SetBasePath(string basePath)
    {
        _moduleLoader = new ModuleLoader(basePath);
    }

    private void DefineBuiltin(string name, string[] paramTypes, string returnType)
    {
        _scopes[0][name] = new SymbolInfo
        {
            IsFunction = true,
            ParamTypes = paramTypes.ToList(),
            ReturnType = returnType,
            Type = "function"
        };
    }

    public List<CompileError> Errors { get; } = new();

    public void Check(IReadOnlyList<Stmt> statements, string? moduleName = null)
    {
        var oldModule = _currentModule;
        _currentModule = moduleName;
        if (moduleName != null && !_moduleExportedNames.ContainsKey(moduleName))
        {
            _moduleExportedNames[moduleName] = new HashSet<string>();
        }

        // Phase 1: Declare all top-level types (classes, structs, enums)
        foreach (var stmt in statements)
        {
            try
            {
                if (stmt is Stmt.Class c) DeclareClass(c);
                else if (stmt is Stmt.Struct s) DeclareStruct(s);
                else if (stmt is Stmt.Enum e) DeclareEnum(e);
                else if (stmt is Stmt.Export ex)
                {
                    if (_currentModule != null)
                    {
                        string? name = null;
                        if (ex.Declaration is Stmt.Class ec) name = ec.Name.Lexeme;
                        else if (ex.Declaration is Stmt.Struct es) name = es.Name.Lexeme;
                        else if (ex.Declaration is Stmt.Enum ee) name = ee.Name.Lexeme;
                        
                        if (name != null) _moduleExportedNames[_currentModule].Add(name);
                    }

                    if (ex.Declaration is Stmt.Class exc) DeclareClass(exc);
                    else if (ex.Declaration is Stmt.Struct exs) DeclareStruct(exs);
                    else if (ex.Declaration is Stmt.Enum exe) DeclareEnum(exe);
                }
            }
            catch (CompileError ex) { Errors.Add(ex); }
        }
    
        // Phase 2: Declare all functions
        foreach (var stmt in statements)
        {
            try
            {
                if (stmt is Stmt.Function f) DeclareFunction(f);
                else if (stmt is Stmt.Export ex && ex.Declaration is Stmt.Function ef)
                {
                    if (_currentModule != null) _moduleExportedNames[_currentModule].Add(ef.Name.Lexeme);
                    DeclareFunction(ef);
                }
            }
            catch (CompileError ex) { Errors.Add(ex); }
        }
    
        // Phase 3: Check bodies
        foreach (var stmt in statements)
        {
            try
            {
                CheckStmt(stmt);
            }
            catch (CompileError ex) { Errors.Add(ex); }
        }

        _currentModule = oldModule;
    }

    private void DeclareClass(Stmt.Class c)
    {
        string name = _currentModule != null ? $"{_currentModule}.{c.Name.Lexeme}" : c.Name.Lexeme;
        var info = new ClassInfo { Name = name };
        if (c.Parents.Any()) info.Parent = c.Parents[0].Lexeme;

        foreach (var member in c.Members)
        {
            if (member is Stmt.Var v)
            {
                info.Fields[v.Name.Lexeme] = GetTypeName(v.Type) ?? "any";
            }
            else if (member is Stmt.Function f)
            {
                info.Methods[f.Name.Lexeme] = new SymbolInfo
                {
                    IsFunction = true,
                    ParamTypes = f.Params.Select(p => GetTypeName(p.Type) ?? "any").ToList(),
                    ReturnType = GetTypeName(f.ReturnType) ?? "any",
                    Type = "function"
                };
            }
        }
        _classes[name] = info;
    }

    private void DeclareStruct(Stmt.Struct s)
    {
        string name = _currentModule != null ? $"{_currentModule}.{s.Name.Lexeme}" : s.Name.Lexeme;
        _structs[name] = s.Fields.Select(f => f.Name.Lexeme).ToList();
    }

    private void DeclareEnum(Stmt.Enum e)
    {
        string name = _currentModule != null ? $"{_currentModule}.{e.Name.Lexeme}" : e.Name.Lexeme;
        _enums[name] = e.Variants.Select(v => v.Lexeme).ToList();
    }

    private void DeclareFunction(Stmt.Function f)
    {
        string name = _currentModule != null ? $"{_currentModule}.{f.Name.Lexeme}" : f.Name.Lexeme;
        _scopes[0][name] = new SymbolInfo
        {
            IsFunction = true,
            ParamTypes = f.Params.Select(p => GetTypeName(p.Type) ?? "any").ToList(),
            ReturnType = GetTypeName(f.ReturnType) ?? "any",
            Type = "function"
        };
    }
    private void CheckStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case Stmt.Expression e:
                CheckExpr(e.Expr);
                break;
            case Stmt.Var v:
                var initType = v.Initializer != null ? CheckExpr(v.Initializer) : "any";
                var declaredType = GetTypeName(v.Type) ?? "any";
                if (declaredType != "any" && initType != "any" && !IsAssignable(declaredType, initType))
                {
                    throw new CompileError($"Cannot assign {initType} to variable of type {declaredType}", v.Name);
                }
                Define(v.Name.Lexeme, declaredType == "any" ? initType : declaredType, v.Name);
                break;
            case Stmt.Import i: CheckImport(i); break;
            case Stmt.Block b:
                BeginScope();
                foreach (var s in b.Statements) CheckStmt(s);
                EndScope();
                break;
            case Stmt.If i:
                CheckExpr(i.Condition);
                CheckStmt(i.ThenBranch);
                if (i.ElseBranch != null) CheckStmt(i.ElseBranch);
                break;
            case Stmt.While w:
                CheckExpr(w.Condition);
                CheckStmt(w.Body);
                break;
            case Stmt.Function f:
                CheckFunction(f);
                break;
            case Stmt.Return r:
                var retType = r.Value != null ? CheckExpr(r.Value) : "void";
                if (_currentFunctionReturnType != null && _currentFunctionReturnType != "any" && !IsAssignable(_currentFunctionReturnType, retType))
                {
                    throw new CompileError($"Return type {retType} does not match function return type {_currentFunctionReturnType}", r.Keyword);
                }
                break;
            case Stmt.Class c:
                _currentClassName = c.Name.Lexeme;
                foreach (var member in c.Members) CheckStmt(member);
                _currentClassName = null;
                break;
            case Stmt.Export ex:
                CheckStmt(ex.Declaration);
                break;
        }
    }

    private void CheckImport(Stmt.Import i)
    {
        if (_moduleLoader == null) return;

        string moduleName = i.Path.Last().Lexeme;
        string pathKey = string.Join(".", i.Path.Select(t => t.Lexeme));
        
        if (_checkedModules.Contains(pathKey)) return;
        _checkedModules.Add(pathKey);
        
        bool isFlat = pathKey.StartsWith("stdlib.");

        if (!isFlat)
        {
            _importedModules.Add(moduleName);
            Define(moduleName, "namespace");
        }
        try
        {
            var moduleStatements = _moduleLoader.LoadModule(i.Path);
            Check(moduleStatements, isFlat ? null : moduleName);
        }
        catch (CompileError ex)
        {
            Errors.Add(ex);
        }
    }

    private void CheckFunction(Stmt.Function f)
    {
        var oldRet = _currentFunctionReturnType;
        _currentFunctionReturnType = GetTypeName(f.ReturnType) ?? "any";

        BeginScope();
        foreach (var p in f.Params)
        {
            Define(p.Name.Lexeme, GetTypeName(p.Type) ?? "any", p.Name);
        }
        foreach (var s in f.Body) CheckStmt(s);
        EndScope();

        _currentFunctionReturnType = oldRet;
    }

    private string CheckExpr(Expr expr)
    {
        switch (expr)
        {
            case Expr.Literal l:
                if (l.Value is double) return "number";
                if (l.Value is string) return "string";
                if (l.Value is bool) return "bool";
                return "null";
            case Expr.Variable v:
                return Resolve(v.Name) ?? "any";
            case Expr.Lambda l:
                BeginScope();
                foreach (var param in l.Parameters)
                {
                    Define(param.Name.Lexeme, "any", param.Name);
                }
                if (l.ExpressionBody != null)
                {
                    CheckExpr(l.ExpressionBody);
                }
                else
                {
                    foreach (var stmt in l.Body)
                    {
                        CheckStmt(stmt);
                    }
                }
                EndScope();
                return "any";
            case Expr.Binary b:
                var left = CheckExpr(b.Left);
                var right = CheckExpr(b.Right);
                if (b.Operator.Type == TokenType.Plus)
                {
                    if (left == "string" || right == "string") return "string";
                    return "number";
                }
                if (IsComparison(b.Operator.Type))
                {
                    if ((left == "string" || right == "string") && left != "any" && right != "any")
                    {
                        if (b.Operator.Type is not (TokenType.EqualEqual or TokenType.BangEqual))
                        {
                            throw new CompileError($"Comparison operator {b.Operator.Lexeme} not supported for strings", b.Operator);
                        }
                        if (left != right && left != "any" && right != "any")
                        {
                            throw new CompileError($"Cannot compare {left} with {right} at {b.Operator.Lexeme}", b.Operator);
                        }
                    }
                    return "bool";
                }
                if (IsArithmetic(b.Operator.Type))
                {
                    if (left == "string" || right == "string")
                        throw new CompileError($"Arithmetic operator {b.Operator.Lexeme} not supported for strings", b.Operator);
                    return "number";
                }
                return "any";
            case Expr.Call c:
                {
                    // Handle special built-ins with variadic or optional arguments
                    if (c.Callee is Expr.Variable vBuiltin)
                    {
                        if (vBuiltin.Name.Lexeme == "print" || vBuiltin.Name.Lexeme == "println")
                        {
                            foreach (var arg in c.Arguments) CheckExpr(arg);
                            return "void";
                        }
                        if (vBuiltin.Name.Lexeme == "input" || vBuiltin.Name.Lexeme == "ReadLine")
                        {
                            if (c.Arguments.Count > 1)
                                throw new CompileError($"{vBuiltin.Name.Lexeme} expects 0 or 1 arguments", c.Paren);
                            foreach (var arg in c.Arguments) CheckExpr(arg);
                            return "string";
                        }
                    }

                    if (c.Callee is Expr.Variable vCallee)
                    {
                        var info = GetSymbol(vCallee.Name.Lexeme);
                        if (info != null && info.IsFunction) return CheckCall(info, c.Arguments, c.Paren);
                    }
                    else if (c.Callee is Expr.Get gCallee)
                    {
                        if (gCallee.Object is Expr.Variable gNs && _importedModules.Contains(gNs.Name.Lexeme))
                        {
                            var prefixedName = $"{gNs.Name.Lexeme}.{gCallee.Name.Lexeme}";
                            var info = GetSymbol(prefixedName);
                            if (info != null && info.IsFunction) return CheckCall(info, c.Arguments, c.Paren);
                        }
                        else
                        {
                            var callObjType = CheckExpr(gCallee.Object);
                            if (_classes.TryGetValue(callObjType, out var callClassInfo))
                            {
                                if (callClassInfo.Methods.TryGetValue(gCallee.Name.Lexeme, out var mInfo))
                                    return CheckCall(mInfo, c.Arguments, c.Paren);
                            }
                        }
                    }

                    CheckExpr(c.Callee);
                    foreach (var arg in c.Arguments) CheckExpr(arg);
                    return "any";
                }
            case Expr.Assign a:
                {
                    var valType = CheckExpr(a.Value);
                    if (a.Target is Expr.Variable vTarget)
                    {
                        var targetType = Resolve(vTarget.Name) ?? "any";
                        if (!IsAssignable(targetType, valType))
                            throw new CompileError($"Cannot assign {valType} to {targetType}", vTarget.Name);
                        return targetType;
                    }
                    return valType;
                }
            case Expr.New n:
                {
                    foreach (var arg in n.Arguments) CheckExpr(arg);
                    if (n.Callee is Expr.Variable vName && _classes.ContainsKey(vName.Name.Lexeme))
                        return vName.Name.Lexeme;
                    return CheckExpr(n.Callee);
                }
            case Expr.This t:
                if (_currentClassName == null) throw new CompileError("'this' used outside class", t.Keyword);
                return _currentClassName;
            case Expr.Get g:
                {
                    if (g.Object is Expr.Variable gNs && _importedModules.Contains(gNs.Name.Lexeme))
                    {
                        string moduleName = gNs.Name.Lexeme;
                        if (!_moduleExportedNames.ContainsKey(moduleName) || !_moduleExportedNames[moduleName].Contains(g.Name.Lexeme))
                        {
                            throw new CompileError($"Symbol '{g.Name.Lexeme}' is not exported from module '{moduleName}'", g.Name);
                        }

                        var prefixedName = $"{moduleName}.{g.Name.Lexeme}";
                        if (_classes.ContainsKey(prefixedName)) return prefixedName;
                        if (_structs.ContainsKey(prefixedName)) return "struct";
                        if (_enums.ContainsKey(prefixedName)) return "enum";
                        
                        var sym = GetSymbol(prefixedName);
                        if (sym != null) return sym.ReturnType;

                        return "any";
                    }
                    var objType = CheckExpr(g.Object);
                    if (_classes.TryGetValue(objType, out var classInfo))
                    {
                        if (classInfo.Fields.TryGetValue(g.Name.Lexeme, out var fType)) return fType;
                        if (classInfo.Methods.TryGetValue(g.Name.Lexeme, out var mInfo)) return "function";
                    }
                    return "any";
                }
            case Expr.TypeOf t:
                CheckExpr(t.Expr); // Just to typecheck it
                return "string";
            case Expr.NameOf n:
                if (n.Expr is not Expr.Variable && n.Expr is not Expr.Get)
                    throw new CompileError("nameof argument must be a variable or property", n.Keyword);
                return "string";
            case Expr.ToStringExpr ts:
                CheckExpr(ts.Expr);
                return "string";
            case Expr.ToNumberExpr tn:
                CheckExpr(tn.Expr);
                return "number";
            case Expr.IsNullExpr isnull:
                CheckExpr(isnull.Expr);
                return "bool";
            case Expr.AssertExpr a:
                var condType = CheckExpr(a.Condition);
                if (a.Message != null)
                {
                    var msgType = CheckExpr(a.Message);
                    if (msgType != "string" && msgType != "any")
                        throw new CompileError("assert message must be a string", a.Keyword);
                }
                return "void";
            case Expr.PanicExpr p:
                var panicMsgType = CheckExpr(p.Message);
                if (panicMsgType != "string" && panicMsgType != "any")
                    throw new CompileError("panic message must be a string", p.Keyword);
                return "void";
            case Expr.InterpolatedString interpolated:
                foreach (var part in interpolated.Parts) CheckExpr(part);
                return "string";
            default:
                return "any";
        }
    }

    private string? GetTypeName(TypeNode? node)
    {
        if (node == null) return null;
        return node switch
        {
            TypeNode.Primitive p => p.Token.Type == TokenType.NumType ? "number" : p.Token.Lexeme,
            TypeNode.UserDefined u => u.Name.Lexeme,
            TypeNode.Array a => $"{GetTypeName(a.BaseType)}[]",
            _ => "any"
        };
    }

    private bool IsAssignable(string target, string source)
    {
        if (target == "any" || source == "any") return true;
        if (target == source) return true;

        // Inheritance check
        if (_classes.TryGetValue(source, out var sourceInfo))
        {
            var current = sourceInfo.Parent;
            while (current != null)
            {
                if (current == target) return true;
                if (_classes.TryGetValue(current, out var pInfo)) current = pInfo.Parent;
                else break;
            }
        }

        return false;
    }

    private bool IsArithmetic(TokenType type) => type is TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash;
    private bool IsComparison(TokenType type) => type is TokenType.Greater or TokenType.GreaterEqual or TokenType.Less or TokenType.LessEqual or TokenType.EqualEqual or TokenType.BangEqual;

    private void BeginScope() => _scopes.Add(new Dictionary<string, SymbolInfo>());
    private void EndScope() => _scopes.RemoveAt(_scopes.Count - 1);

    private void Define(string name, string type, Token? token = null)
    {
        if (_scopes.Last().ContainsKey(name) && token != null)
        {
            throw new CompileError($"Variable '{name}' is already defined in this scope", token);
        }
        _scopes.Last()[name] = new SymbolInfo { Type = type };
    }

    private string? Resolve(Token name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].TryGetValue(name.Lexeme, out var info)) return info.Type;
        }

        if (_classes.ContainsKey(name.Lexeme)) return name.Lexeme;
        if (_structs.ContainsKey(name.Lexeme)) return "struct";
        if (_enums.ContainsKey(name.Lexeme)) return "enum";

        throw new CompileError($"Undefined variable '{name.Lexeme}'", name);
    }

    private string CheckCall(SymbolInfo info, IReadOnlyList<Expr> args, Token paren)
    {
        if (info.ParamTypes.Count != args.Count)
            throw new CompileError($"Expected {info.ParamTypes.Count} arguments but got {args.Count}", paren);
        for (int i = 0; i < args.Count; i++)
        {
            var argType = CheckExpr(args[i]);
            if (!IsAssignable(info.ParamTypes[i], argType))
                throw new CompileError($"Argument {i + 1} expected {info.ParamTypes[i]} but got {argType}", paren);
        }
        return info.ReturnType;
    }

    private SymbolInfo? GetSymbol(string name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].TryGetValue(name, out var info)) return info;
        }
        return null;
    }
}
