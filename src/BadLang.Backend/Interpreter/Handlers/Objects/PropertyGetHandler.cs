using System;
using System.Collections.Generic;
using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Objects;

public class PropertyGetHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRPropertyGet) };
    public bool CanHandle(IRNode node) => node is IRPropertyGet;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var pget = (IRPropertyGet)node;
        var obj = context.Eval(pget.Object);
        
        if (obj is BadLangInstance instance)
        {
            context.Environment.Define(pget.Target, instance.Get(new Token(TokenType.Identifier, pget.Property, null, 0, 0, 0)));
        }
        else if (obj is Dictionary<object, object?> dict)
        {
            context.Environment.Define(pget.Target, dict.TryGetValue(pget.Property, out var v) ? v : null);
        }
        else throw new Exception("Only instances and maps have properties.");

        return HandlerResult.Continue;
    }
}
