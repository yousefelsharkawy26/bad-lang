using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class NewHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrNew)];
    public bool CanHandle(IrNode node) => node is IrNew;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var n = (IrNew)node;
        var klass = context.Eval(n.Class);

        if (klass is BadLangClass badClass)
        {
            var args = n.Arguments.Select(context.Eval).ToList();
            context.Environment.Define(n.Target, badClass.Call(context.Interpreter, args));
        }
        else throw new Exception("Can only instantiate classes.");

        return HandlerResult.Continue;
    }
}
