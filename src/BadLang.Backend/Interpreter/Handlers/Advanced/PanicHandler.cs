using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class PanicHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrPanic)];
    public bool CanHandle(IrNode node) => node is IrPanic;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var panic = (IrPanic)node;
        throw new Exception("panic: " + context.Eval(panic.Message));
    }
}
