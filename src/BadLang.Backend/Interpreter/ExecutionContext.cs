using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter;

public class TryContext
{
    public string CatchLabel { get; set; } = "";
    public string? FinallyLabel { get; set; }
    public Environment SavedEnv { get; set; } = null!;
}

public class ExecutionContext
{
    public Interpreter Interpreter { get; }
    public Environment Environment { get; set; }
    public Dictionary<string, int> Labels { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    public int InstructionPointer { get; set; }
    public Stack<TryContext> TryStack { get; } = new();

    public ExecutionContext(
        Interpreter interpreter,
        Environment environment,
        Dictionary<string, int> labels)
    {
        Interpreter = interpreter;
        Environment = environment;
        Labels = labels;
        InstructionPointer = 0;
    }

    public object? Eval(IRValue value)
    {
        return Interpreter.GetValueInternal(value, Environment);
    }
}
