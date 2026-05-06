using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class BinaryHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IrBinary) };
    public bool CanHandle(IrNode node) => node is IrBinary;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var bin = (IrBinary)node;
        var left = context.Eval(bin.Left);
        var right = context.Eval(bin.Right);
        var result = Evaluator.EvaluateBinary(bin.Op, left, right);
        context.Environment.Define(bin.Target, result);
        
        return HandlerResult.Continue;
    }
}
