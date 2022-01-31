namespace PactSharp;

public class BodyPactExpression : PactExpression
{
    public PactExpression[] Children { get; set; }
    
    public BodyPactExpression(TextReference backing, PactExpression parent)
    {
        Type = ExpressionType.Body;

        Backing = backing;
        Parent = parent;
        Children = ConsumeAll(backing, this).ToArray();
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        return Children;
    }

    public override string ToString()
    {
        return $"[Expression body with {Children.Length} statements]";
    }
}

public class ListPactExpression : PactExpression
{
    public PactExpression[] Children { get; set; }
    
    public ListPactExpression(TextReference backing, PactExpression parent)
    {
        Type = ExpressionType.ListLiteral;

        var debracedBacking = backing;

        while (debracedBacking.Span.Length > 0 &&
               (char.IsWhiteSpace(debracedBacking.Span[0]) || debracedBacking.Span[0] == '['))
            debracedBacking = debracedBacking.Slice(1);
        
        while (debracedBacking.Span.Length > 0 &&
               (char.IsWhiteSpace(debracedBacking.Span[debracedBacking.Span.Length - 1]) || debracedBacking.Span[debracedBacking.Span.Length - 1] == ']'))
            debracedBacking = debracedBacking.Slice(0, debracedBacking.Span.Length - 1);
        
        
        Backing = backing;
        Parent = parent;
        Children = ConsumeAll(debracedBacking, this).ToArray();
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        return Children;
    }

    public override string ToString()
    {
        return $"[List with {Children.Length} children]";
    }
}