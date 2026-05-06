namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangClass(
    string name,
    BadLangClass? superClass,
    Dictionary<string, BadLangFunction> methods,
    IReadOnlyList<string> fields)
    : IBadLangCallable
{
    public string Name { get; } = name;

    public List<string> GetAllFields()
    {
        var fields1 = new List<string>(fields);
        if (superClass != null)
        {
            fields1.AddRange(superClass.GetAllFields());
        }
        return fields1.Distinct().ToList();
    }

    public BadLangFunction? FindMethod(string name)
    {
        if (methods.TryGetValue(name, out var method))
        {
            return method;
        }

        if (superClass != null)
        {
            return superClass.FindMethod(name);
        }

        return null;
    }

    public object Call(Interpreter interpreter, List<object?> arguments)
    {
        BadLangInstance instance = new(this);
        BadLangFunction? initializer = FindMethod("init") ?? FindMethod("__init");
        if (initializer != null)
        {
            initializer.Bind(instance).Call(interpreter, arguments);
        }
        return instance;
    }
}
