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
        var nodes = new List<IrNode>
        {
            new IrAssign { Target = "x", Value = new IrConst(5.0) },
            new IrBinary { Target = "y", Op = "+", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(2, optimized.Count);
        
        var bin = Assert.IsType<IrBinary>(optimized[1]);
        Assert.Equal("y", bin.Target);
        Assert.Equal("+", bin.Op);
        
        var left = Assert.IsType<IrConst>(bin.Left);
        Assert.Equal(5.0, left.Value);
    }

    [Fact]
    public void Apply_InvalidatesOnLabel()
    {
        var pass = new ConstantPropagationPass();
        var nodes = new List<IrNode>
        {
            new IrAssign { Target = "x", Value = new IrConst(5.0) },
            new IrLabel { Name = "L1" },
            new IrBinary { Target = "y", Op = "+", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IrBinary>(optimized[2]);
        var left = Assert.IsType<IrVar>(bin.Left);
        Assert.Equal("x", left.Name);
    }

    [Fact]
    public void Apply_InvalidatesOnReassignment()
    {
        var pass = new ConstantPropagationPass();
        var nodes = new List<IrNode>
        {
            new IrAssign { Target = "x", Value = new IrConst(5.0) },
            new IrBinary { Target = "x", Op = "+", Left = new IrConst(1.0), Right = new IrConst(2.0) },
            new IrBinary { Target = "y", Op = "+", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var bin = Assert.IsType<IrBinary>(optimized[2]);
        var left = Assert.IsType<IrVar>(bin.Left);
        Assert.Equal("x", left.Name);
    }
}
