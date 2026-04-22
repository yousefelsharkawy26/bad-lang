using System;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BadLang.Cli.Lsp;

public class BadLangLanguageServer
{
    public static async Task StartAsync()
    {
        var server = await LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithLoggerFactory(new LoggerFactory())
                .AddDefaultLoggingProvider()
                .WithServices(services => { })
                .WithHandler<DocumentSyncHandler>()
            );

        await server.WaitForExit;
    }
}
