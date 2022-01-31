namespace PactSharp;

public class AssignmentLikePactExpression : PactExpression
{
    public PactExpression Left { get; set; }
    public PactExpression Right { get; set; }
    
    internal AssignmentLikePactExpression(TextReference backing, PactExpression left, PactExpression right, PactExpression parent) : base(backing, parent)
    {
        Left = left;
        Left.Parent = this;
        Right = right;
        Right.Parent = this;
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        yield return Left;
        yield return Right;
    }

    public override string ToString()
    {
        return $"[Assignment of {Right} to {Left}]";
    }
}

public class ObjectPactExpression : PactExpression
{    
    public AssignmentLikePactExpression[] Children { get; set; } = Array.Empty<AssignmentLikePactExpression>();

    internal ObjectPactExpression(PactExpression expr) : base(expr.Backing)
    {
        Parent = expr.Parent;
        Type = ExpressionType.ObjectLiteral;
    }
    
    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        return Children;
    }

    public static ObjectPactExpression Parse(PactExpression root)
    {
        if (root.Type != ExpressionType.ObjectLiteral)
            return null;

        var ret = new ObjectPactExpression(root);
        var debracedSpan = root.Span.Slice(1, root.Span.Length - 2);
        var debracedMemory = root.Backing.Slice(1, debracedSpan.Length);

        var parts = new List<AssignmentLikePactExpression>();

        var allConsumed = new Queue<PactExpression>(PactExpression.ConsumeAll(debracedMemory, ret));

        PactExpression PopNext()
        {
            PactExpression popped = default;

            while (allConsumed.Any() && (popped == default || popped.Type == ExpressionType.Comment || popped.Contents == ","))
                popped = allConsumed.Dequeue();

            if (popped == default || popped.Type == ExpressionType.Comment)
                return null;

            return popped;
        }

        while (allConsumed.Any())
        {
            var identifier = PopNext();
            var inbetween = PopNext();
            var assigned = PopNext();

            if (identifier == null || inbetween == null || assigned == null)
                break;
            
            bool isBinding = inbetween.Contents == ":=";

            if (identifier.Type != ExpressionType.AtomLiteral && identifier.Type != ExpressionType.StringLiteral)
                throw new Exception();
            
            var entireReference = debracedMemory.Slice(identifier.Backing.Offset - debracedMemory.Offset, (assigned.Backing.Offset + assigned.Backing.Length) - identifier.Backing.Offset);
            var assignment = new AssignmentLikePactExpression(entireReference, identifier, assigned, ret);

            if (isBinding)
                assignment.Type = ExpressionType.Binding;
            else
                assignment.Type = ExpressionType.Assignment;
            
            parts.Add(assignment);
        }
        
        // while (debracedSpan.Length > 0)
        // {
        //     bool isBinding = false;
        //     
        //     while (debracedSpan.Length > 0 && (char.IsWhiteSpace(debracedSpan[0]) || debracedSpan[0] == ','))
        //     {
        //         debracedSpan = debracedSpan.Slice(1);
        //         debracedMemory = debracedMemory.Slice(1);
        //     }
        //
        //     if (debracedSpan.Length == 0)
        //         break;
        //
        //     var startingReference = debracedMemory.Slice(0);
        //     
        //     (var maybeKey, debracedMemory) = Consume(debracedMemory, ret);
        //     if (maybeKey.Type != ExpressionType.AtomLiteral && maybeKey.Type != ExpressionType.StringLiteral)
        //         throw new Exception();
        //
        //     debracedSpan = debracedMemory.Span;
        //     
        //     while (debracedSpan.Length > 0 && (char.IsWhiteSpace(debracedSpan[0]) || debracedSpan[0] == ':' || debracedSpan[0] == '='))
        //     {
        //         if (debracedSpan[0] == '=')
        //             isBinding = true;
        //         
        //         debracedSpan = debracedSpan.Slice(1);
        //         debracedMemory = debracedMemory.Slice(1);
        //     }
        //
        //     if (debracedSpan.Length == 0)
        //         break;
        //     
        //     (var maybeValue, debracedMemory) = Consume(debracedMemory, ret);
        //     debracedSpan = debracedMemory.Span;
        //
        //     var entireReference = startingReference.Slice(0, debracedMemory.Offset - startingReference.Offset);
        //     var assignment = new AssignmentLikePactExpression(entireReference, maybeKey, maybeValue, ret);
        //
        //     if (isBinding)
        //         assignment.Type = ExpressionType.Binding;
        //     else
        //         assignment.Type = ExpressionType.Assignment;
        //     
        //     parts.Add(assignment);
        //
        //     //
        //     // while (length < debracedSpan.Length && !char.IsWhiteSpace(debracedSpan[length]))
        //     //     length++;
        //     //
        //     // if (length == 0)
        //     //     continue;
        //     //     
        //     // parts.Add(TypedIdentifierPactExpression.Parse(debracedMemory.Slice(0, length), ret, ExpressionType.ArgumentIdentifier));
        //     //
        //     // debracedSpan = debracedSpan.Slice(length);
        //     // debracedMemory = debracedMemory.Slice(length);
        // }

        ret.Children = parts.ToArray();
        return ret;
    }

    public override string ToString()
    {
        return $"[Object with {Children.Length} properties]";
    }
}