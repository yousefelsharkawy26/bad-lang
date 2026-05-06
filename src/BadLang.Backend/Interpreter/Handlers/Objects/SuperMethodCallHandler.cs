using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class SuperMethodCallHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrSuperMethodCall)];
    public bool CanHandle(IrNode node) => node is IrSuperMethodCall;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var smcall = (IrSuperMethodCall)node;
        var env = context.Environment;
        var superClass = env.Get("super") as BadLangClass;
        var instance = env.Get("this") as BadLangInstance;

        if (superClass != null && instance != null)
        {
            var method = superClass.FindMethod(smcall.MethodName);
            if (method != null)
            {
                var callable = method.Bind(instance);
                var args = smcall.Arguments.Select(context.Eval).ToList();
                context.Interpreter.HandleCall(smcall.Target, callable, args, env);
            }
            else throw new Exception($"Undefined super method '{smcall.MethodName}'.");
        }
        else throw new Exception("Cannot use 'super' outside of a class method.");

        return HandlerResult.Continue;
    }
}
