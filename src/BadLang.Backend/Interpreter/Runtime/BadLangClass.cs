using System.Collections.Generic;
using System.Linq;

namespace BadLang.Backend.Interpreter.Runtime;

public class BadLangClass : IBadLangCallable
{
    public string Name { get; }
    public BadLangClass? SuperClass { get; }
    private readonly Dictionary<string, BadLangFunction> _methods;
    private readonly IReadOnlyList<string> _fields;

    public BadLangClass(string name, BadLangClass? superClass, Dictionary<string, BadLangFunction> methods, IReadOnlyList<string> fields)
    {
        Name = name;
        SuperClass = superClass;
        _methods = methods;
        _fields = fields;
    }

    public List<string> GetAllFields()
    {
        var fields = new List<string>(_fields);
        if (SuperClass != null)
        {
            fields.AddRange(SuperClass.GetAllFields());
        }
        return fields.Distinct().ToList();
    }

    public BadLangFunction? FindMethod(string name)
    {
        if (_methods.TryGetValue(name, out var method))
        {
            return method;
        }

        if (SuperClass != null)
        {
            return SuperClass.FindMethod(name);
        }

        return null;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
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
