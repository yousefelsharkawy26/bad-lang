namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangStruct(string name, IReadOnlyList<string> fields)
    : IBadLangCallable
{
    public string Name { get; } = name;

    public object Call(Interpreter interpreter, List<object?> arguments)
    {
        var map = new Dictionary<object, object?>();
        for (int i = 0; i < fields.Count; i++)
        {
            map[fields[i]] = i < arguments.Count ? arguments[i] : null;
        }
        return map;
    }
}
