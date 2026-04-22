using BadLang.Cli;
using BadLang.Cli.Commands;
using BadLang.Backend.Interpreter;
using Spectre.Console;

var args_parsed = CliArgs.Parse(args);
var interpreter = new Interpreter();

if (!args_parsed.LspMode)
    CliOutput.ShowBanner();

if (args_parsed.LspMode)
{
    await BadLang.Cli.Lsp.BadLangLanguageServer.StartAsync();
    return;
}

if (args_parsed.ShowHelp)
{
    CliOutput.ShowHelp();
    return;
}

switch (args_parsed)
{
    case { Command: "build" or "llvm", InputPath: { } path }:
        new BuildCommand().Execute(path, args_parsed.OutputPath, args_parsed.OptLevel, args_parsed.Verbose, args_parsed.LspMode);
        break;

    case { Command: "check", InputPath: { } path }:
        new CheckCommand().Execute(path);
        break;

    case { Command: null, InputPath: { } path }:
        new RunCommand(interpreter).Execute(path);
        break;

    case { Command: null, InputPath: null }:
        new Repl(interpreter).Run();
        break;

    default:
        AnsiConsole.MarkupLine("[red]Error:[/] Invalid arguments.");
        CliOutput.ShowHelp();
        System.Environment.Exit(64);
        break;
}