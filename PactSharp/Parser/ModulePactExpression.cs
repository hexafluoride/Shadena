namespace PactSharp;

public class ModulePactExpression : BodiedPactExpression
{
    public PactExpression ModuleIdentifier { get; set; }
    public PactExpression Governance { get; set; }
    public PactExpression Model { get; set; }
    
    internal ModulePactExpression(PactExpression root) : base(root)
    {
        Parent = root.Parent;
        Backing = root.Backing;
        Type = ExpressionType.Module;

        var rest = root.Backing.Slice(1, root.Backing.Length - 2);
        rest = rest.Slice("module".Length);
        
        var nextRest = rest;
        while (ModuleIdentifier == null)
            (ModuleIdentifier, nextRest) = Consume(nextRest, this);
        while (Governance == null)
            (Governance, nextRest) = Consume(nextRest, this);

        ModuleIdentifier.Parent = this;
        ModuleIdentifier.Type = ExpressionType.ModuleIdentifier;
        Governance.Parent = this;
        Governance.Type = ExpressionType.CapabilityIdentifier;

        var bodyStart = nextRest;

        (var maybeModel, nextRest) = Consume(nextRest, this);
        while (maybeModel == null)
            (maybeModel, nextRest) = Consume(nextRest, this);
        
        if (maybeModel.Type == ExpressionType.Model)
        {
            Model = maybeModel;
            Model.Parent = this;
            bodyStart = nextRest;
        }

        Body = new BodyPactExpression(bodyStart, this);
    }
    
    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        yield return ModuleIdentifier;
        if (Model != null)
            yield return Model;

        if (Governance != null)
            yield return Governance;

        if (Body != null)
            yield return Body;
    }

    
    public override string ToString()
    {
        return $"[Module \"{ModuleIdentifier.Contents}\" with governance capability \"{Governance.Contents}\" and {Body.Children.Length}-expression body]";
    }
}