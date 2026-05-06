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
        var nodes = new List<IrNode>
        {
            new IrLabel { Name = "L1" },
            new IrAssign { Target = "x", Value = new IrConst(5.0) }, // Invariant, assigned once
            new IrBinary { Target = "y", Op = "+", Left = new IrVar("y"), Right = new IrVar("x") },
            new IrJump { TargetLabel = "L1" }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(4, optimized.Count);
        
        // x = 5 should be hoisted above L1
        var assign = Assert.IsType<IrAssign>(optimized[0]);
        Assert.Equal("x", assign.Target);
        
        var label = Assert.IsType<IrLabel>(optimized[1]);
        Assert.Equal("L1", label.Name);
    }

    [Fact]
    public void Apply_DoesNotHoistMultiplyAssigned()
    {
        var pass = new LoopOptimizationPass();
        var nodes = new List<IrNode>
        {
            new IrLabel { Name = "L1" },
            new IrAssign { Target = "x", Value = new IrConst(5.0) }, 
            new IrAssign { Target = "x", Value = new IrConst(6.0) }, 
            new IrJump { TargetLabel = "L1" }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(4, optimized.Count);
        
        // Label should still be first
        var label = Assert.IsType<IrLabel>(optimized[0]);
        Assert.Equal("L1", label.Name);
    }

    [Fact]
    public void Apply_HoistsFromCondJump()
    {
        var pass = new LoopOptimizationPass();
        var nodes = new List<IrNode>
        {
            new IrLabel { Name = "L1" },
            new IrAssign { Target = "x", Value = new IrConst(5.0) },
            new IrCondJump { Condition = new IrVar("c"), TrueLabel = "L1", FalseLabel = "L2" }
        };

        var optimized = pass.Apply(nodes);

        Assert.Equal(3, optimized.Count);
        
        var assign = Assert.IsType<IrAssign>(optimized[0]);
        Assert.Equal("x", assign.Target);
        
        var label = Assert.IsType<IrLabel>(optimized[1]);
        Assert.Equal("L1", label.Name);
    }
}
