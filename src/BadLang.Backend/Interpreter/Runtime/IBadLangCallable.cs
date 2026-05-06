namespace BadLang.Backend.Interpreter.Runtime;

public interface IBadLangCallable
{
    object? Call(Interpreter interpreter, List<object?> arguments);
}
