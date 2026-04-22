using System.Collections.Generic;
using BadLang.IR;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangLambda : IBadLangCallable
{
    private readonly IRLambda _declaration;
    private readonly Environment _closure;

    public BadLangLambda(IRLambda declaration, Environment closure)
    {
        _declaration = declaration;
        _closure = closure;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        Environment environment = new(_closure);
        for (int i = 0; i < _declaration.Parameters.Count; i++)
        {
            if (i < arguments.Count)
                environment.Define(_declaration.Parameters[i], arguments[i]);
            else
                environment.Define(_declaration.Parameters[i], null);
        }

        return interpreter.ExecuteIR(_declaration.Body, environment);
    }
}
