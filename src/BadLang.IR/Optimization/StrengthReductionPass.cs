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

        public IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes)
        {
            var result = new List<IRNode>();

            foreach (var node in nodes)
            {
                if (node is IRBinary bin)
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

        private IRNode ReduceStrength(IRBinary bin)
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

        private bool IsConst2(IRValue value)
        {
            if (value is IRConst c && c.Value is double d)
            {
                // Due to floating point representation, just check equality for small integers
                return d == 2.0;
            }
            return false;
        }
    }
}
