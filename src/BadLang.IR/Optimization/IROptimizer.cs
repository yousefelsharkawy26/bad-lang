using System.Collections.Generic;

namespace BadLang.IR.Optimization;

/// <summary>
/// Composes multiple optimization passes into a pipeline.
/// Runs each pass in sequence — the output of one is the input of the next.
/// </summary>
public class IROptimizer
{
    private readonly List<IOptimizationPass> _passes;

    public IROptimizer(IEnumerable<IOptimizationPass> passes)
    {
        _passes = new List<IOptimizationPass>(passes);
    }

    /// <summary>
    /// Run all passes in order. Returns the final optimized IR.
    /// The original input list is never mutated.
    /// </summary>
    public IReadOnlyList<IrNode> Optimize(IReadOnlyList<IrNode> nodes)
    {
        var result = nodes;
        foreach (var pass in _passes)
        {
            result = pass.Apply(result);
        }
        return result;
    }
}
