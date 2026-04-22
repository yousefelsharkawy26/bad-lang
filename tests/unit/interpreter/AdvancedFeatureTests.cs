using BadLang.Core;
using BadLang.Lexer;
using BadLang.Parser;
using BadLang.Backend.Interpreter;
using BadLang.IR;
using Xunit;
using System.IO;
using System.Collections.Generic;

namespace BadLang.Tests;

public class AdvancedFeatureTests
{
    private string Run(string source)
    {
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new BadLang.Parser.Parser(tokens);
        var statements = parser.Parse();
        
        using var sw = new StringWriter();
        var interpreter = new Interpreter(null, sw);

        var irBuilder = new IRBuilder();
        var irNodes = irBuilder.Build(statements);
        interpreter.Interpret(irNodes);
        
        return sw.ToString().Trim();
    }

    [Fact]
    public void Execute_ShortCircuit_And()
    {
        string source = @"
var sideEffect = false;
func setSideEffect() {
    sideEffect = true;
    return true;
}
var result = false && setSideEffect();
print(result);
print(sideEffect);
";
        string output = Run(source);
        Assert.Equal("FalseFalse", output);
    }

    [Fact]
    public void Execute_ShortCircuit_Or()
    {
        string source = @"
var sideEffect = false;
func setSideEffect() {
    sideEffect = true;
    return true;
}
var result = true || setSideEffect();
print(result);
print(sideEffect);
";
        string output = Run(source);
        Assert.Equal("TrueFalse", output);
    }

    [Fact]
    public void Execute_ToNumber()
    {
        string source = @"
var n = toNumber(""42.5"");
print(n + 0.5);
";
        string output = Run(source);
        Assert.Equal("43", output);
    }

    [Fact]
    public void Execute_Super()
    {
        string source = @"
class Base {
    func init() {}
    func greet() {
        return ""Base"";
    }
}
class Derived : Base {
    func init() {}
    func greet() {
        return super.greet() + ""Derived"";
    }
}
var d = new Derived();
print(d.greet());
";
        string output = Run(source);
        Assert.Equal("BaseDerived", output);
    }

    [Fact]
    public void Execute_TypeCast()
    {
        string source = @"
var x = ""123"" as number;
print(x + 1);
";
        string output = Run(source);
        Assert.Equal("124", output);
    }

    [Fact]
    public void Execute_TryCatchFinally_WithException()
    {
        string source = @"
try {
    throw ""error"";
} catch(e) {
    print(""Caught:"" + e);
} finally {
    print(""Finally"");
}
";
        string output = Run(source);
        // Using Trim() in Run helper, so check if spacing is correct
        Assert.Equal("Caught:errorFinally", output);
    }

    [Fact]
    public void Execute_TryFinally_Normal()
    {
        string source = @"
try {
    print(""Try"");
} finally {
    print(""Finally"");
}
";
        string output = Run(source);
        Assert.Equal("TryFinally", output);
    }
    [Fact]
    public void Execute_SuperConstructor()
    {
        string source = @"
class A {
    fn init(val) {
        this.val = val;
    }
}
class B : A {
    fn init(val, val2) {
        super.init(val);
        this.val2 = val2;
    }
}
var b = new B(10, 20);
print(b.val);
print("" "");
print(b.val2);
";
        string output = Run(source);
        Assert.Equal("10 20", output);
    }

    [Fact]
    public void Execute_ToNumberBuiltin()
    {
        string source = @"print(toNumber(""42"") + 8);";
        string output = Run(source);
        Assert.Equal("50", output);
    }

    [Fact]
    public void Execute_SuperPropertyAccess()
    {
        string source = @"
class A {
    fn init() {
        this.baseVal = 100;
    }
    fn getBase() { return this.baseVal; }
}
class B : A {
    fn init() {
        super.init();
        this.val = 200;
    }
    fn test() {
        return super.getBase();
    }
}
var b = new B();
print(b.test());
";
        string output = Run(source);
        Assert.Equal("100", output);
    }
}
