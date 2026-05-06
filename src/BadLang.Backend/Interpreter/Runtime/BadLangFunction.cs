using BadLang.IR;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangFunction(IrFunctionDef declaration, Environment closure, bool isInitializer)
    : IBadLangCallable
{
    public BadLangFunction Bind(BadLangInstance instance)
    {
        Environment environment = new(closure);
        environment.Define("this", instance);
        return new BadLangFunction(declaration, environment, isInitializer);
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        Environment environment = new(closure);
        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            environment.Define(declaration.Parameters[i], arguments[i]);
        }

        object? returnValue = interpreter.ExecuteIR(declaration.Body, environment);

        if (isInitializer) return closure.GetAt(0, "this");
        return returnValue;
    }
}
