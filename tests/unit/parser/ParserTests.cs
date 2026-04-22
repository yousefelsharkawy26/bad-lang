using BadLang.Core;
using BadLang.Lexer;
using BadLang.Parser;

namespace BadLang.Tests;

public class ParserTests
{
    private List<Stmt> Parse(string source)
    {
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new BadLang.Parser.Parser(tokens);
        return parser.Parse();
    }

    [Fact]
    public void Parse_VarDeclaration()
    {
        string source = "var x = 10;";
        var statements = Parse(source);

        Assert.Single(statements);
        Assert.IsType<Stmt.Var>(statements[0]);
        var varStmt = (Stmt.Var)statements[0];
        Assert.Equal("x", varStmt.Name.Lexeme);
    }

    [Fact]
    public void Parse_FunctionDeclaration()
    {
        string source = "fn add(a, b) { return a + b; }";
        var statements = Parse(source);

        Assert.Single(statements);
        Assert.IsType<Stmt.Function>(statements[0]);
        var fnStmt = (Stmt.Function)statements[0];
        Assert.Equal("add", fnStmt.Name.Lexeme);
        Assert.Equal(2, fnStmt.Params.Count);
    }

    [Fact]
    public void Parse_ClassDeclaration()
    {
        string source = "class MyClass : Base { fn method() {} }";
        var statements = Parse(source);

        Assert.Single(statements);
        Assert.IsType<Stmt.Class>(statements[0]);
        var classStmt = (Stmt.Class)statements[0];
        Assert.Equal("MyClass", classStmt.Name.Lexeme);
        Assert.Contains("Base", classStmt.Parents.Select(p => p.Lexeme));
    }

    [Fact]
    public void Parse_TryCatch()
    {
        string source = "try { throw 1; } catch(e) { print(e); } finally { cleanup(); }";
        var statements = Parse(source);

        Assert.Single(statements);
        Assert.IsType<Stmt.TryCatch>(statements[0]);
        var tryStmt = (Stmt.TryCatch)statements[0];
        Assert.Single(tryStmt.CatchClauses);
        Assert.NotNull(tryStmt.FinallyBlock);
    }
}
