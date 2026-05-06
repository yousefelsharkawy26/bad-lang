using System;
using System.Linq;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class FunctionHandler : IIrNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IrCall) };
    public bool CanHandle(IrNode node) => node is IrCall;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var env = context.Environment;
        var interpreter = context.Interpreter;

        if (node is IrCall call)
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
