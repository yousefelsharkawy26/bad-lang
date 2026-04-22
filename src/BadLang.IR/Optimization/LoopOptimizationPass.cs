using System.Collections.Generic;
using System.Linq;

namespace BadLang.IR.Optimization
{
    /// <summary>
    /// Implements simplified Loop-Invariant Code Motion (LICM).
    /// Detects loops by matching backward `IRJump` or `IRCondJump` to an earlier `IRLabel`.
    /// Hoists invariant assignments (e.g., constants) above the loop's start label.
    /// </summary>
    public class LoopOptimizationPass : IOptimizationPass
    {
        public string Name => "Loop Optimization";

        public IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes)
        {
            var result = new List<IRNode>(nodes);
            bool changed = true;

            // Run until no more code can be hoisted
            while (changed)
            {
                changed = false;

                // Find all labels and their indices
                var labels = new Dictionary<string, int>();
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is IRLabel label)
                    {
                        labels[label.Name] = i;
                    }
                }

                // Find the first loop that we can optimize
                for (int i = 0; i < result.Count; i++)
                {
                    string? targetLabel = null;
                    if (result[i] is IRJump jump)
                    {
                        targetLabel = jump.TargetLabel;
                    }
                    else if (result[i] is IRCondJump cjump)
                    {
                        targetLabel = cjump.TrueLabel; // Only check true label for backward jumps typically
                        if (!labels.ContainsKey(targetLabel) || labels[targetLabel] > i)
                        {
                            targetLabel = cjump.FalseLabel;
                        }
                    }

                    if (targetLabel != null && labels.TryGetValue(targetLabel, out int startIndex) && startIndex < i)
                    {
                        // Loop found from startIndex to i
                        if (TryHoistInvariant(result, startIndex, i))
                        {
                            changed = true;
                            break; // Restart the outer while loop as indices have changed
                        }
                    }
                }
            }

            return result;
        }

        private bool TryHoistInvariant(List<IRNode> nodes, int startIndex, int endIndex)
        {
            // Find an invariant assignment: IRAssign with IRConst
            // Ensure the target is only assigned ONCE in the loop.
            var assignmentCounts = new Dictionary<string, int>();
            
            for (int i = startIndex; i <= endIndex; i++)
            {
                string? target = GetTarget(nodes[i]);
                if (target != null)
                {
                    if (assignmentCounts.ContainsKey(target))
                        assignmentCounts[target]++;
                    else
                        assignmentCounts[target] = 1;
                }
            }

            // Now find the first hoistable node
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (nodes[i] is IRAssign assign && assign.Value is IRConst && assignmentCounts.TryGetValue(assign.Target, out int count) && count == 1)
                {
                    // Hoist it!
                    var nodeToHoist = nodes[i];
                    nodes.RemoveAt(i);
                    nodes.Insert(startIndex, nodeToHoist);
                    return true;
                }
            }

            return false;
        }

        private string? GetTarget(IRNode node)
        {
            return node switch
            {
                IRAssign a => a.Target,
                IRBinary b => b.Target,
                IRUnary u => u.Target,
                IRCall c => c.Target,
                IRMethodCall mc => mc.Target,
                IRLoad l => l.Target,
                IRStore s => s.VariableName, // Treat stores to named variables as modifications
                IRDefine d => d.VariableName,
                IRPropertyGet pget => pget.Target,
                IRIndexGet iget => iget.Target,
                IRNewArray narr => narr.Target,
                IRNewMap nmap => nmap.Target,
                IRNew n => n.Target,
                IRLambda lam => lam.Target,
                IRSuperPropertyGet spg => spg.Target,
                IRSuperMethodCall smc => smc.Target,
                _ => null
            };
        }
    }
}
