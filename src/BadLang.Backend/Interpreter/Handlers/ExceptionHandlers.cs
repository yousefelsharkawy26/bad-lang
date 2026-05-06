using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ExceptionHandlers : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IrTry), typeof(IrPopTry), typeof(IrThrow) };
    public bool CanHandle(IrNode node) => 
        node is IrTry || 
        node is IrPopTry || 
        node is IrThrow;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var env = context.Environment;
        var tryStack = context.TryStack;
        var interpreter = context.Interpreter;

        switch (node)
        {
            case IrTry tryNode:
                tryStack.Push(new TryContext
                {
                    CatchLabel = tryNode.CatchLabel,
                    FinallyLabel = tryNode.FinallyLabel,
                    SavedEnv = env
                });
                break;
            case IrPopTry _:
                tryStack.Pop();
                break;
            case IrThrow thrw:
                throw new BadLangException(interpreter.GetValueInternal(thrw.Exception, env));
        }
        
        return HandlerResult.Continue;
    }
}
