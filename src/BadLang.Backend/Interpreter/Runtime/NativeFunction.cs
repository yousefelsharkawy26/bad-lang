namespace BadLang.Backend.Interpreter.Runtime;

public class NativeFunction : IBadLangCallable
{
    private readonly Func<Interpreter, List<object?>, object?> _body;

    public NativeFunction(Func<Interpreter, List<object?>, object?> body)
    {
        _body = body;
    }

    public NativeFunction(Func<List<object?>, object?> body)
    {
        _body = (_, args) => body(args);
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        return _body(interpreter, arguments);
    }
}
