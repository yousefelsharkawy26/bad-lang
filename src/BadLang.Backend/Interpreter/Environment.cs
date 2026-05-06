using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter;

public class Environment
{
    private readonly Environment? _enclosing;
    private readonly Dictionary<string, object?> _values = new();

    public Environment? Enclosing => _enclosing;

    public Environment()
    {
        _enclosing = null;
    }

    public Environment(Environment enclosing)
    {
        _enclosing = enclosing;
    }

    public void Define(string name, object? value)
    {
        _values[name] = value;
    }

    public IEnumerable<KeyValuePair<string, object?>> GetValues()
    {
        return _values;
    }

    public object? Get(Token name)
    {
        return Get(name.Lexeme);
    }

    public object? Get(string name)
    {
        if (_values.TryGetValue(name, out var value))
        {
            return value;
        }

        if (_enclosing != null) return _enclosing.Get(name);

        throw new RuntimeException(new Token(TokenType.Identifier, name, null, 0, 0, 0), $"Undefined variable '{name}'.");
    }

    public void Assign(Token name, object? value)
    {
        Assign(name.Lexeme, value);
    }

    public void Assign(string name, object? value)
    {
        if (_values.ContainsKey(name))
        {
            _values[name] = value;
            return;
        }

        if (_enclosing != null)
        {
            _enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeException(new Token(TokenType.Identifier, name, null, 0, 0, 0), $"Undefined variable '{name}'.");
    }

    public object? GetAt(int distance, string name)
    {
        return Ancestor(distance)._values.GetValueOrDefault(name);
    }

    public void AssignAt(int distance, Token name, object? value)
    {
        Ancestor(distance)._values[name.Lexeme] = value;
    }

    private Environment Ancestor(int distance)
    {
        Environment? environment = this;
        for (int i = 0; i < distance; i++)
        {
            environment = environment?._enclosing;
        }
        return environment!;
    }
}
