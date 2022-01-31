namespace PactSharp;

public class BodiedPactExpression : PactExpression
{
    public BodyPactExpression Body { get; set; }
    internal BodiedPactExpression(PactExpression root, BodyPactExpression body) : base(root.Backing)
    {
        Body = body;
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        yield return Body;
    }

    internal BodiedPactExpression(PactExpression root) : base(root.Backing)
    {
    }
}