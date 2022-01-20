namespace PactSharp.Types;

public class AccountIdentifier
{
    public string Name { get; set; }

    public AccountIdentifier(string name)
    {
        Name = name;
    }
}