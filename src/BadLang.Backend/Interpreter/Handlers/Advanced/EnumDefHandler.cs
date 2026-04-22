using System.Collections.Generic;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class EnumDefHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IREnumDef) };
    public bool CanHandle(IRNode node) => node is IREnumDef;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var edef = (IREnumDef)node;
        var enumValues = new Dictionary<object, object?>();
        for (int i = 0; i < edef.Variants.Count; i++)
        {
            enumValues[edef.Variants[i]] = (double)i;
        }
        context.Environment.Define(edef.Name, enumValues);
        return HandlerResult.Continue;
    }
}
