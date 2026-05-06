namespace BadLang.IR;

// Represents a value in the IR: either a Constant or a Virtual Register (variable)
public abstract record IrValue;
public record IrConst(object? Value) : IrValue;
public record IrVar(string Name) : IrValue;

public abstract record IrNode { }

// Control Flow
public record IrLabel : IrNode
{
    public required string Name { get; init; }
    public override string ToString() => $"{Name}:";
}

public record IrJump : IrNode
{
    public required string TargetLabel { get; init; }
    public override string ToString() => $"  jump {TargetLabel}";
}

public record IrCondJump : IrNode
{
    public required IrValue Condition { get; init; }
    public required string TrueLabel { get; init; }
    public required string FalseLabel { get; init; }
    public override string ToString() => $"  cjump {Condition} ? {TrueLabel} : {FalseLabel}";
}

public record IrReturn : IrNode
{
    public IrValue? Value { get; init; }
    public override string ToString() => Value != null ? $"  ret {Value}" : "  ret";
}

// Scopes
public record IrEnterScope : IrNode
{
    public override string ToString() => "  enter_scope";
}

public record IrExitScope : IrNode
{
    public override string ToString() => "  exit_scope";
}

// Exceptions
public record IrTry : IrNode
{
    public required string CatchLabel { get; init; }
    public string? FinallyLabel { get; init; }
    public override string ToString() => $"  try catch={CatchLabel}" + (FinallyLabel != null ? $" finally={FinallyLabel}" : "");
}

public record IrPopTry : IrNode
{
    public override string ToString() => "  poptry";
}

public record IrThrow : IrNode
{
    public required IrValue Exception { get; init; }
    public override string ToString() => $"  throw {Exception}";
}

// Operations
public record IrAssign : IrNode
{
    public required string Target { get; init; }
    public required IrValue Value { get; init; }
    public override string ToString() => $"  {Target} = {Value}";
}

public record IrBinary : IrNode
{
    public required string Target { get; init; }
    public required string Op { get; init; }
    public required IrValue Left { get; init; }
    public required IrValue Right { get; init; }
    public override string ToString() => $"  {Target} = {Op} {Left}, {Right}";
}

public record IrUnary : IrNode
{
    public required string Target { get; init; }
    public required string Op { get; init; }
    public required IrValue Operand { get; init; }
    public override string ToString() => $"  {Target} = {Op} {Operand}";
}

public record IrCall : IrNode
{
    public required string Target { get; init; }
    public required string FunctionName { get; init; }
    public IReadOnlyList<IrValue> Arguments { get; init; } = Array.Empty<IrValue>();
    public override string ToString() => $"  {Target} = call {FunctionName}({string.Join(", ", Arguments)})";
}
    
// Memory and Objects
public record IrLoad : IrNode
{
    public required string Target { get; init; }
    public required string VariableName { get; init; }
    public override string ToString() => $"  {Target} = load {VariableName}";
}

public record IrDefine : IrNode
{
    public required string VariableName { get; init; }
    public required IrValue Value { get; init; }
    public override string ToString() => $"  define {VariableName}, {Value}";
}

public record IrStore : IrNode
{
    public required string VariableName { get; init; }
    public required IrValue Value { get; init; }
    public override string ToString() => $"  store {VariableName}, {Value}";
}

public record IrPropertyGet : IrNode
{
    public required string Target { get; init; }
    public required IrValue Object { get; init; }
    public required string Property { get; init; }
    public override string ToString() => $"  {Target} = getprop {Object}.{Property}";
}

public record IrPropertySet : IrNode
{
    public required IrValue Object { get; init; }
    public required string Property { get; init; }
    public required IrValue Value { get; init; }
    public override string ToString() => $"  setprop {Object}.{Property}, {Value}";
}

public record IrMethodCall : IrNode
{
    public required string Target { get; init; }
    public required IrValue Object { get; init; }
    public required string MethodName { get; init; }
    public IReadOnlyList<IrValue> Arguments { get; init; } = Array.Empty<IrValue>();
    public override string ToString() => $"  {Target} = mcall {Object}.{MethodName}({string.Join(", ", Arguments)})";
}

public record IrSuperPropertyGet : IrNode
{
    public required string Target { get; init; }
    public required string MethodName { get; init; }
    public override string ToString() => $"  {Target} = super.{MethodName}";
}

