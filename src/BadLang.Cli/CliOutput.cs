using Spectre.Console;

namespace BadLang.Cli;

public static class CliOutput
{
    public static void ShowBanner()
    {
        AnsiConsole.Write(new FigletText("BadLang").Color(Color.Red));
        AnsiConsole.MarkupLine("[bold grey]v1.1.0-llvm[/] - [red]The Fast & Flexible Language[/]");
        AnsiConsole.WriteLine();
    }

    public static void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]BadLang Compiler & Interpreter[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Usage:[/] badlang [green]<command>[/] [blue]<arguments>[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Commands:[/]");
        AnsiConsole.MarkupLine("  [green](none)[/]        Start the interactive REPL.");
        AnsiConsole.MarkupLine("  [green]<file>[/]        Run the specified script using the interpreter.");
        AnsiConsole.MarkupLine("  [green]check <file>[/]  Check the script for syntax and semantic errors without running it.");
        AnsiConsole.MarkupLine("  [green]build <file>[/]  Compile the script to a native executable.");
        AnsiConsole.MarkupLine("  [green]llvm <file>[/]   Compile the script to LLVM bitcode.");
        AnsiConsole.WriteLine();
    }
}