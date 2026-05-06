using BadLang.Core;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangException(object? value)
    : Exception
{
    public object? Value { get; } = value;
}
public class ReturnSignal(object? value) 
    : Exception
{
    public object? Value { get; } = value;
}

public class RuntimeException(Token token, string message)
    : Exception(message)
{
    public Token Token { get; } = token;
}
