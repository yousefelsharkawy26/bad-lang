using System.Collections.Generic;
using BadLang.IR;
using BadLang.IR.Optimization;
using Xunit;

namespace BadLang.Tests.Optimization;

public class StrengthReductionPassTests
{
    [Fact]
    public void Apply_ReducesMultiplyByTwoRight()
    {
        var pass = new StrengthReductionPass();
        var nodes = new List<IrNode>
        {
            new IrBinary { Target = "y", Op = "*", Left = new IrVar("x"), Right = new IrConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Single(optimized);
        
        var bin = Assert.IsType<IrBinary>(optimized[0]);
        Assert.Equal("+", bin.Op);
        
        var left = Assert.IsType<IrVar>(bin.Left);
        var right = Assert.IsType<IrVar>(bin.Right);
        
        Assert.Equal("x", left.Name);
        Assert.Equal("x", right.Name);
    }

    [Fact]
    public void Apply_ReducesMultiplyByTwoLeft()
    {
        var pass = new StrengthReductionPass();
        var nodes = new List<IrNode>
        {
            new IrBinary { Target = "y", Op = "*", Left = new IrConst(2.0), Right = new IrVar("x") }
        };

        var optimized = pass.Apply(nodes);

        Assert.Single(optimized);
        
        var bin = Assert.IsType<IrBinary>(optimized[0]);
        Assert.Equal("+", bin.Op);
        
        var left = Assert.IsType<IrVar>(bin.Left);
        var right = Assert.IsType<IrVar>(bin.Right);
        
        Assert.Equal("x", left.Name);
        Assert.Equal("x", right.Name);
    }

    [Fact]
    public void Apply_DoesNotReduceMultiplyByThree()
    {
        var pass = new StrengthReductionPass();
        var nodes = new List<IrNode>
        {
            new IrBinary { Target = "y", Op = "*", Left = new IrVar("x"), Right = new IrConst(3.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Single(optimized);
        
        var bin = Assert.IsType<IrBinary>(optimized[0]);
        Assert.Equal("*", bin.Op);
    }
}
