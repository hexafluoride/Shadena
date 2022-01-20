namespace PactSharp.Types;

public class PactContPayload
{
    public string PactId { get; set; }
    public bool Rollback { get; set; }
    public int Step { get; set; }
    public string Proof { get; set; }
    public object Data { get; set; }
}