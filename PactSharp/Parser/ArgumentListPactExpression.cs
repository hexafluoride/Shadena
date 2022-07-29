namespace PactSharp;

public class ArgumentListPactExpression : PactExpression
{    
    public TypedIdentifierPactExpression[] Children { get; set; } = Array.Empty<TypedIdentifierPactExpression>();

    internal ArgumentListPactExpression(PactExpression expr) : base(expr.Backing)
    {
        Parent = expr.Parent;
        Type = ExpressionType.ArgumentList;
    }
    
    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        return Children;
    }

    public static ArgumentListPactExpression Parse(PactExpression root)
    {
        if (root.Type != ExpressionType.CallLike)
            return null;

        var ret = new ArgumentListPactExpression(root);
        var debracedSpan = root.Span.Slice(1, root.Span.Length - 2);
        var debracedMemory = root.Backing.Slice(1, debracedSpan.Length);

        var parts = new List<TypedIdentifierPactExpression>();

        while (debracedSpan.Length > 0)
        {
            int length = 0;
            while (length < debracedSpan.Length && char.IsWhiteSpace(debracedSpan[length]))
            {
                debracedSpan = debracedSpan.Slice(1);
                debracedMemory = debracedMemory.Slice(1);
            }

            while (length < debracedSpan.Length && !char.IsWhiteSpace(debracedSpan[length]))
                length++;

            if (length == 0)
                continue;
                
            parts.Add(TypedIdentifierPactExpression.Parse(debracedMemory.Slice(0, length), ret, ExpressionType.ArgumentIdentifier));

            debracedSpan = debracedSpan.Slice(length);
            debracedMemory = debracedMemory.Slice(length);
        }

        ret.Children = parts.ToArray();
        return ret;
    }

    public override string ToString()
    {
        return $"[{Children.Length} arguments at offset {Backing.Offset}: [{string.Join(", ", Children.Select(c => c.ToString()))}]]";
    }
}