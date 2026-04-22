using System;
using System.Linq;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class NewHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRNew) };
    public bool CanHandle(IRNode node) => node is IRNew;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var n = (IRNew)node;
        var klass = context.Eval(n.Class);

        if (klass is BadLangClass badClass)
        {
            var args = n.Arguments.Select(a => context.Eval(a)).ToList();
            context.Environment.Define(n.Target, badClass.Call(context.Interpreter, args));
        }
        else throw new Exception("Can only instantiate classes.");

        return HandlerResult.Continue;
    }
}
