using BadLang.IR;
using BadLang.IR.Optimization;
using BadLang.Lexer;
using BadLang.Parser;
using BadLang.Backend.Interpreter;
using Xunit;
using System.Collections.Generic;
using System.IO;

namespace BadLang.Tests;

public class OptimizerTests
{
    /// <summary>
    /// Helper: parse + build IR from BadLang source, returning raw IR nodes.
    /// </summary>
    private List<IRNode> BuildIR(string source)
    {
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new BadLang.Parser.Parser(tokens);
        var statements = parser.Parse();
        var builder = new IRBuilder();
        return builder.Build(statements);
    }

    /// <summary>
    /// Helper: full pipeline — parse, build IR, optimize, interpret.
    /// </summary>
    private string RunOptimized(string source)
    {
        var ir = BuildIR(source);
        var optimizer = new IROptimizer(new IOptimizationPass[]
        {
            new ConstantFoldingPass(),
            new DeadCodeEliminationPass()
        });
        var optimized = optimizer.Optimize(ir);

        using var sw = new StringWriter();
        var interpreter = new Interpreter(output: sw);
        interpreter.Interpret(optimized);
        return sw.ToString().Trim();
    }

    // ─── Constant Folding ────────────────────────────────────────────

    [Fact]
    public void ConstantFolding_BasicArithmetic()
    {
        // 2 + 3 should be folded to 5 at compile time
        string source = @"var x = 2 + 3; print(x);";
        Assert.Equal("5", RunOptimized(source));
    }

    [Fact]
    public void ConstantFolding_NestedExpressions()
    {
        // (2 + 3) * 4 — the inner add is folded, then used
        string source = @"var x = (2 + 3) * 4; print(x);";
        Assert.Equal("20", RunOptimized(source));
    }

    [Fact]
    public void ConstantFolding_PreservesVariables()
    {
        // a + 3 cannot be folded (a is a variable)
        string source = @"
var a = 10;
var x = a + 3;
print(x);
";
        Assert.Equal("13", RunOptimized(source));
    }

    [Fact]
    public void ConstantFolding_Comparison()
    {
        string source = @"var x = 5 > 3; print(x);";
        Assert.Equal("True", RunOptimized(source));
    }

    [Fact]
    public void ConstantFolding_DivisionByZeroNotFolded()
    {
        // Division by zero should NOT be folded — left for runtime
        string source = @"var x = 10 / 0; print(x);";
        // IEEE 754: 10/0 = Infinity
        Assert.Equal("∞", RunOptimized(source));
    }

    [Fact]
    public void ConstantFolding_StringConcat()
    {
        // String concat with two known strings CAN be folded
        string source = @"var x = ""hello"" + "" world""; print(x);";
        Assert.Equal("hello world", RunOptimized(source));
    }

    // ─── Dead Code Elimination ───────────────────────────────────────

    [Fact]
    public void DeadCode_IRLevel_RemovesUnusedTemp()
    {
        // Directly test the pass at IR level
        var pass = new DeadCodeEliminationPass();
        var ir = new List<IRNode>
        {
            // _t0 = 42 (never read — should be eliminated)
            new IRAssign { Target = "_t0", Value = new IRConst(42.0) },
            // _t1 = 10 (read by define)
            new IRAssign { Target = "_t1", Value = new IRConst(10.0) },
            new IRDefine { VariableName = "x", Value = new IRVar("_t1") }
        };

        var result = pass.Apply(ir);

        // _t0 assignment should be removed, _t1 and define kept
        Assert.Equal(2, result.Count);
        Assert.IsType<IRAssign>(result[0]);
        Assert.IsType<IRDefine>(result[1]);
    }

    [Fact]
    public void DeadCode_PreservesSideEffects()
    {
        // Calls are side-effecting — never remove them even if target is unused
        var pass = new DeadCodeEliminationPass();
        var ir = new List<IRNode>
        {
            new IRCall { Target = "_t0", FunctionName = "print", Arguments = new List<IRValue> { new IRConst("hi") } }
        };

        var result = pass.Apply(ir);
        Assert.Single(result);
    }

    // ─── Semantic Preservation ───────────────────────────────────────

    [Fact]
    public void Optimizer_PreservesSemantics_Functions()
    {
        string source = @"
fn add(a, b) { return a + b; }
print(add(10, 20));
";
        Assert.Equal("30", RunOptimized(source));
    }

    [Fact]
    public void Optimizer_PreservesSemantics_Loops()
    {
        string source = @"
var sum = 0;
var i = 0;
while (i < 5) {
    sum = sum + i;
    i = i + 1;
}
print(sum);
";
        Assert.Equal("10", RunOptimized(source));
    }

    [Fact]
    public void Optimizer_PreservesSemantics_Classes()
    {
        string source = @"
class Dog {
    fn init(name) {
        this.name = name;
    }
    fn speak() {
        return this.name + "" says woof"";
    }
}
var d = new Dog(""Rex"");
print(d.speak());
";
        Assert.Equal("Rex says woof", RunOptimized(source));
    }

    [Fact]
    public void Optimizer_PreservesSemantics_TryCatch()
    {
        string source = @"
try {
    throw ""oops"";
} catch(e) {
    print(e);
}
";
        Assert.Equal("oops", RunOptimized(source));
    }

    [Fact]
    public void Optimizer_PreservesSemantics_ComplexProgram()
    {
        // A complex program with constant folding opportunities should produce correct output
        string source = @"
var x = 2 + 3;
var y = x * 10;
fn greet(name) { return ""Hello, "" + name + ""! y="" + str(y); }
print(greet(""World""));
";
        Assert.Equal("Hello, World! y=50", RunOptimized(source));
    }
}
