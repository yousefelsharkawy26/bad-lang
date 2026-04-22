using System;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class PropertySetHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRPropertySet) };
    public bool CanHandle(IRNode node) => node is IRPropertySet;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var pset = (IRPropertySet)node;
        var obj = context.Eval(pset.Object);
        var val = context.Eval(pset.Value);

        if (obj is BadLangInstance instance)
        {
            instance.Set(new Token(TokenType.Identifier, pset.Property, null, 0, 0, 0), val);
        }
        else if (obj is Dictionary<object, object?> dict)
        {
            dict[pset.Property] = val;
        }
        else throw new Exception("Only instances and maps have fields.");

        return HandlerResult.Continue;
    }
}
