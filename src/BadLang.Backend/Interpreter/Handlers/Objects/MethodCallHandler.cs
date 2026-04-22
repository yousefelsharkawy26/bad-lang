using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class MethodCallHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRMethodCall) };
    public bool CanHandle(IRNode node) => node is IRMethodCall;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var mcall = (IRMethodCall)node;
        var obj = context.Eval(mcall.Object);

        if (obj is BadLangInstance instance)
        {
            var method = instance.Get(new Token(TokenType.Identifier, mcall.MethodName, null, 0, 0, 0));
            if (method is IBadLangCallable callable)
            {
                var args = mcall.Arguments.Select(a => context.Eval(a)).ToList();
                context.Interpreter.HandleCall(mcall.Target, callable, args, context.Environment);
            }
            else throw new Exception("Undefined method.");
        }
        else if (obj is Dictionary<object, object?> dict)
        {
            if (dict.TryGetValue(mcall.MethodName, out var method) && method is IBadLangCallable callable)
            {
                var args = mcall.Arguments.Select(a => context.Eval(a)).ToList();
                context.Interpreter.HandleCall(mcall.Target, callable, args, context.Environment);
            }
            else throw new Exception($"Undefined method '{mcall.MethodName}' on module.");
        }
        else throw new Exception("Only instances have methods.");

        return HandlerResult.Continue;
    }
}
