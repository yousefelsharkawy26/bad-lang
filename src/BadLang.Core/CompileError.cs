namespace BadLang.Core;

public class CompileError(string message, Token? token = null) : Exception(message)
{
    public Token? Token { get; } = token;
}
