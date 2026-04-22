using BadLang.Backend.Interpreter.Runtime;
namespace BadLang.Backend.Interpreter.Handlers;

public class HandlerResult
{
    public bool AdvanceIP { get; set; } = true;
    
    public static readonly HandlerResult Continue = new() { AdvanceIP = true };
    public static readonly HandlerResult Jump = new() { AdvanceIP = false };
    
    [System.Obsolete("Use Continue instead")]
    public static readonly HandlerResult Default = Continue;
    [System.Obsolete("Use Jump instead")]
    public static readonly HandlerResult NoAdvance = Jump;
}
