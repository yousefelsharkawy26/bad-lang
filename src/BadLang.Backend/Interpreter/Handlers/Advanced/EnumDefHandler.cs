using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class EnumDefHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrEnumDef)];
    public bool CanHandle(IrNode node) => node is IrEnumDef;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var edef = (IrEnumDef)node;
        var enumValues = new Dictionary<object, object?>();
        for (int i = 0; i < edef.Variants.Count; i++)
        {
            enumValues[edef.Variants[i]] = (double)i;
        }
        context.Environment.Define(edef.Name, enumValues);
        return HandlerResult.Continue;
    }
}
