using System;
using System.Linq;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class FunctionHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRCall) };
    public bool CanHandle(IRNode node) => node is IRCall;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var env = context.Environment;
        var interpreter = context.Interpreter;

        if (node is IRCall call)
        {
            var callee = env.Get(call.FunctionName);
            if (callee is IBadLangCallable callable)
            {
                var args = call.Arguments.Select(a => context.Eval(a)).ToList();
                interpreter.HandleCall(call.Target, callable, args, env);
            }
            else throw new Exception($"Can only call functions and classes.");
        }
        
        return HandlerResult.Continue;
    }
}
