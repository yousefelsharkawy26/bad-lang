using System.Collections.Generic;
using BadLang.IR;
using BadLang.IR.Optimization;
using Xunit;

namespace BadLang.Tests.Optimization;

public class CopyPropagationPassTests
{
    [Fact]
    public void Apply_PropagatesCopies()
    {
        var pass = new CopyPropagationPass();
        var nodes = new List<IrNode>
        {
            new IrAssign { Target = "x", Value = new IrVar("y") },
            new IrBinary { Target = "z", Op = "+", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(2, optimized.Count);
        
        var bin = Assert.IsType<IrBinary>(optimized[1]);
        var left = Assert.IsType<IrVar>(bin.Left);
        Assert.Equal("y", left.Name);
    }

    [Fact]
    public void Apply_InvalidatesOnSourceModification()
    {
        var pass = new CopyPropagationPass();
        var nodes = new List<IrNode>
        {
            new IrAssign { Target = "x", Value = new IrVar("y") },
            new IrAssign { Target = "y", Value = new IrConst(10.0) },
            new IrBinary { Target = "z", Op = "+", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IrBinary>(optimized[2]);
        var left = Assert.IsType<IrVar>(bin.Left);
        Assert.Equal("x", left.Name); // Should not have propagated because y changed
    }

    [Fact]
    public void Apply_InvalidatesOnTargetModification()
    {
        var pass = new CopyPropagationPass();
        var nodes = new List<IrNode>
        {
            new IrAssign { Target = "x", Value = new IrVar("y") },
            new IrAssign { Target = "x", Value = new IrVar("z") },
            new IrBinary { Target = "w", Op = "+", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IrBinary>(optimized[2]);
        var left = Assert.IsType<IrVar>(bin.Left);
        Assert.Equal("z", left.Name); // Propagated the SECOND copy
    }
}
