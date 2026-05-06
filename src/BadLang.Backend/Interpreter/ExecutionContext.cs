using BadLang.IR;

namespace BadLang.Backend.Interpreter;

public class TryContext
{
    public string CatchLabel { get; set; } = "";
    public string? FinallyLabel { get; set; } = "";
    public Environment SavedEnv { get; set; } = null!;

    public override string ToString()
    {
        return $"{CatchLabel} {FinallyLabel}";
    }
}

public class ExecutionContext(
    Interpreter interpreter,
    Environment environment,
    Dictionary<string, int> labels)
{
    public Interpreter Interpreter { get; } = interpreter;
    public Environment Environment { get; set; } = environment;
    public Dictionary<string, int> Labels { get; } = labels;
    public Dictionary<string, object> Metadata { get; } = new();
    public int InstructionPointer { get; set; }
    public Stack<TryContext> TryStack { get; } = new();

    public object? Eval(IrValue value)
    {
        return Interpreter.GetValueInternal(value, Environment);
    }
}
