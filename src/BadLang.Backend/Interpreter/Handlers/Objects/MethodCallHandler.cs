using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class MethodCallHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrMethodCall)];
    public bool CanHandle(IrNode node) => node is IrMethodCall;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var mcall = (IrMethodCall)node;
        var obj = context.Eval(mcall.Object);

        if (obj is BadLangInstance instance)
        {
            var method = instance.Get(new Token(TokenType.Identifier, mcall.MethodName, null, 0, 0, 0));
            if (method is IBadLangCallable callable)
            {
                var args = mcall.Arguments.Select(context.Eval).ToList();
                context.Interpreter.HandleCall(mcall.Target, callable, args, context.Environment);
            }
            else throw new Exception("Undefined method.");
        }
        else if (obj is Dictionary<object, object?> dict)
        {
            if (dict.TryGetValue(mcall.MethodName, out var method) && method is IBadLangCallable callable)
            {
                var args = mcall.Arguments.Select(context.Eval).ToList();
                context.Interpreter.HandleCall(mcall.Target, callable, args, context.Environment);
            }
            else throw new Exception($"Undefined method '{mcall.MethodName}' on module.");
        }
        else throw new Exception("Only instances have methods.");

        return HandlerResult.Continue;
    }
}
