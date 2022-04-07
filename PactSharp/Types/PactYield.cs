#nullable enable

namespace PactSharp.Types;

public class PactYield
{
    public Dictionary<string, object> Data { get; set; } = new ();
    public PactProvenance? Provenance { get; set; }
}
