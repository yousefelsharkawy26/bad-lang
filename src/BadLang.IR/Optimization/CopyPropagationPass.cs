using System.Collections.Generic;
using System.Linq;

namespace BadLang.IR.Optimization
{
    /// <summary>
    /// Tracks `IRAssign` nodes where a variable is assigned another variable (`x = y`).
    /// Replaces subsequent uses of `x` with `y` as long as neither `x` nor `y` are modified.
    /// Due to linear IR, it conservatively invalidates mappings if any branch target is hit or aliased variables are reassigned.
    /// </summary>
    public class CopyPropagationPass : IOptimizationPass
    {
        public string Name => "Copy Propagation";

        public IReadOnlyList<IrNode> Apply(IReadOnlyList<IrNode> nodes)
        {
            var result = new List<IrNode>();
            var knownAliases = new Dictionary<string, string>(); // target -> source

            foreach (var node in nodes)
            {
                // Invalidate all tracked state on jump targets (labels) to be safe across branches
                if (node is IrLabel || node is IrTry || node is IrEnterScope || node is IrExitScope)
                {
                    knownAliases.Clear();
                }

                if (node is IrAssign assign)
                {
                    var newValue = ResolveValue(assign.Value, knownAliases);
                    
                    if (newValue is IrVar v)
                    {
                        // x = y
                        InvalidateVariable(assign.Target, knownAliases);
                        knownAliases[assign.Target] = v.Name;
                    }
                    else
                    {
                        InvalidateVariable(assign.Target, knownAliases);
                    }
                    
                    result.Add(assign with { Value = newValue });
                }
                else if (node is IrBinary bin)
                {
                    InvalidateVariable(bin.Target, knownAliases);
                    result.Add(bin with { 
                        Left = ResolveValue(bin.Left, knownAliases), 
                        Right = ResolveValue(bin.Right, knownAliases) 
                    });
                }
                else if (node is IrUnary un)
                {
                    InvalidateVariable(un.Target, knownAliases);
                    result.Add(un with { Operand = ResolveValue(un.Operand, knownAliases) });
                }
                else if (node is IrCondJump cj)
                {
                    result.Add(cj with { Condition = ResolveValue(cj.Condition, knownAliases) });
                }
                else if (node is IrReturn ret && ret.Value != null)
                {
                    result.Add(ret with { Value = ResolveValue(ret.Value, knownAliases) });
                }
                else if (node is IrThrow thr)
                {
                    result.Add(thr with { Exception = ResolveValue(thr.Exception, knownAliases) });
                }
                else if (node is IrCall call)
                {
                    InvalidateVariable(call.Target, knownAliases);
                    var newArgs = new List<IrValue>(call.Arguments.Count);
                    foreach (var arg in call.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(call with { Arguments = newArgs });
                }
                else if (node is IrMethodCall mcall)
                {
                    InvalidateVariable(mcall.Target, knownAliases);
                    var newArgs = new List<IrValue>(mcall.Arguments.Count);
                    foreach (var arg in mcall.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(mcall with { 
                        Object = ResolveValue(mcall.Object, knownAliases),
                        Arguments = newArgs 
                    });
                }
                else if (node is IrLoad load)
                {
                    InvalidateVariable(load.Target, knownAliases);
                    result.Add(load);
                }
                else if (node is IrStore store)
                {
                    InvalidateVariable(store.VariableName, knownAliases);
                    result.Add(store with { Value = ResolveValue(store.Value, knownAliases) });
                }
                else if (node is IrDefine def)
                {
                    InvalidateVariable(def.VariableName, knownAliases);
                    result.Add(def with { Value = ResolveValue(def.Value, knownAliases) });
                }
                else if (node is IrPropertyGet pget)
                {
                    InvalidateVariable(pget.Target, knownAliases);
                    result.Add(pget with { Object = ResolveValue(pget.Object, knownAliases) });
                }
                else if (node is IrPropertySet pset)
                {
                    result.Add(pset with { 
                        Object = ResolveValue(pset.Object, knownAliases),
                        Value = ResolveValue(pset.Value, knownAliases) 
                    });
                }
                else if (node is IrIndexGet iget)
                {
                    InvalidateVariable(iget.Target, knownAliases);
                    result.Add(iget with { 
                        ArrayOrMap = ResolveValue(iget.ArrayOrMap, knownAliases),
                        Index = ResolveValue(iget.Index, knownAliases)
                    });
                }
                else if (node is IrIndexSet iset)
                {
                    result.Add(iset with { 
                        ArrayOrMap = ResolveValue(iset.ArrayOrMap, knownAliases),
                        Index = ResolveValue(iset.Index, knownAliases),
                        Value = ResolveValue(iset.Value, knownAliases)
                    });
                }
                else if (node is IrNewArray narr)
                {
                    InvalidateVariable(narr.Target, knownAliases);
                    var newElems = new List<IrValue>(narr.Elements.Count);
                    foreach (var el in narr.Elements) newElems.Add(ResolveValue(el, knownAliases));
                    result.Add(narr with { Elements = newElems });
                }
                else if (node is IrNewMap nmap)
                {
                    InvalidateVariable(nmap.Target, knownAliases);
                    result.Add(nmap);
                }
                else if (node is IrNew newObj)
                {
                    InvalidateVariable(newObj.Target, knownAliases);
                    var newArgs = new List<IrValue>(newObj.Arguments.Count);
                    foreach (var arg in newObj.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(newObj with { 
                        Class = ResolveValue(newObj.Class, knownAliases),
                        Arguments = newArgs 
                    });
                }
                else if (node is IrLambda lam)
                {
                    InvalidateVariable(lam.Target, knownAliases);
                    result.Add(lam);
                }
                else if (node is IrSuperPropertyGet spg)
                {
                    InvalidateVariable(spg.Target, knownAliases);
                    result.Add(spg);
                }
                else if (node is IrSuperMethodCall smc)
                {
                    InvalidateVariable(smc.Target, knownAliases);
                    var newArgs = new List<IrValue>(smc.Arguments.Count);
                    foreach (var arg in smc.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(smc with { Arguments = newArgs });
                }
                else if (node is IrAssert ast)
                {
                    result.Add(ast with { 
                        Condition = ResolveValue(ast.Condition, knownAliases),
                        Message = ResolveValue(ast.Message, knownAliases)
                    });
                }
                else if (node is IrPanic pan)
                {
                    result.Add(pan with { Message = ResolveValue(pan.Message, knownAliases) });
                }
                else
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private IrValue ResolveValue(IrValue value, Dictionary<string, string> knownAliases)
        {
            if (value is IrVar v && knownAliases.TryGetValue(v.Name, out var source))
            {
                return new IrVar(source);
            }
            return value;
        }

        private void InvalidateVariable(string varName, Dictionary<string, string> knownAliases)
        {
            // If the variable is assigned to, it's no longer an alias for whatever it was.
            knownAliases.Remove(varName);

            // Also, any aliases that point TO this variable are now invalid because its value changed.
            var keysToRemove = knownAliases.Where(kvp => kvp.Value == varName).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                knownAliases.Remove(key);
            }
        }
    }
}
