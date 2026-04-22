using System.Collections.Generic;
using System.Linq;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ValueHandlers : IIRNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { 
        typeof(IRUnary), typeof(IRNewArray), typeof(IRNewMap), typeof(IRIndexGet), typeof(IRIndexSet) 
    };
    public bool CanHandle(IRNode node) => 
        node is IRUnary || 
        node is IRNewArray || 
        node is IRNewMap ||
        node is IRIndexGet || 
        node is IRIndexSet;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var env = context.Environment;
        var interpreter = context.Interpreter;

        switch (node)
        {
            case IRUnary un:
                env.Define(un.Target, Evaluator.EvaluateUnary(un.Op, context.Eval(un.Operand)));
                break;
            case IRNewArray narr:
                env.Define(narr.Target, narr.Elements.Select(e => context.Eval(e)).ToList());
                break;
            case IRNewMap nmap:
                env.Define(nmap.Target, new Dictionary<object, object?>());
                break;
            case IRIndexGet iget:
                {
                    var arr = context.Eval(iget.ArrayOrMap);
                    var idx = context.Eval(iget.Index);
                    if (arr is List<object?> list) {
                        int i = (int)(double)idx!;
                        env.Define(iget.Target, list[i]);
                    } else if (arr is string s) {
                        int i = (int)(double)idx!;
                        env.Define(iget.Target, s[i].ToString());
                    } else if (arr is Dictionary<object, object?> dict) {
                        env.Define(iget.Target, dict.TryGetValue(idx!, out var v) ? v : null);
                    }
                }
                break;
            case IRIndexSet iset:
                {
                    var arr = context.Eval(iset.ArrayOrMap);
                    var idx = context.Eval(iset.Index);
                    var val = context.Eval(iset.Value);
                    if (arr is List<object?> list) {
                        int i = (int)(double)idx!;
                        list[i] = val;
                    } else if (arr is Dictionary<object, object?> dict) {
                        dict[idx!] = val;
                    }
                }
                break;
        }
        
        return HandlerResult.Continue;
    }
}
