using System;
using System.Collections.Generic;
using System.IO;
using BadLang.Lexer;
using BadLang.Parser;
using BadLang.Semantic;
using BadLang.Backend.Interpreter;
using BadLang.Backend.LLVM;
using BadLang.Cli.Diagnostics;
using Spectre.Console;
using BadLang.IR;

namespace BadLang.Cli.Commands
{
    public class RunCommand
    {
        private readonly Interpreter _interpreter;

        public RunCommand(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public void Execute(string path)
        {
            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File not found: [yellow]{path}[/]");
                return;
            }

            _interpreter.SetBasePath(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            string source = File.ReadAllText(path);
            
            try
            {
                var lexer = new BadLang.Lexer.Lexer(source);
                var tokens = lexer.ScanTokens();
                var parser = new BadLang.Parser.Parser(tokens);
                var statements = parser.Parse();

                if (parser.Errors.Count > 0)
                {
                    foreach (var err in parser.Errors) ErrorRenderer.Render(source, err.Token, err.Message);
                    return;
                }

                var typeChecker = new TypeChecker();
                typeChecker.SetBasePath(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
                typeChecker.Check(statements);

                if (typeChecker.Errors.Count > 0)
                {
                    foreach (var err in typeChecker.Errors)
                    {
                        if (err.Token != null) ErrorRenderer.Render(source, err.Token, err.Message);
                        else AnsiConsole.MarkupLine($"[red]Type Error:[/] {err.Message}");
                    }
                    return;
                }

                var irBuilder = new IRBuilder();
                var irNodes = irBuilder.Build(statements);

                _interpreter.Interpret(irNodes);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }
    }
}
