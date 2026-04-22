using System.Collections.Generic;
using BadLang.IR;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangFunction : IBadLangCallable
{
    private readonly IRFunctionDef _declaration;
    private readonly Environment _closure;
    private readonly bool _isInitializer;

    public BadLangFunction(IRFunctionDef declaration, Environment closure, bool isInitializer)
    {
        _declaration = declaration;
        _closure = closure;
        _isInitializer = isInitializer;
    }

    public BadLangFunction Bind(BadLangInstance instance)
    {
        Environment environment = new(_closure);
        environment.Define("this", instance);
        return new BadLangFunction(_declaration, environment, _isInitializer);
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        Environment environment = new(_closure);
        for (int i = 0; i < _declaration.Parameters.Count; i++)
        {
            environment.Define(_declaration.Parameters[i], arguments[i]);
        }

        object? returnValue = interpreter.ExecuteIR(_declaration.Body, environment);

        if (_isInitializer) return _closure.GetAt(0, "this");
        return returnValue;
    }
}
