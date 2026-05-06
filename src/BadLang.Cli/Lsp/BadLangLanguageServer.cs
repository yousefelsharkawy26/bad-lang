using OmniSharp.Extensions.LanguageServer.Server;
using Microsoft.Extensions.Logging;

namespace BadLang.Cli.Lsp;

public static class BadLangLanguageServer
{
    public static async Task StartAsync()
    {
        var server = await LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithLoggerFactory(new LoggerFactory())
                .AddDefaultLoggingProvider()
                .WithServices(_ => { })
                .WithHandler<DocumentSyncHandler>()
            );
        
        await server.WaitForExit;
    }
}
