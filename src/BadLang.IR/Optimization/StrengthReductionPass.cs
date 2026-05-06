using System.Collections.Generic;

namespace BadLang.IR.Optimization
{
    /// <summary>
    /// Identifies and replaces computationally expensive `IRBinary` operations with cheaper equivalents.
    /// E.g., `x * 2` -> `x + x`.
    /// </summary>
    public class StrengthReductionPass : IOptimizationPass
    {
        public string Name => "Strength Reduction";

        public IReadOnlyList<IrNode> Apply(IReadOnlyList<IrNode> nodes)
        {
            var result = new List<IrNode>();

            foreach (var node in nodes)
            {
                if (node is IrBinary bin)
                {
                    result.Add(ReduceStrength(bin));
                }
                else
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private IrNode ReduceStrength(IrBinary bin)
        {
            if (bin.Op == "*")
            {
                // x * 2 -> x + x
                if (IsConst2(bin.Right))
                {
                    return bin with { Op = "+", Right = bin.Left }; // Left + Left
                }
                
                // 2 * x -> x + x
                if (IsConst2(bin.Left))
                {
                    return bin with { Op = "+", Left = bin.Right }; // Right + Right
                }
            }

            return bin;
        }

        private bool IsConst2(IrValue value)
        {
            if (value is IrConst c && c.Value is double d)
            {
                // Due to floating point representation, just check equality for small integers
                return d == 2.0;
            }
            return false;
        }
    }
}
