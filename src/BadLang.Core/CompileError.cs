namespace BadLang.Core;

public class CompileError : Exception
{
    public Token? Token { get; }

    public CompileError(string message, Token? token = null) : base(message)
    {
        Token = token;
    }
}
