using BadLang.Semantic;
using BadLang.Backend.LLVM;
using BadLang.Cli.Diagnostics;
using Spectre.Console;

namespace BadLang.Cli.Commands
{
    public class BuildCommand
    {
        public void Execute(string path, string? outputPath, string optLevel, bool verbose, bool lspMode)
        {
            if (!File.Exists(path))
            {
                if (lspMode) Console.WriteLine($"{{\"error\": \"File not found: {path}\"}}");
                else AnsiConsole.MarkupLine($"[red]Error:[/] File not found: [yellow]{path}[/]");
                return;
            }

            string source = File.ReadAllText(path);
            bool success = true;

            void ReportError(string message, Core.Token? token = null)
            {
                success = false;
                if (lspMode)
                {
                    var diag = new { type = "error", message, line = token?.Line ?? 0, lexeme = token?.Lexeme ?? "" };
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(diag));
                }
                else
                {
                    if (token != null) ErrorRenderer.Render(source, token, message);
                    else AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
                }
            }

            if (!lspMode) AnsiConsole.Status().Start("Building native executable [yellow]" + path + "[/]...", _ => { RunBuild(); });
            else RunBuild();

            void RunBuild()
            {
                try
                {
                    var lexer = new BadLang.Lexer.Lexer(source);
                    var tokens = lexer.ScanTokens();
                    var parser = new BadLang.Parser.Parser(tokens);
                    var statements = parser.Parse();

                    foreach (var err in parser.Errors) ReportError(err.Message, err.Token);

                    var typeChecker = new TypeChecker();
                    string basePath = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
                    typeChecker.SetBasePath(basePath);
                    typeChecker.Check(statements);

                    foreach (var err in typeChecker.Errors) ReportError(err.Message, err.Token);

                    if (!success) return;

                    string moduleName = Path.GetFileNameWithoutExtension(path);
                    var compiler = new LlvmBackend(moduleName);
                    compiler.SetBasePath(basePath);
                    compiler.Compile(statements);

                    string llPath = Path.ChangeExtension(path, ".ll");
                    string finalOutputPath = outputPath ?? Path.Combine(Path.GetDirectoryName(path) ?? "", moduleName);

                    compiler.WriteAssemblyToFile(llPath);
                    PatchIr(llPath);

                    string runtimePath = ResolveRuntimePath();
                    string clangArgs = $"{llPath} {runtimePath} -o {finalOutputPath} -lm -O{optLevel}";
                    
                    if (verbose) AnsiConsole.MarkupLine($"[grey]Exec:[/] clang {clangArgs}");

                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "clang",
                        Arguments = clangArgs,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    });

                    process?.WaitForExit();

                    if (process?.ExitCode == 0)
                    {
                        if (!lspMode) AnsiConsole.MarkupLine($"[green]Success:[/] Native executable created at [yellow]{finalOutputPath}[/]");
                        if (File.Exists(llPath)) File.Delete(llPath);
                    }
                    else
                    {
                        string error = process?.StandardError.ReadToEnd() ?? "Unknown error";
                        if (lspMode) Console.WriteLine($"{{\"error\": \"Clang error: {error.Replace("\"", "\\\"")}\"}}");
                        else { AnsiConsole.Write(new Markup("[red]Clang Error:[/] ")); AnsiConsole.WriteLine(error); }
                    }
                }
                catch (Exception ex)
                {
                    if (lspMode) Console.WriteLine($"{{\"error\": \"{ex.Message.Replace("\"", "\\\"")}\"}}");
                    else { AnsiConsole.Write(new Markup("[red]Internal Build Error:[/] ")); AnsiConsole.WriteLine(ex.Message); }
                }
            }
        }

        private void PatchIr(string path)
        {
            if (File.Exists(path))
            {
                string llContent = File.ReadAllText(path);
                llContent = llContent.Replace("inbounds nuw ", "inbounds ");
                llContent = System.Text.RegularExpressions.Regex.Replace(llContent, @"initializes\s*\(\s*\([^)]+\)\s*\)\s*", "");
                llContent = System.Text.RegularExpressions.Regex.Replace(llContent, @"initializes\s*\([^)]+\)\s*", "");
                File.WriteAllText(path, llContent);
            }
        }

        private string ResolveRuntimePath()
        {
            // Logic moved from Program.cs, updated for new location
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidate = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "..", "src", "BadLang.Runtime", "runtime.c"));
            if (File.Exists(candidate)) return candidate;
            
            return "src/BadLang.Runtime/runtime.c";
        }
    }
}
