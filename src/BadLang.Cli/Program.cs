using BadLang.Cli;
using BadLang.Cli.Commands;
using BadLang.Backend.Interpreter;
using Spectre.Console;

var argsParsed = CliArgs.Parse(args);
var interpreter = new Interpreter();

if (!argsParsed.LspMode)
    CliOutput.ShowBanner();

if (argsParsed.LspMode)
{
    await BadLang.Cli.Lsp.BadLangLanguageServer.StartAsync();
    return;
}

if (argsParsed.ShowHelp)
{
    CliOutput.ShowHelp();
    return;
}

switch (argsParsed)
{
    case { Command: "build" or "llvm", InputPath: { } path }:
        new BuildCommand().Execute(path, argsParsed.OutputPath, argsParsed.OptLevel, argsParsed.Verbose, argsParsed.LspMode);
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