using System;
using System.Linq;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class SuperMethodCallHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRSuperMethodCall) };
    public bool CanHandle(IRNode node) => node is IRSuperMethodCall;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var smcall = (IRSuperMethodCall)node;
        var env = context.Environment;
        var superClass = env.Get("super") as BadLangClass;
        var instance = env.Get("this") as BadLangInstance;

        if (superClass != null && instance != null)
        {
            var method = superClass.FindMethod(smcall.MethodName);
            if (method != null)
            {
                var callable = method.Bind(instance);
                var args = smcall.Arguments.Select(a => context.Eval(a)).ToList();
                context.Interpreter.HandleCall(smcall.Target, callable, args, env);
            }
            else throw new Exception($"Undefined super method '{smcall.MethodName}'.");
        }
        else throw new Exception("Cannot use 'super' outside of a class method.");

        return HandlerResult.Continue;
    }
}
