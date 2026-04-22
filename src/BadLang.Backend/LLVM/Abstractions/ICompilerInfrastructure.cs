using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Abstractions;

public interface ICompilerInfrastructure : IDisposable
{
    LLVMContextRef Context { get; }
    LLVMModuleRef Module { get; }
    LLVMBuilderRef Builder { get; }

    // Common types
    LLVMTypeRef VoidPtrType { get; }
    LLVMTypeRef StringPtrType { get; }
    LLVMTypeRef ExceptionPtrType { get; }
}
