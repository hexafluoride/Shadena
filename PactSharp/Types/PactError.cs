namespace PactSharp.Types;

public class PactError
{
    public string[] CallStack { get; set; }
    public string Info { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
}