public record IrSuperMethodCall : IrNode
{
    public required string Target { get; init; }
    public required string MethodName { get; init; }
    public IReadOnlyList<IrValue> Arguments { get; init; } = Array.Empty<IrValue>();
    public override string ToString() => $"  {Target} = super.{MethodName}({string.Join(", ", Arguments)})";
}

public record IrNewArray : IrNode
{
    public required string Target { get; init; }
    public IReadOnlyList<IrValue> Elements { get; init; } = Array.Empty<IrValue>();
    public override string ToString() => $"  {Target} = newarray [{string.Join(", ", Elements)}]";
}

public record IrIndexGet : IrNode
{
    public required string Target { get; init; }
    public required IrValue ArrayOrMap { get; init; }
    public required IrValue Index { get; init; }
    public override string ToString() => $"  {Target} = loadindex {ArrayOrMap}[{Index}]";
}

public record IrIndexSet : IrNode
{
    public required IrValue ArrayOrMap { get; init; }
    public required IrValue Index { get; init; }
    public required IrValue Value { get; init; }
    public override string ToString() => $"  storeindex {ArrayOrMap}[{Index}], {Value}";
}
    
public record IrNewMap : IrNode
{
    public required string Target { get; init; }
    public override string ToString() => $"  {Target} = newmap";
}

public record IrNew : IrNode
{
    public required string Target { get; init; }
    public required IrValue Class { get; init; }
    public IReadOnlyList<IrValue> Arguments { get; init; } = Array.Empty<IrValue>();
    public override string ToString() => $"  {Target} = new {Class}({string.Join(", ", Arguments)})";
}

public record IrLambda : IrNode
{
    public required string Target { get; init; }
    public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IrNode> Body { get; init; } = Array.Empty<IrNode>();
    public IReadOnlyList<string> CapturedVariables { get; init; } = Array.Empty<string>();
    public override string ToString() => $"  {Target} = lambda({string.Join(", ", Parameters)}) captured=[{string.Join(", ", CapturedVariables)}] {{\n{string.Join("\n", Body)}\n}}";
}

// High level abstractions
public record IrFunctionDef : IrNode
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IrNode> Body { get; init; } = Array.Empty<IrNode>();
    public override string ToString() => $"func {Name}({string.Join(", ", Parameters)}) {{\n{string.Join("\n", Body)}\n}}";
}

public record IrClassDef : IrNode
{
    public required string Name { get; init; }
    public string? SuperClass { get; init; }
    public IReadOnlyList<IrFunctionDef> Methods { get; init; } = Array.Empty<IrFunctionDef>();
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
    public override string ToString() => $"class {Name}{(SuperClass != null ? " : " + SuperClass : "")} {{\n  fields: {string.Join(", ", Fields)}\n{string.Join("\n", Methods)}\n}}";
}

public record IrInterfaceDef : IrNode
{
    public required string Name { get; init; }
    public IReadOnlyList<string> MethodSignatures { get; init; } = Array.Empty<string>();
    public override string ToString() => $"interface {Name} {{\n  {string.Join("\n  ", MethodSignatures)}\n}}";
}

public record IrStructDef : IrNode
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
    public override string ToString() => $"struct {Name} {{\n  {string.Join(", ", Fields)}\n}}";
}

public record IrEnumDef : IrNode
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Variants { get; init; } = Array.Empty<string>();
    public override string ToString() => $"enum {Name} {{ {string.Join(", ", Variants)} }}";
}

public record IrImport : IrNode
{
    public required string Path { get; init; }
    public string? Alias { get; init; }
    public IReadOnlyList<string>? Symbols { get; init; }
    public override string ToString() => $"  import {Path}{(Alias != null ? " as " + Alias : "")}{(Symbols != null ? " {" + string.Join(", ", Symbols) + "}" : "")}";
}

public record IrExport : IrNode
{
    public required string Name { get; init; }
    public override string ToString() => $"  export {Name}";
}


public record IrAssert : IrNode
{
    public required IrValue Condition { get; init; }
    public required IrValue Message { get; init; }
    public override string ToString() => $"  assert {Condition}, {Message}";
}

public record IrPanic : IrNode
{
    public required IrValue Message { get; init; }
    public override string ToString() => $"  panic {Message}";
}