namespace PactSharp;

public class FunctionPactExpression : BodiedPactExpression
{
    public TypedIdentifierPactExpression MethodIdentifier { get; set; }
    public ArgumentListPactExpression Arguments { get; set; }
    public PactExpression Model { get; set; }
    public PactExpression Documentation { get; set; }
    
    internal FunctionPactExpression(PactExpression root) : base(root)
    {
        Backing = root.Backing;
        Type = ExpressionType.Function;
        Parent = root.Parent;

        var rest = root.Backing.Slice(1, root.Backing.Length - 2);
        rest = rest.Slice("defun".Length);
        
        var nextRest = rest;
        PactExpression methodId = null, argList = null;
        
        while (methodId == null)
            (methodId, nextRest) = Consume(nextRest, this);
        
        while (argList == null)
            (argList, nextRest) = Consume(nextRest, this);
        
        MethodIdentifier = TypedIdentifierPactExpression.Parse(methodId.Backing,this, ExpressionType.MethodIdentifier);
        Arguments = ArgumentListPactExpression.Parse(argList);
        
        var bodyStart = nextRest;

        (var maybeModel, nextRest) = Consume(nextRest, this);
        while (maybeModel == null)
            (maybeModel, nextRest) = Consume(nextRest, this);
        
        if (maybeModel.Type == ExpressionType.Model)
        {
            Model = maybeModel;
            bodyStart = nextRest;
        }
        else if (maybeModel.Type == ExpressionType.StringLiteral)
        {
            Documentation = maybeModel;
            bodyStart = nextRest;
        }

        Body = new BodyPactExpression(bodyStart, this);
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        yield return MethodIdentifier;
        yield return Arguments;
        if (Model != null)
            yield return Model;

        if (Documentation != null)
            yield return Documentation;

        if (Body != null)
            yield return Body;
    }

    public override string ToString()
    {
        return $"[Function \"{MethodIdentifier}\" with {Arguments.Children.Length} arguments and {Body.Children.Length}-expression body]";
    }
}