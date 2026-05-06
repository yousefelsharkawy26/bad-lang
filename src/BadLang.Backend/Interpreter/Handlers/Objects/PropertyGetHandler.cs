using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class PropertyGetHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrPropertyGet)];
    public bool CanHandle(IrNode node) => node is IrPropertyGet;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var pget = (IrPropertyGet)node;
        var obj = context.Eval(pget.Object);
        
        if (obj is BadLangInstance instance)
        {
            context.Environment.Define(pget.Target, instance.Get(new Token(TokenType.Identifier, pget.Property, null, 0, 0, 0)));
        }
        else if (obj is Dictionary<object, object?> dict)
        {
            context.Environment.Define(pget.Target, dict.GetValueOrDefault(pget.Property));
        }
        else throw new Exception("Only instances and maps have properties.");

        return HandlerResult.Continue;
    }
}
