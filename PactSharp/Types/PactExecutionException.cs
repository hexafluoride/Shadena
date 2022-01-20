namespace PactSharp.Types;

public class PactExecutionException : Exception
{
    public override string Message { get; }

    public PactExecutionException(PactCommandResponse resp)
    {
        if (resp == null)
        {
            Message = "Null response received";
        }
        else if (resp.Result.Status != "success")
        {
            Message =
                $"Execution status is \"{resp.Result.Status}\" with remote message \"{resp.Result.Error.Message}\" and \"{resp.Result.Error.Info}\", type {resp.Result.Error.Type}";
        }
        else
        {
            Message = "Unknown Pact execution error";
        }
    }
}