using LLVMSharp.Interop;
using BadLang.Backend.LLVM.Abstractions;

namespace BadLang.Backend.LLVM.Infrastructure;

public class LlvmInfrastructure : ICompilerInfrastructure
{
    public LLVMContextRef Context { get; }
    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; }

    public LLVMTypeRef VoidPtrType => LLVMTypeRef.CreatePointer(Context.Int8Type, 0);
    public LLVMTypeRef StringPtrType => LLVMTypeRef.CreatePointer(Context.Int8Type, 0);
    public LLVMTypeRef ExceptionPtrType => LLVMTypeRef.CreatePointer(Context.Int8Type, 0);

    public LlvmInfrastructure(LLVMContextRef context, LLVMModuleRef module, LLVMBuilderRef builder)
    {
        Context = context;
        Module = module;
        Builder = builder;
    }

    public void Dispose()
    {
        Builder.Dispose();
        Module.Dispose();
        Context.Dispose();
    }
}
