using System.Collections.Generic;

namespace BadLang.IR.Optimization;

/// <summary>
/// Represents a single optimization pass over the IR.
/// Each pass takes an IR list and returns a new (potentially optimized) IR list.
/// Passes must be pure — they never mutate the input list.
/// </summary>
public interface IOptimizationPass
{
    /// <summary>
    /// Human-readable name for diagnostics and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Apply this optimization pass to the given IR nodes.
    /// Returns a new list — the input list is never mutated.
    /// </summary>
    IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes);
}
