using System;
using BadLang.Parser;
using BadLang.Core;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangException : Exception
{
    public object? Value { get; }
    public Token? Token { get; }
    public BadLangException(object? value) => Value = value;
    public BadLangException(Token token, string message) : base(message) => Token = token;
}

public class ReturnValue : Exception
{
    public object? Value { get; }
    public ReturnValue(object? value) => Value = value;
}

public class BreakException : Exception { }
public class ContinueException : Exception { }

public class ReturnSignal : Exception
{
    public object? Value { get; }
    public ReturnSignal(object? value) => Value = value;
}

public class RuntimeException : Exception
{
    public Token Token { get; }
    public RuntimeException(Token token, string message) : base(message)
    {
        Token = token;
    }
}
