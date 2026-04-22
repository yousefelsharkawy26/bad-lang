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
        var nodes = new List<IRNode>
        {
            new IRAssign { Target = "x", Value = new IRVar("y") },
            new IRBinary { Target = "z", Op = "+", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(2, optimized.Count);
        
        var bin = Assert.IsType<IRBinary>(optimized[1]);
        var left = Assert.IsType<IRVar>(bin.Left);
        Assert.Equal("y", left.Name);
    }

    [Fact]
    public void Apply_InvalidatesOnSourceModification()
    {
        var pass = new CopyPropagationPass();
        var nodes = new List<IRNode>
        {
            new IRAssign { Target = "x", Value = new IRVar("y") },
            new IRAssign { Target = "y", Value = new IRConst(10.0) },
            new IRBinary { Target = "z", Op = "+", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IRBinary>(optimized[2]);
        var left = Assert.IsType<IRVar>(bin.Left);
        Assert.Equal("x", left.Name); // Should not have propagated because y changed
    }

    [Fact]
    public void Apply_InvalidatesOnTargetModification()
    {
        var pass = new CopyPropagationPass();
        var nodes = new List<IRNode>
        {
            new IRAssign { Target = "x", Value = new IRVar("y") },
            new IRAssign { Target = "x", Value = new IRVar("z") },
            new IRBinary { Target = "w", Op = "+", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IRBinary>(optimized[2]);
        var left = Assert.IsType<IRVar>(bin.Left);
        Assert.Equal("z", left.Name); // Propagated the SECOND copy
    }
}
