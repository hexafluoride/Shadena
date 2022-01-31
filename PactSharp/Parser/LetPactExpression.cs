namespace PactSharp;

public class LetPactExpression : BodiedPactExpression
{
    public AssignmentLikePactExpression[] Bindings { get; set; }
    public bool IsLetStar { get; set; }
    
    internal LetPactExpression(PactExpression root) : base(root)
    {
        Backing = root.Backing;
        Type = ExpressionType.LetBlock;
        Parent = root.Parent;

        var rest = root.Backing.Slice(1, root.Backing.Length - 2);
        rest = rest.Slice("let".Length);
        if ((IsLetStar = (rest.Span[0] == '*')))
            rest = rest.Slice(1);

        (var letAssignmentBlock, var bodyBacking) = PactExpression.Consume(rest, this);

        var letAssignmentDebraced = letAssignmentBlock.Backing.Slice(1, letAssignmentBlock.Backing.Length - 2);

        while (rest.Length > 0 && char.IsWhiteSpace(rest.Span[0]))
            rest = rest.Slice(1);

        if (rest.Span[0] == '(')
            rest = rest.Slice(1);
        
        var bindings = new List<AssignmentLikePactExpression>();
        (var bindingBlock, letAssignmentDebraced) = Consume(letAssignmentDebraced, this);

        do
        {
            while (bindingBlock.Type != ExpressionType.CallLike)
                (bindingBlock, letAssignmentDebraced) = Consume(letAssignmentDebraced, this);

            var bindingCallLike = bindingBlock as CallLikePactExpression;
            var bindingDebraced = bindingCallLike.Backing.Debrace();

            //var bindingConsumed = PactExpression.ConsumeAll(bindingDebraced, this).ToList();

            (var boundIdentifier, bindingDebraced) = ConsumeUntil(bindingDebraced, expr => expr.Type.HasFlag(ExpressionType.Identifier), this);
            (var boundValue, bindingDebraced) = ConsumeUntil(bindingDebraced, expr => expr.Type != ExpressionType.Comment, this);
            
            var bindingCast = new AssignmentLikePactExpression(bindingBlock.Backing,
                TypedIdentifierPactExpression.Parse(boundIdentifier.Backing, this,
                    ExpressionType.BoundIdentifier),
                boundValue, this);

            bindings.Add(bindingCast);

            (bindingBlock, letAssignmentDebraced) = Consume(letAssignmentDebraced, this);
        } while (letAssignmentDebraced.Length > 0);
        //
        // foreach (var binding in (bindingBlock as CallLikePactExpression).Arguments)
        // {
        // }

        Bindings = bindings.ToArray();
        //Bindings = (bindings as CallLikePactExpression).Arguments.Select(arg => AssignmentLikePactExpression)
        
        Body = new BodyPactExpression(bodyBacking, this);
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        foreach (var binding in Bindings)
            yield return binding;

        if (Body != null)
            yield return Body;
    }

    public override string ToString()
    {
        return $"[let block with {Bindings.Length} bound variables and {Body.Children.Length}-long body]";
    }
}