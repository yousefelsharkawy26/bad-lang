using BadLang.Core;
using BadLang.Lexer;
using BadLang.Parser;
using BadLang.Backend.Interpreter;
using BadLang.IR;

namespace BadLang.Tests;

public class InterpreterTests
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
    public void Execute_HelloWorld()
    {
        string source = "print(\"Hello World\");";
        string output = Run(source);
        Assert.Equal("Hello World", output);
    }

    [Fact]
    public void Execute_Arithmetic()
    {
        string source = "print(1 + 2 * 3);";
        string output = Run(source);
        Assert.Equal("7", output);
    }

    [Fact]
    public void Execute_InheritanceAndSuper()
    {
        string source = @"
class A {
    fn test() { println(""A""); }
}
class B : A {
    fn test() { 
        super.test();
        println(""B""); 
    }
}
var b = new B();
b.test();
";
        string output = Run(source);
        Assert.Equal("A\nB", output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Execute_TryCatchFinally()
    {
        string source = @"
try {
    throw ""error"";
} catch(e) {
    println(""Caught: "" + e);
} finally {
    println(""Finally"");
}
";
        string output = Run(source);
        Assert.Equal("Caught: error\nFinally", output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Execute_StandardLibrary()
    {
        string source = @"
var arr = [1, 2, 3];
println(len(arr));
println(str(123));
println(num(""456"") + 1);
";
        string output = Run(source);
        Assert.Equal("3\n123\n457", output.Replace("\r\n", "\n"));
    }
    [Fact]
    public void Execute_MapLiteral()
    {
        string source = @"
            var m = { ""a"": 1, ""b"": 2 };
            println(m[""a""]);
            m[""c""] = 3;
            println(m[""c""]);
        ";
        string output = Run(source);
        Assert.Equal("1\n3", output.Trim().Replace("\r\n", "\n"));
    }
    [Fact]
    public void Execute_ForIn()
    {
        string source = @"
            var arr = [1, 2, 3];
            var sum = 0;
            for x in arr {
                sum = sum + x;
            }
            print(sum);
        ";
        string output = Run(source);
        Assert.Equal("6", output.Trim());
    }

    [Fact]
    public void Execute_InterpolatedString()
    {
        string source = @"
            var name = ""BadLang"";
            var version = 1.0;
            print($""Welcome to {name} v{version}!"");
        ";
        string output = Run(source);
        Assert.Equal("Welcome to BadLang v1!", output.Trim());
    }

    [Fact]
    public void Execute_MathModule()
    {
        string source = @"
            println(math_sqrt(16));
            println(math_abs(-10));
        ";
        string output = Run(source);
        Assert.Equal("4\n10", output.Trim().Replace("\r\n", "\n"));
    }

    [Fact]
    public void Execute_IOModule()
    {
        string tempFile = "test_io.txt";
        string source = $@"
            io_write_file(""{tempFile}"", ""Hello BadLang"");
            print(io_read_file(""{tempFile}""));
        ";
        string output = Run(source);
        Assert.Equal("Hello BadLang", output.Trim());
        if (File.Exists(tempFile)) File.Delete(tempFile);
    }

    [Fact]
    public void Execute_IntAlias()
    {
        string source = @"
            int x = 10;
            num y = 20;
            print(x + y);
        ";
        string output = Run(source);
        Assert.Equal("30", output.Trim());
    }
}
