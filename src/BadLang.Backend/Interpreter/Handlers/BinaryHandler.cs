using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class BinaryHandler : IIRNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IRBinary) };
    public bool CanHandle(IRNode node) => node is IRBinary;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var bin = (IRBinary)node;
        var left = context.Eval(bin.Left);
        var right = context.Eval(bin.Right);
        var result = Evaluator.EvaluateBinary(bin.Op, left, right);
        context.Environment.Define(bin.Target, result);
        
        return HandlerResult.Continue;
    }
}
