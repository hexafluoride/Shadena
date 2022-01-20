namespace PactSharp.Types;

public class PactPayload
{
    public PactExecPayload Exec { get; set; }
    public PactContPayload Cont { get; set; }
}