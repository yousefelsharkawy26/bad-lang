using BadLang.Core;
using BadLang.Lexer;

namespace BadLang.Tests;

public class LexerTests
{
    [Fact]
    public void ScanTokens_RecognizesKeywords()
    {
        string source = "var fn if else while for return true false class try catch finally throw this super new num string bool char any void import as interface struct enum switch case default in break continue";
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();

        int i = 0;
        Assert.Equal(TokenType.Var, tokens[i++].Type);
        Assert.Equal(TokenType.Fn, tokens[i++].Type);
        Assert.Equal(TokenType.If, tokens[i++].Type);
        Assert.Equal(TokenType.Else, tokens[i++].Type);
        Assert.Equal(TokenType.While, tokens[i++].Type);
        Assert.Equal(TokenType.For, tokens[i++].Type);
        Assert.Equal(TokenType.Return, tokens[i++].Type);
        Assert.Equal(TokenType.True, tokens[i++].Type);
        Assert.Equal(TokenType.False, tokens[i++].Type);
        Assert.Equal(TokenType.Class, tokens[i++].Type);
        Assert.Equal(TokenType.Try, tokens[i++].Type);
        Assert.Equal(TokenType.Catch, tokens[i++].Type);
        Assert.Equal(TokenType.Finally, tokens[i++].Type);
        Assert.Equal(TokenType.Throw, tokens[i++].Type);
        Assert.Equal(TokenType.This, tokens[i++].Type);
        Assert.Equal(TokenType.Super, tokens[i++].Type);
        Assert.Equal(TokenType.New, tokens[i++].Type);
        Assert.Equal(TokenType.NumType, tokens[i++].Type);
        Assert.Equal(TokenType.StringType, tokens[i++].Type);
        Assert.Equal(TokenType.BoolType, tokens[i++].Type);
        Assert.Equal(TokenType.CharType, tokens[i++].Type);
        Assert.Equal(TokenType.AnyType, tokens[i++].Type);
        Assert.Equal(TokenType.VoidType, tokens[i++].Type);
        Assert.Equal(TokenType.Import, tokens[i++].Type);
        Assert.Equal(TokenType.As, tokens[i++].Type);
        Assert.Equal(TokenType.Interface, tokens[i++].Type);
        Assert.Equal(TokenType.Struct, tokens[i++].Type);
        Assert.Equal(TokenType.Enum, tokens[i++].Type);
        Assert.Equal(TokenType.Switch, tokens[i++].Type);
        Assert.Equal(TokenType.Case, tokens[i++].Type);
        Assert.Equal(TokenType.Default, tokens[i++].Type);
        Assert.Equal(TokenType.In, tokens[i++].Type);
        Assert.Equal(TokenType.Break, tokens[i++].Type);
        Assert.Equal(TokenType.Continue, tokens[i++].Type);
        Assert.Equal(TokenType.EOF, tokens[i].Type);
    }

    [Fact]
    public void ScanTokens_RecognizesNumbers()
    {
        string source = "123 45.67";
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();

        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(123.0, tokens[0].Literal);

        Assert.Equal(TokenType.Number, tokens[1].Type);
        Assert.Equal(45.67, tokens[1].Literal);
    }

    [Fact]
    public void ScanTokens_RecognizesStrings()
    {
        string source = "\"hello world\"";
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();

        Assert.Equal(TokenType.String, tokens[0].Type);
        Assert.Equal("hello world", tokens[0].Literal);
    }

    [Fact]
    public void ScanTokens_RecognizesOperators()
    {
        string source = "+ - * / % == != < > <= >= && || ! = += -= *= /= %= => ?? .. ..= [ ] . : ^";
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();

        int i = 0;
        Assert.Equal(TokenType.Plus, tokens[i++].Type);
        Assert.Equal(TokenType.Minus, tokens[i++].Type);
        Assert.Equal(TokenType.Star, tokens[i++].Type);
        Assert.Equal(TokenType.Slash, tokens[i++].Type);
        Assert.Equal(TokenType.Percent, tokens[i++].Type);
        Assert.Equal(TokenType.EqualEqual, tokens[i++].Type);
        Assert.Equal(TokenType.BangEqual, tokens[i++].Type);
        Assert.Equal(TokenType.Less, tokens[i++].Type);
        Assert.Equal(TokenType.Greater, tokens[i++].Type);
        Assert.Equal(TokenType.LessEqual, tokens[i++].Type);
        Assert.Equal(TokenType.GreaterEqual, tokens[i++].Type);
        Assert.Equal(TokenType.And, tokens[i++].Type);
        Assert.Equal(TokenType.Or, tokens[i++].Type);
        Assert.Equal(TokenType.BangLog, tokens[i++].Type);
        Assert.Equal(TokenType.Equal, tokens[i++].Type);
        Assert.Equal(TokenType.PlusEqual, tokens[i++].Type);
        Assert.Equal(TokenType.MinusEqual, tokens[i++].Type);
        Assert.Equal(TokenType.StarEqual, tokens[i++].Type);
        Assert.Equal(TokenType.SlashEqual, tokens[i++].Type);
        Assert.Equal(TokenType.PercentEqual, tokens[i++].Type);
        Assert.Equal(TokenType.Arrow, tokens[i++].Type);
        Assert.Equal(TokenType.QuestionQuestion, tokens[i++].Type);
        Assert.Equal(TokenType.DotDot, tokens[i++].Type);
        Assert.Equal(TokenType.DotDotEqual, tokens[i++].Type);
        Assert.Equal(TokenType.OpenBracket, tokens[i++].Type);
        Assert.Equal(TokenType.CloseBracket, tokens[i++].Type);
        Assert.Equal(TokenType.Dot, tokens[i++].Type);
        Assert.Equal(TokenType.Colon, tokens[i++].Type);
        Assert.Equal(TokenType.Caret, tokens[i].Type);
    }
}
