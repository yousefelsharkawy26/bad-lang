using System;
using System.Collections.Generic;

namespace BadLang.IR
{
    // Represents a value in the IR: either a Constant or a Virtual Register (variable)
    public abstract record IRValue;
    public record IRConst(object? Value) : IRValue;
    public record IRVar(string Name) : IRValue;

    public abstract record IRNode { }

    // Control Flow
    public record IRLabel : IRNode
    {
        public required string Name { get; init; }
        public override string ToString() => $"{Name}:";
    }

    public record IRJump : IRNode
    {
        public required string TargetLabel { get; init; }
        public override string ToString() => $"  jump {TargetLabel}";
    }

    public record IRCondJump : IRNode
    {
        public required IRValue Condition { get; init; }
        public required string TrueLabel { get; init; }
        public required string FalseLabel { get; init; }
        public override string ToString() => $"  cjump {Condition} ? {TrueLabel} : {FalseLabel}";
    }

    public record IRReturn : IRNode
    {
        public IRValue? Value { get; init; }
        public override string ToString() => Value != null ? $"  ret {Value}" : "  ret";
    }

    // Scopes
    public record IREnterScope : IRNode
    {
        public override string ToString() => "  enter_scope";
    }

    public record IRExitScope : IRNode
    {
        public override string ToString() => "  exit_scope";
    }

    // Exceptions
    public record IRTry : IRNode
    {
        public required string CatchLabel { get; init; }
        public string? FinallyLabel { get; init; }
        public override string ToString() => $"  try catch={CatchLabel}" + (FinallyLabel != null ? $" finally={FinallyLabel}" : "");
    }

    public record IRPopTry : IRNode
    {
        public override string ToString() => "  poptry";
    }

    public record IRThrow : IRNode
    {
        public required IRValue Exception { get; init; }
        public override string ToString() => $"  throw {Exception}";
    }

    // Operations
    public record IRAssign : IRNode
    {
        public required string Target { get; init; }
        public required IRValue Value { get; init; }
        public override string ToString() => $"  {Target} = {Value}";
    }

    public record IRBinary : IRNode
    {
        public required string Target { get; init; }
        public required string Op { get; init; }
        public required IRValue Left { get; init; }
        public required IRValue Right { get; init; }
        public override string ToString() => $"  {Target} = {Op} {Left}, {Right}";
    }

    public record IRUnary : IRNode
    {
        public required string Target { get; init; }
        public required string Op { get; init; }
        public required IRValue Operand { get; init; }
        public override string ToString() => $"  {Target} = {Op} {Operand}";
    }

    public record IRCall : IRNode
    {
        public required string Target { get; init; }
        public required string FunctionName { get; init; }
        public IReadOnlyList<IRValue> Arguments { get; init; } = Array.Empty<IRValue>();
        public override string ToString() => $"  {Target} = call {FunctionName}({string.Join(", ", Arguments)})";
    }
    
    // Memory and Objects
    public record IRLoad : IRNode
    {
        public required string Target { get; init; }
        public required string VariableName { get; init; }
        public override string ToString() => $"  {Target} = load {VariableName}";
    }

    public record IRDefine : IRNode
    {
        public required string VariableName { get; init; }
        public required IRValue Value { get; init; }
        public override string ToString() => $"  define {VariableName}, {Value}";
    }

    public record IRStore : IRNode
    {
        public required string VariableName { get; init; }
        public required IRValue Value { get; init; }
        public override string ToString() => $"  store {VariableName}, {Value}";
    }

    public record IRPropertyGet : IRNode
    {
        public required string Target { get; init; }
        public required IRValue Object { get; init; }
        public required string Property { get; init; }
        public override string ToString() => $"  {Target} = getprop {Object}.{Property}";
    }

    public record IRPropertySet : IRNode
    {
        public required IRValue Object { get; init; }
        public required string Property { get; init; }
        public required IRValue Value { get; init; }
        public override string ToString() => $"  setprop {Object}.{Property}, {Value}";
    }

    public record IRMethodCall : IRNode
    {
        public required string Target { get; init; }
        public required IRValue Object { get; init; }
        public required string MethodName { get; init; }
        public IReadOnlyList<IRValue> Arguments { get; init; } = Array.Empty<IRValue>();
        public override string ToString() => $"  {Target} = mcall {Object}.{MethodName}({string.Join(", ", Arguments)})";
    }

    public record IRSuperPropertyGet : IRNode
    {
        public required string Target { get; init; }
        public required string MethodName { get; init; }
        public override string ToString() => $"  {Target} = super.{MethodName}";
    }

