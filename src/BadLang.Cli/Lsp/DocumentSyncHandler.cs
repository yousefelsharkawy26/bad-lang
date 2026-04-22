using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace BadLang.Cli.Lsp;

public class DocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly ILanguageServerFacade _router;
    private readonly ConcurrentDictionary<string, string> _documents = new();

    public DocumentSyncHandler(ILanguageServerFacade router)
    {
        _router = router;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "badlang");
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var text = request.TextDocument.Text;
        _documents[uri] = text;
        
        Analyze(uri, text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var text = request.ContentChanges.First().Text;
        _documents[uri] = text;
        
        Analyze(uri, text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        _documents.TryRemove(uri, out _);
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.bad"),
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions { IncludeText = true }
        };
    }

    private void Analyze(string uri, string text)
    {
        var diagnostics = new List<Diagnostic>();
        
        try
        {
            var lexer = new BadLang.Lexer.Lexer(text);
            var tokens = lexer.ScanTokens();
            var parser = new BadLang.Parser.Parser(tokens);
            var statements = parser.Parse();

            if (parser.Errors.Count > 0)
            {
                foreach (var err in parser.Errors)
                {
                    if (err.Token == null) continue;
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new Position(err.Token.Line - 1, err.Token.Column),
                            new Position(err.Token.Line - 1, err.Token.Column + (err.Token.Lexeme?.Length ?? 1))
                        ),
                        Message = err.Message,
                        Source = "badlang"
                    });
                }
            }
            else
            {
                var typeChecker = new BadLang.Semantic.TypeChecker();
                // We'll use a relative path logic or default
                typeChecker.SetBasePath(".");
                typeChecker.Check(statements);

                foreach (var err in typeChecker.Errors)
                {
                    if (err.Token == null) continue;
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new Position(err.Token.Line - 1, err.Token.Column),
                            new Position(err.Token.Line - 1, err.Token.Column + (err.Token.Lexeme?.Length ?? 1))
                        ),
                        Message = err.Message,
                        Source = "badlang"
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(0, 0),
                    new Position(0, 1)
                ),
                Message = "Compiler exception: " + ex.Message,
                Source = "badlang"
            });
        }

        _router.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = DocumentUri.Parse(uri),
            Diagnostics = new Container<Diagnostic>(diagnostics)
        });
    }
}
