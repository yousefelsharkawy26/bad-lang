using System;
using System.Text;
using BadLang.Core;
using Spectre.Console;

namespace BadLang.Cli.Diagnostics;

public static class ErrorRenderer
{
    public static void Render(string source, Token token, string message)
    {
        try
        {
            // Find the start and end of the line
            int lineStart = token.Offset;
            while (lineStart > 0 && source[lineStart - 1] != '\n') lineStart--;
            
            int lineEnd = token.Offset;
            while (lineEnd < source.Length && source[lineEnd] != '\n' && source[lineEnd] != '\r') lineEnd++;
            
            string lineText = source.Substring(lineStart, lineEnd - lineStart);
            int column = token.Offset - lineStart;

            // Header
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
            AnsiConsole.MarkupLine($"  [grey]-->[/] line {token.Line}:{column + 1}");
            
            // Code line
            string lineNum = token.Line.ToString();
            string padding = new string(' ', lineNum.Length);
            
            AnsiConsole.MarkupLine($"[blue]{padding} |[/]");
            
            // Highlight the line text. 
            // Note: We escape it first because Highlight might add markup, 
            // but Highlight already calls Markup.Escape internally.
            string highlighted = Highlighter.Highlight(lineText);
            AnsiConsole.MarkupLine($"[blue]{lineNum} |[/] {highlighted}");
            
            // Carat line
            var caratLine = new StringBuilder();
            caratLine.Append(new string(' ', column));
            caratLine.Append("[red]^[/]");
            if (token.Lexeme.Length > 1)
            {
                caratLine.Append("[red]" + new string('~', token.Lexeme.Length - 1) + "[/]");
            }
            
            AnsiConsole.MarkupLine($"[blue]{padding} |[/] {caratLine}");
            AnsiConsole.WriteLine();
        }
        catch
        {
            // Fallback to simple reporting if rendering fails
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)} at [yellow]{token.Lexeme}[/] (line {token.Line})");
        }
    }
}
