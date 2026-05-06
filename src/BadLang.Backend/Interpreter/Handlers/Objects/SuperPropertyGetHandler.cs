using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class SuperPropertyGetHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrSuperPropertyGet)];
    public bool CanHandle(IrNode node) => node is IrSuperPropertyGet;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var spget = (IrSuperPropertyGet)node;
        var env = context.Environment;
        var superClass = env.Get("super") as BadLangClass;
        var instance = env.Get("this") as BadLangInstance;

        if (superClass != null && instance != null)
        {
            var method = superClass.FindMethod(spget.MethodName);
            if (method != null) env.Define(spget.Target, method.Bind(instance));
            else throw new Exception($"Undefined super method '{spget.MethodName}'.");
        }
        else throw new Exception("Cannot use 'super' outside of a class method.");

        return HandlerResult.Continue;
    }
}
