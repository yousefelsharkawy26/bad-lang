using BadLang.IR;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangLambda(IrLambda declaration, Environment closure)
    : IBadLangCallable
{
    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        Environment environment = new(closure);
        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            environment.Define(declaration.Parameters[i], i < arguments.Count ? arguments[i] : null);
        }

        return interpreter.ExecuteIR(declaration.Body, environment);
    }
}
