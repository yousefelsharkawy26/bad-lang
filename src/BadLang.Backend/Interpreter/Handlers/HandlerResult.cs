namespace BadLang.Backend.Interpreter.Handlers;

public class HandlerResult
{
    public bool AdvanceIp { get; private init; } = true;
    
    public static readonly HandlerResult Continue = new() { AdvanceIp = true };
    public static readonly HandlerResult Jump = new() { AdvanceIp = false };
    
    [System.Obsolete("Use Continue instead")]
    public static readonly HandlerResult Default = Continue;
    [System.Obsolete("Use Jump instead")]
    public static readonly HandlerResult NoAdvance = Jump;
}
