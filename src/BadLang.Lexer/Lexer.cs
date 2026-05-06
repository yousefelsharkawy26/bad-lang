using System.Text;
using BadLang.Core;

namespace BadLang.Lexer;

public class Lexer(string source)
{
    private readonly List<Token> _tokens = new();
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "var", TokenType.Var },
        { "let", TokenType.Var },
        { "const", TokenType.Const },
        { "export", TokenType.Export },
        { "if", TokenType.If },
        { "else", TokenType.Else },
        { "switch", TokenType.Switch },
        { "case", TokenType.Case },
        { "default", TokenType.Default },
        { "while", TokenType.While },
        { "do", TokenType.Do },
        { "for", TokenType.For },
        { "in", TokenType.In },
        { "break", TokenType.Break },
        { "continue", TokenType.Continue },
        { "return", TokenType.Return },
        { "fn", TokenType.Fn },
        { "fun", TokenType.Fn },
        { "func", TokenType.Fn },
        { "function", TokenType.Fn },
        { "class", TokenType.Class },
        { "interface", TokenType.Interface },
        { "struct", TokenType.Struct },
        { "enum", TokenType.Enum },
        { "try", TokenType.Try },
        { "catch", TokenType.Catch },
        { "finally", TokenType.Finally },
        { "throw", TokenType.Throw },
        { "new", TokenType.New },
        { "import", TokenType.Import },
        { "as", TokenType.As },
        { "bool", TokenType.BoolType },
        { "string", TokenType.StringType },
        { "char", TokenType.CharType },
        { "num", TokenType.NumType },
        { "number", TokenType.NumType },
        { "int", TokenType.NumType },
        { "float", TokenType.NumType },
        { "double", TokenType.NumType },
        { "any", TokenType.AnyType },
        { "void", TokenType.VoidType },
        { "true", TokenType.True },
        { "false", TokenType.False },
        { "null", TokenType.Null },
        { "typeof", TokenType.TypeOf },
        { "nameof", TokenType.NameOf },
        { "toString", TokenType.ToString },
        { "toNumber", TokenType.ToNumber },
        { "isNull", TokenType.IsNull },
        { "assert", TokenType.Assert },
        { "panic", TokenType.Panic },
        { "this", TokenType.This },
        { "super", TokenType.Super }
    };

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", null, _line, _column, _current));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(': AddToken(TokenType.OpenParen); break;
            case ')': AddToken(TokenType.CloseParen); break;
            case '{': AddToken(TokenType.OpenBrace); break;
            case '}': AddToken(TokenType.CloseBrace); break;
            case '[': AddToken(TokenType.OpenBracket); break;
            case ']': AddToken(TokenType.CloseBracket); break;
            case ',': AddToken(TokenType.Comma); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case ':': AddToken(TokenType.Colon); break;
            case '^': AddToken(TokenType.Caret); break;

            case '.':
                if (Match('.'))
                {
                    AddToken(Match('=') ? TokenType.DotDotEqual : TokenType.DotDot);
                }
                else
                {
                    AddToken(TokenType.Dot);
                }
                break;

            case '-':
                if (Match('-')) AddToken(TokenType.MinusMinus);
                else if (Match('=')) AddToken(TokenType.MinusEqual);
                else AddToken(TokenType.Minus);
                break;

            case '+':
                if (Match('+')) AddToken(TokenType.PlusPlus);
                else if (Match('=')) AddToken(TokenType.PlusEqual);
                else AddToken(TokenType.Plus);
                break;

            case '*':
                AddToken(Match('=') ? TokenType.StarEqual : TokenType.Star);
                break;

            case '%':
                AddToken(Match('=') ? TokenType.PercentEqual : TokenType.Percent);
                break;

            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.BangLog);
                break;

            case '=':
                if (Match('=')) AddToken(TokenType.EqualEqual);
                else if (Match('>')) AddToken(TokenType.Arrow);
                else AddToken(TokenType.Equal);
                break;

            case '<':
                if (Match('<')) AddToken(TokenType.LessLess);
                else if (Match('=')) AddToken(TokenType.LessEqual);
                else AddToken(TokenType.Less);
                break;

            case '>':
                if (Match('>')) AddToken(TokenType.GreaterGreater);
                else if (Match('=')) AddToken(TokenType.GreaterEqual);
                else AddToken(TokenType.Greater);
                break;

            case '/':
                if (Match('/'))
                {
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                    AddToken(TokenType.Comment);
                }
                else if (Match('='))
                {
                    AddToken(TokenType.SlashEqual);
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;

            case '?':
                AddToken(Match('?') ? TokenType.QuestionQuestion : TokenType.Question);
                break;

            case '&':
                AddToken(Match('&') ? TokenType.And : TokenType.Ampersand);
                break;

            case '|':
                AddToken(Match('|') ? TokenType.Or : TokenType.Pipe);
                break;

            case '$':
                if (Match('"')) ScanInterpolatedString();
                else AddToken(TokenType.Unknown);
                break;

            case ' ':
            case '\r':
            case '\t':
                break;

            case '\n':
                _line++;
                _column = 1;
                break;

            case '"': ScanString(); break;

            default:
                if (char.IsDigit(c))
                {
                    ScanNumber();
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    ScanIdentifier();
                }
                else
                {
                    AddToken(TokenType.Unknown);
                }
                break;
        }
    }

    private void ScanString()
    {
        var sb = new StringBuilder();
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 0;
            }
            
            if (Peek() == '\\')
            {
                Advance(); // Consume \
                switch (Peek())
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case '$': sb.Append('$'); break;
                    default:
                        sb.Append('\\');
                        sb.Append(Peek());
                        break;
                }
            }
            else
            {
                sb.Append(Peek());
            }
            Advance();
        }

        if (IsAtEnd())
        {
            AddToken(TokenType.Unknown, "Unterminated string.");
            return;
        }

        Advance(); // Consume closing quote
        AddToken(TokenType.String, sb.ToString());
    }

    private void ScanNumber()
    {
        while (char.IsDigit(Peek())) Advance();

        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            Advance();
            while (char.IsDigit(Peek())) Advance();
        }

        string text = source.Substring(_start, _current - _start);
        AddToken(TokenType.Number, double.Parse(text));
    }

    private void ScanIdentifier()
    {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();

        string text = source.Substring(_start, _current - _start);
        TokenType type = Keywords.GetValueOrDefault(text, TokenType.Identifier);
        AddToken(type);
    }

    private bool IsAtEnd() => _current >= source.Length;

    private char Advance()
    {
        _column++;
        return source[_current++];
    }

    private void AddToken(TokenType type) => AddToken(type, null);

    private void AddToken(TokenType type, object? literal)
    {
        string text = source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line, _column, _start));
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (source[_current] != expected) return false;

        _current++;
        _column++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : source[_current];

    private char PeekNext()
    {
        if (_current + 1 >= source.Length) return '\0';
        return source[_current + 1];
    }
    private void ScanInterpolatedString()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd())
        {
            AddToken(TokenType.Unknown, "Unterminated interpolated string.");
            return;
        }

        Advance();
        string value = source.Substring(_start + 2, _current - _start - 3);
        AddToken(TokenType.InterpolatedString, value);
    }
}
