#nullable enable

namespace PactSharp.Types;

public class PactContinuation
{
    public bool? Executed { get; set; }
    public string PactId { get; set; } = "";
    public int Step { get; set; }
    public int StepCount { get; set; }
    public PactContinuationMetadata Continuation { get; set; } = new PactContinuationMetadata();
    public PactYield? Yield { get; set; }
}
