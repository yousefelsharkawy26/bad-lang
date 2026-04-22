using System.Collections.Generic;
using BadLang.IR;
using BadLang.IR.Optimization;
using Xunit;

namespace BadLang.Tests.Optimization;

public class LoopOptimizationPassTests
{
    [Fact]
    public void Apply_HoistsInvariantConstant()
    {
        var pass = new LoopOptimizationPass();
        var nodes = new List<IRNode>
        {
            new IRLabel { Name = "L1" },
            new IRAssign { Target = "x", Value = new IRConst(5.0) }, // Invariant, assigned once
            new IRBinary { Target = "y", Op = "+", Left = new IRVar("y"), Right = new IRVar("x") },
            new IRJump { TargetLabel = "L1" }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(4, optimized.Count);
        
        // x = 5 should be hoisted above L1
        var assign = Assert.IsType<IRAssign>(optimized[0]);
        Assert.Equal("x", assign.Target);
        
        var label = Assert.IsType<IRLabel>(optimized[1]);
        Assert.Equal("L1", label.Name);
    }

    [Fact]
    public void Apply_DoesNotHoistMultiplyAssigned()
    {
        var pass = new LoopOptimizationPass();
        var nodes = new List<IRNode>
        {
            new IRLabel { Name = "L1" },
            new IRAssign { Target = "x", Value = new IRConst(5.0) }, 
            new IRAssign { Target = "x", Value = new IRConst(6.0) }, 
            new IRJump { TargetLabel = "L1" }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(4, optimized.Count);
        
        // Label should still be first
        var label = Assert.IsType<IRLabel>(optimized[0]);
        Assert.Equal("L1", label.Name);
    }

    [Fact]
    public void Apply_HoistsFromCondJump()
    {
        var pass = new LoopOptimizationPass();
        var nodes = new List<IRNode>
        {
            new IRLabel { Name = "L1" },
            new IRAssign { Target = "x", Value = new IRConst(5.0) },
            new IRCondJump { Condition = new IRVar("c"), TrueLabel = "L1", FalseLabel = "L2" }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var assign = Assert.IsType<IRAssign>(optimized[0]);
        Assert.Equal("x", assign.Target);
        
        var label = Assert.IsType<IRLabel>(optimized[1]);
        Assert.Equal("L1", label.Name);
    }
}
