using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Definitions;

public class ClassDefHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrClassDef)];
    public bool CanHandle(IrNode node) => node is IrClassDef;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var cdef = (IrClassDef)node;
        var env = context.Environment;

        BadLangClass? superClass = null;
        if (cdef.SuperClass != null) 
        {
            var obj = env.Get(cdef.SuperClass);
            if (obj is BadLangClass bc) superClass = bc;
            else throw new Exception("Superclass must be a class.");
        }

        var methodEnv = env;
        if (superClass != null)
        {
            methodEnv = new Environment(env);
            methodEnv.Define("super", superClass);
        }

        var methods = new Dictionary<string, BadLangFunction>();
        foreach (var mdef in cdef.Methods) 
        {
            methods[mdef.Name] = new BadLangFunction(mdef, methodEnv, mdef.Name == "init" || mdef.Name == "__init");
        }

        var klass = new BadLangClass(cdef.Name, superClass, methods, cdef.Fields);
        env.Define(cdef.Name, klass);

        return HandlerResult.Continue;
    }
}
