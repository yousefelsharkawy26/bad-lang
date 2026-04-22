using System.Collections.Generic;
using BadLang.IR;
using BadLang.IR.Optimization;
using Xunit;

namespace BadLang.Tests.Optimization;

public class ConstantPropagationPassTests
{
    [Fact]
    public void Apply_PropagatesConstants()
    {
        var pass = new ConstantPropagationPass();
        var nodes = new List<IRNode>
        {
            new IRAssign { Target = "x", Value = new IRConst(5.0) },
            new IRBinary { Target = "y", Op = "+", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(2, optimized.Count);
        
        var bin = Assert.IsType<IRBinary>(optimized[1]);
        Assert.Equal("y", bin.Target);
        Assert.Equal("+", bin.Op);
        
        var left = Assert.IsType<IRConst>(bin.Left);
        Assert.Equal(5.0, left.Value);
    }

    [Fact]
    public void Apply_InvalidatesOnLabel()
    {
        var pass = new ConstantPropagationPass();
        var nodes = new List<IRNode>
        {
            new IRAssign { Target = "x", Value = new IRConst(5.0) },
            new IRLabel { Name = "L1" },
            new IRBinary { Target = "y", Op = "+", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IRBinary>(optimized[2]);
        var left = Assert.IsType<IRVar>(bin.Left);
        Assert.Equal("x", left.Name);
    }

    [Fact]
    public void Apply_InvalidatesOnReassignment()
    {
        var pass = new ConstantPropagationPass();
        var nodes = new List<IRNode>
        {
            new IRAssign { Target = "x", Value = new IRConst(5.0) },
            new IRBinary { Target = "x", Op = "+", Left = new IRConst(1.0), Right = new IRConst(2.0) },
            new IRBinary { Target = "y", Op = "+", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IRBinary>(optimized[2]);
        var left = Assert.IsType<IRVar>(bin.Left);
        Assert.Equal("x", left.Name);
    }
}
