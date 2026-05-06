using LLVMSharp.Interop;
using BadLang.Backend.LLVM.Abstractions;

namespace BadLang.Backend.LLVM.Infrastructure;

public class LlvmInfrastructure(LLVMContextRef context, LLVMModuleRef module, LLVMBuilderRef builder)
    : ICompilerInfrastructure
{
    public LLVMContextRef Context { get; } = context;
    public LLVMModuleRef Module { get; } = module;
    public LLVMBuilderRef Builder { get; } = builder;

    public LLVMTypeRef VoidPtrType => LLVMTypeRef.CreatePointer(Context.Int8Type, 0);
    public LLVMTypeRef StringPtrType => LLVMTypeRef.CreatePointer(Context.Int8Type, 0);
    public LLVMTypeRef ExceptionPtrType => LLVMTypeRef.CreatePointer(Context.Int8Type, 0);

    public void Dispose()
    {
        Builder.Dispose();
        Module.Dispose();
        Context.Dispose();
    }
}
