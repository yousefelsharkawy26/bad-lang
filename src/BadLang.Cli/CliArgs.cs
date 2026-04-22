namespace BadLang.Cli;

public sealed record CliArgs(
    string? Command,
    string? InputPath,
    string? OutputPath,
    string OptLevel,
    bool Verbose,
    bool LspMode,
    bool ShowHelp)
{
    public static CliArgs Parse(string[] args)
    {
        string? command = null;
        string? inputPath = null;
        string? outputPath = null;
        string optLevel = "0";
        bool verbose = false;
        bool lspMode = false;

        bool showHelp = args.Length > 0 &&
            args[0] is "-h" or "--help" or "help";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "build" or "llvm" or "check":
                    command = args[i];
                    break;

                case "-o" when i + 1 < args.Length:
                    outputPath = args[++i];
                    break;

                case "-v" or "--verbose":
                    verbose = true;
                    break;

                case "--lsp":
                    lspMode = true;
                    break;

                case string a when a.StartsWith("-O") && a.Length == 3 && "0123".Contains(a[2]):
                    optLevel = a[2].ToString();
                    break;

                default:
                    inputPath ??= args[i];
                    break;
            }
        }

        return new CliArgs(command, inputPath, outputPath, optLevel, verbose, lspMode, showHelp);
    }
}