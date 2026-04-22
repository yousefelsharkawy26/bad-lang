using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BadLang.Core;
using BadLang.Lexer;

using Spectre.Console;

namespace BadLang.Cli.Diagnostics;

public static class Highlighter
{
    public static string Highlight(string source)
    {
        try
        {
            var lexer = new BadLang.Lexer.Lexer(source);
            var tokens = lexer.ScanTokens();
            var sb = new StringBuilder();
            int lastPos = 0;

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.EOF) break;

                // Add whitespace/gap between tokens
                if (token.Offset > lastPos)
                {
                    sb.Append(Markup.Escape(source.Substring(lastPos, token.Offset - lastPos)));
                }

                string color = GetTokenColor(token.Type);
                if (color != null)
                {
                    sb.Append($"[{color}]{Markup.Escape(token.Lexeme)}[/]");
                }
                else
                {
                    sb.Append(Markup.Escape(token.Lexeme));
                }
                
                lastPos = token.Offset + token.Lexeme.Length;
            }

            // Add any trailing whitespace
            if (lastPos < source.Length)
            {
                sb.Append(Markup.Escape(source.Substring(lastPos)));
            }

            return sb.ToString();
        }
        catch
        {
            return Markup.Escape(source);
        }
    }

    private static string GetTokenColor(TokenType type)
    {
        return type switch
        {
            TokenType.Var or TokenType.Fn or TokenType.If or TokenType.Else or 
            TokenType.While or TokenType.For or TokenType.Return or 
            TokenType.Try or TokenType.Catch or TokenType.Finally or 
            TokenType.Throw or TokenType.Class or TokenType.Super or 
            TokenType.This or TokenType.New or TokenType.Break or
            TokenType.Continue or TokenType.In or TokenType.As or
            TokenType.Switch or TokenType.Case or TokenType.Default or
            TokenType.Interface or TokenType.Struct or TokenType.Enum => "blue",

            TokenType.String or TokenType.InterpolatedString => "orange1",
            TokenType.Number => "green",
            TokenType.True or TokenType.False or TokenType.Null => "magenta",
            
            TokenType.Identifier => "yellow",
            
            TokenType.Comment => "grey",
            
            TokenType.OpenParen or TokenType.CloseParen or 
            TokenType.OpenBrace or TokenType.CloseBrace or 
            TokenType.OpenBracket or TokenType.CloseBracket => "white",
            
            _ => null!
        };
    }
}
