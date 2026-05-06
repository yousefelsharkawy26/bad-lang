using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Abstractions;

public interface IRuntimeProvider
{
    void DeclareRuntime(LLVMModuleRef module, LLVMContextRef context);
    LLVMTypeRef GetRuntimeType(string name);
    bool HasRuntimeType(string name);
    
    // Helper for printf which is used very frequently
    LLVMTypeRef PrintfType { get; }
}
