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
        var nodes = new List<IRNode>
        {
            new IRBinary { Target = "y", Op = "*", Left = new IRVar("x"), Right = new IRConst(2.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Single(optimized);
        
        var bin = Assert.IsType<IRBinary>(optimized[0]);
        Assert.Equal("+", bin.Op);
        
        var left = Assert.IsType<IRVar>(bin.Left);
        var right = Assert.IsType<IRVar>(bin.Right);
        
        Assert.Equal("x", left.Name);
        Assert.Equal("x", right.Name);
    }

    [Fact]
    public void Apply_ReducesMultiplyByTwoLeft()
    {
        var pass = new StrengthReductionPass();
        var nodes = new List<IRNode>
        {
            new IRBinary { Target = "y", Op = "*", Left = new IRConst(2.0), Right = new IRVar("x") }
        };

        var optimized = pass.Apply(nodes);

        Assert.Single(optimized);
        
        var bin = Assert.IsType<IRBinary>(optimized[0]);
        Assert.Equal("+", bin.Op);
        
        var left = Assert.IsType<IRVar>(bin.Left);
        var right = Assert.IsType<IRVar>(bin.Right);
        
        Assert.Equal("x", left.Name);
        Assert.Equal("x", right.Name);
    }

    [Fact]
    public void Apply_DoesNotReduceMultiplyByThree()
    {
        var pass = new StrengthReductionPass();
        var nodes = new List<IRNode>
        {
            new IRBinary { Target = "y", Op = "*", Left = new IRVar("x"), Right = new IRConst(3.0) }
        };

        var optimized = pass.Apply(nodes);

        Assert.Single(optimized);
        
        var bin = Assert.IsType<IRBinary>(optimized[0]);
        Assert.Equal("*", bin.Op);
    }
}
