using System.Collections.Generic;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangStruct : IBadLangCallable
{
    public string Name { get; }
    private readonly IReadOnlyList<string> _fields;

    public BadLangStruct(string name, IReadOnlyList<string> fields)
    {
        Name = name;
        _fields = fields;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var map = new Dictionary<object, object?>();
        for (int i = 0; i < _fields.Count; i++)
        {
            map[_fields[i]] = i < arguments.Count ? arguments[i] : null;
        }
        return map;
    }
}
