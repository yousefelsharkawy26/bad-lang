using System.Text;
using BadLang.Semantic;
using BadLang.Backend.Interpreter;
using BadLang.Backend.Interpreter.Runtime;
using BadLang.Cli.Diagnostics;
using Spectre.Console;
using BadLang.IR;

namespace BadLang.Cli;

public class Repl(Interpreter interpreter)
{
    private readonly Interpreter _interpreter = interpreter;
    private readonly List<string> _history = new();
    private readonly TypeChecker _typeChecker = new();
    private int _historyIndex = -1;

    public void Run()
    {
        AnsiConsole.Write(
            new FigletText("Bad-Lang")
                .Color(Color.DeepSkyBlue1));

        AnsiConsole.MarkupLine("[bold blue]Interactive REPL (v1.1.0)[/]");
        AnsiConsole.MarkupLine("Type [yellow]exit[/] to quit, [yellow]clear[/] to clear screen.");
        AnsiConsole.WriteLine();

        while (true)
        {
            string line = ReadLine();
            if (line == "exit") break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line == "clear")
            {
                Console.Clear();
                continue;
            }

            _history.Add(line);
            _historyIndex = _history.Count;

            Execute(line);
        }
    }

    private string ReadLine()
    {
        StringBuilder input = new();
        int cursorPosition = 0;

        AnsiConsole.Markup("\n[gray]badlang[/] > ");
        AnsiConsole.Write("");
        while (true)
        {
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input.ToString();
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (cursorPosition > 0)
                {
                    input.Remove(cursorPosition - 1, 1);
                    cursorPosition--;
                    RedrawLine(input.ToString(), cursorPosition);
                }
            }
            else if (keyInfo.Key == ConsoleKey.Delete)
            {
                if (cursorPosition < input.Length)
                {
                    input.Remove(cursorPosition, 1);
                    RedrawLine(input.ToString(), cursorPosition);
                }
            }
            else if (keyInfo.Key == ConsoleKey.LeftArrow)
            {
                if (cursorPosition > 0)
                {
                    cursorPosition--;
                    MoveCursor(cursorPosition);
                }
            }
            else if (keyInfo.Key == ConsoleKey.RightArrow)
            {
                if (cursorPosition < input.Length)
                {
                    cursorPosition++;
                    MoveCursor(cursorPosition);
                }
            }
            else if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                if (_history.Count > 0 && _historyIndex > 0)
                {
                    _historyIndex--;
                    input.Clear();
                    input.Append(_history[_historyIndex]);
                    cursorPosition = input.Length;
                    RedrawLine(input.ToString(), cursorPosition);
                }
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                if (_historyIndex < _history.Count - 1)
                {
                    _historyIndex++;
                    input.Clear();
                    input.Append(_history[_historyIndex]);
                    cursorPosition = input.Length;
                    RedrawLine(input.ToString(), cursorPosition);
                }
                else if (_historyIndex == _history.Count - 1)
                {
                    _historyIndex++;
                    input.Clear();
                    cursorPosition = 0;
                    RedrawLine("", 0);
                }
            }
            else if (keyInfo.KeyChar != '\0' && !char.IsControl(keyInfo.KeyChar))
            {
                input.Insert(cursorPosition, keyInfo.KeyChar);
                cursorPosition++;
                RedrawLine(input.ToString(), cursorPosition);
            }
        }
    }

    private void RedrawLine(string input, int cursorPosition)
    {
        // Clear current line
        int top = Console.CursorTop;
        
        Console.SetCursorPosition(9, top); // "badlang> " is 9 chars
        Console.Write(new string(' ', Console.WindowWidth - 10));
        Console.SetCursorPosition(9, top);

        // Highlight and write
        string highlighted = Highlighter.Highlight(input);
        AnsiConsole.Markup(highlighted);

        // Reset cursor
        Console.SetCursorPosition(9 + cursorPosition, top);
    }

    private void MoveCursor(int position)
    {
        Console.SetCursorPosition(9 + position, Console.CursorTop);
    }

    private void Execute(string source)
    {
        try 
        {
            var lexer = new BadLang.Lexer.Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new BadLang.Parser.Parser(tokens);
            var statements = parser.Parse();

            if (parser.Errors.Count > 0)
            {
                foreach (var err in parser.Errors)
                {
                    ErrorRenderer.Render(source, err.Token, err.Message);
                }
                return;
            }

            if (statements.Count == 0) return;

            // Type check with persistent checker — show warnings but don't block execution
            _typeChecker.Check(statements);

            if (_typeChecker.Errors.Count > 0)
            {
                foreach (var err in _typeChecker.Errors)
                {
                    if (err.Token != null) ErrorRenderer.Render(source, err.Token, err.Message);
                    else AnsiConsole.MarkupLine($"[yellow]Warning:[/] {err.Message}");
                }
                _typeChecker.Errors.Clear();
                // Don't return — let the interpreter run so variables get defined
            }

            var irBuilder = new IRBuilder();
            var irNodes = irBuilder.Build(statements);

            _interpreter.Interpret(irNodes);
        }
        catch (RuntimeException ex)
        {
            ErrorRenderer.Render(source, ex.Token, ex.Message);
        }
        catch (Exception ex)
        {
             AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }
}

