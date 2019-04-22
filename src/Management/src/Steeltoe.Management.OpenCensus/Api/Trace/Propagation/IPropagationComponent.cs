namespace Steeltoe.Management.Census.Trace.Propagation
{
    public interface IPropagationComponent
    {
        IBinaryFormat BinaryFormat { get; }
        ITextFormat TextFormat { get; }
    }
}