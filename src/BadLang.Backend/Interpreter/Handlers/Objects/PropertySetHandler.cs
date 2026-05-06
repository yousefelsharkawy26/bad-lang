using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class PropertySetHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrPropertySet)];
    public bool CanHandle(IrNode node) => node is IrPropertySet;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var pset = (IrPropertySet)node;
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
