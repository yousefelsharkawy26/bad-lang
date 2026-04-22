using BadLang.Core;
using BadLang.Lexer;
using BadLang.Parser;
using BadLang.Backend.Interpreter;
using BadLang.IR;
using Xunit;
using System.IO;
using System.Collections.Generic;

namespace BadLang.Tests;

public class ImportTests : IDisposable
{
    private readonly string _tempDir;

    public ImportTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private string Run(string source)
    {
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new BadLang.Parser.Parser(tokens);
        var statements = parser.Parse();
        
        using var sw = new StringWriter();
        var interpreter = new Interpreter(_tempDir, sw);

        var irBuilder = new IRBuilder();
        var irNodes = irBuilder.Build(statements);
        interpreter.Interpret(irNodes);
        
        return sw.ToString().Trim();
    }

    [Fact]
    public void Import_WithAlias()
    {
        File.WriteAllText(Path.Combine(_tempDir, "math_lib.bad"), @"
export fn add(a, b) { return a + b; }
");

        string source = @"
import math_lib as m;
print(m.add(10, 20));
";
        string output = Run(source);
        Assert.Equal("30", output);
    }

    [Fact]
    public void Import_WithSymbols()
    {
        File.WriteAllText(Path.Combine(_tempDir, "math_lib.bad"), @"
export fn add(a, b) { return a + b; }
export fn sub(a, b) { return a - b; }
");

        string source = @"
import math_lib { add };
print(add(10, 20));
";
        string output = Run(source);
        Assert.Equal("30", output);
    }

    [Fact]
    public void Import_WithSymbolsAndAlias()
    {
        File.WriteAllText(Path.Combine(_tempDir, "math_lib.bad"), @"
export fn add(a, b) { return a + b; }
");

        string source = @"
import math_lib { add } as m;
print(add(1, 2));
print("" "");
print(m.add(3, 4));
";
        string output = Run(source);
        Assert.Equal("3 7", output);
    }

    [Fact]
    public void Import_MultipleWithDifferentAliases()
    {
        File.WriteAllText(Path.Combine(_tempDir, "lib.bad"), @"
export var x = 10;
");

        string source = @"
import lib as a;
import lib as b;
a.x = 20;
print(a.x);
print("" "");
print(b.x);
";
        // Modules in BadLang (interpreter) are cached by path. 
        // So a and b should refer to the same module instance.
        string output = Run(source);
        Assert.Equal("20 20", output);
    }

    [Fact]
    public void Import_DefaultAlias_NonStdlib()
    {
        File.WriteAllText(Path.Combine(_tempDir, "mylib.bad"), @"
export var x = 42;
");
        // IRBuilder.cs: if (alias == null && !pathStr.StartsWith("stdlib.")) alias = pathStr;
        string source = @"
import mylib;
print(mylib.x);
";
        string output = Run(source);
        Assert.Equal("42", output);
    }
    [Fact]
    public void Export_Visibility()
    {
        File.WriteAllText(Path.Combine(_tempDir, "mod.bad"), @"
var hidden = 1;
export var visible = 2;
");

        string source = @"
import mod;
print(mod.visible);
print("" "");
print(mod.hidden == null ? ""hidden"" : ""visible"");
";
        string output = Run(source);
        Assert.Equal("2 hidden", output);
    }
    [Fact]
    public void Import_WithAliasAndSymbols()
    {
        File.WriteAllText(Path.Combine(_tempDir, "math_lib.bad"), @"
export fn add(a, b) { return a + b; }
");

        string source = @"
import math_lib as m { add };
print(add(1, 2));
print("" "");
print(m.add(3, 4));
";
        string output = Run(source);
        Assert.Equal("3 7", output);
    }
    [Fact]
    public void Export_List()
    {
        File.WriteAllText(Path.Combine(_tempDir, "mod.bad"), @"
var hidden = 1;
var visible1 = 2;
var visible2 = 3;

export { visible1, visible2 };
");

        string source = @"
import mod;
print(mod.visible1);
print("" "");
print(mod.visible2);
print("" "");
print(mod.hidden == null ? ""hidden"" : ""visible"");
";
        string output = Run(source);
        Assert.Equal("2 3 hidden", output);
    }
}