    public record IRSuperMethodCall : IRNode
    {
        public required string Target { get; init; }
        public required string MethodName { get; init; }
        public IReadOnlyList<IRValue> Arguments { get; init; } = Array.Empty<IRValue>();
        public override string ToString() => $"  {Target} = super.{MethodName}({string.Join(", ", Arguments)})";
    }

    public record IRNewArray : IRNode
    {
        public required string Target { get; init; }
        public IReadOnlyList<IRValue> Elements { get; init; } = Array.Empty<IRValue>();
        public override string ToString() => $"  {Target} = newarray [{string.Join(", ", Elements)}]";
    }

    public record IRIndexGet : IRNode
    {
        public required string Target { get; init; }
        public required IRValue ArrayOrMap { get; init; }
        public required IRValue Index { get; init; }
        public override string ToString() => $"  {Target} = loadindex {ArrayOrMap}[{Index}]";
    }

    public record IRIndexSet : IRNode
    {
        public required IRValue ArrayOrMap { get; init; }
        public required IRValue Index { get; init; }
        public required IRValue Value { get; init; }
        public override string ToString() => $"  storeindex {ArrayOrMap}[{Index}], {Value}";
    }
    
    public record IRNewMap : IRNode
    {
        public required string Target { get; init; }
        public override string ToString() => $"  {Target} = newmap";
    }

    public record IRNew : IRNode
    {
        public required string Target { get; init; }
        public required IRValue Class { get; init; }
        public IReadOnlyList<IRValue> Arguments { get; init; } = Array.Empty<IRValue>();
        public override string ToString() => $"  {Target} = new {Class}({string.Join(", ", Arguments)})";
    }

    public record IRLambda : IRNode
    {
        public required string Target { get; init; }
        public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();
        public IReadOnlyList<IRNode> Body { get; init; } = Array.Empty<IRNode>();
        public IReadOnlyList<string> CapturedVariables { get; init; } = Array.Empty<string>();
        public override string ToString() => $"  {Target} = lambda({string.Join(", ", Parameters)}) captured=[{string.Join(", ", CapturedVariables)}] {{\n{string.Join("\n", Body)}\n}}";
    }

    // High level abstractions
    public record IRFunctionDef : IRNode
    {
        public required string Name { get; init; }
        public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();
        public IReadOnlyList<IRNode> Body { get; init; } = Array.Empty<IRNode>();
        public override string ToString() => $"func {Name}({string.Join(", ", Parameters)}) {{\n{string.Join("\n", Body)}\n}}";
    }

    public record IRClassDef : IRNode
    {
        public required string Name { get; init; }
        public string? SuperClass { get; init; }
        public IReadOnlyList<IRFunctionDef> Methods { get; init; } = Array.Empty<IRFunctionDef>();
        public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
        public override string ToString() => $"class {Name}{(SuperClass != null ? " : " + SuperClass : "")} {{\n  fields: {string.Join(", ", Fields)}\n{string.Join("\n", Methods)}\n}}";
    }

    public record IRInterfaceDef : IRNode
    {
        public required string Name { get; init; }
        public IReadOnlyList<string> MethodSignatures { get; init; } = Array.Empty<string>();
        public override string ToString() => $"interface {Name} {{\n  {string.Join("\n  ", MethodSignatures)}\n}}";
    }

    public record IRStructDef : IRNode
    {
        public required string Name { get; init; }
        public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
        public override string ToString() => $"struct {Name} {{\n  {string.Join(", ", Fields)}\n}}";
    }

    public record IREnumDef : IRNode
    {
        public required string Name { get; init; }
        public IReadOnlyList<string> Variants { get; init; } = Array.Empty<string>();
        public override string ToString() => $"enum {Name} {{ {string.Join(", ", Variants)} }}";
    }

    public record IRImport : IRNode
    {
        public required string Path { get; init; }
        public string? Alias { get; init; }
        public IReadOnlyList<string>? Symbols { get; init; }
        public override string ToString() => $"  import {Path}{(Alias != null ? " as " + Alias : "")}{(Symbols != null ? " {" + string.Join(", ", Symbols) + "}" : "")}";
    }

    public record IRExport : IRNode
    {
        public required string Name { get; init; }
        public override string ToString() => $"  export {Name}";
    }


    public record IRAssert : IRNode
    {
        public required IRValue Condition { get; init; }
        public required IRValue Message { get; init; }
        public override string ToString() => $"  assert {Condition}, {Message}";
    }

    public record IRPanic : IRNode
    {
        public required IRValue Message { get; init; }
        public override string ToString() => $"  panic {Message}";
    }
}
