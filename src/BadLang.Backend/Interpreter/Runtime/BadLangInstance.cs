using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Backend.Interpreter.Runtime;
using BadLang.Core;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangInstance
{
    private readonly BadLangClass _klass;
    private readonly Dictionary<string, object?> _fields = new();

    public BadLangInstance(BadLangClass klass)
    {
        _klass = klass;
        foreach (var field in _klass.GetAllFields())
        {
            _fields[field] = null;
        }
    }

    public object? Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out var value))
        {
            return value;
        }

        BadLangFunction? method = _klass.FindMethod(name.Lexeme);
        if (method != null) return method.Bind(this);

        throw new RuntimeException(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object? value)
    {
        _fields[name.Lexeme] = value;
    }
}
