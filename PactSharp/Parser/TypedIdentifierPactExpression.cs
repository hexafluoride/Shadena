namespace PactSharp;

public class TypedIdentifierPactExpression : PactExpression
{    
    public PactExpression Name { get; set; }
    public PactExpression PactType { get; set; }

    internal TypedIdentifierPactExpression(TextReference nameBacking, ExpressionType nameType, PactExpression parent) : base(nameBacking, parent)
    {
        Type = ExpressionType.TypedIdentifier;
        Name = new PactExpression(nameBacking, this) { Type = nameType };
    }
    
    internal TypedIdentifierPactExpression(TextReference nameBacking, ExpressionType nameType, TextReference typeBacking, PactExpression parent) : this(nameBacking, nameType, parent)
    {
        PactType = new PactExpression(typeBacking, this) { Type = ExpressionType.TypeIdentifier };
    }

    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        yield return Name;
        
        if (PactType != null)
            yield return PactType;
    }

    public static TypedIdentifierPactExpression Parse(TextReference backing, PactExpression parent, ExpressionType identifierType = ExpressionType.Identifier)
    {
        var span = backing.Span;

        var colonIndex = -1;
        for (int i = 0; i < backing.Length; i++)
        {
            if (span[i] == ':')
            {
                colonIndex = i;
                break;
            }
        }

        if (colonIndex == -1)
        {
            return new TypedIdentifierPactExpression(backing, identifierType, parent);
        }
        else
        {
            var nameBacking = backing.Slice(0, colonIndex);
            var typeBacking = backing.Slice(colonIndex + 1);
            return new TypedIdentifierPactExpression(nameBacking, identifierType, typeBacking, parent);
        }
    }

    public override string ToString()
    {
        var typeHuman = Name.Type == ExpressionType.ArgumentIdentifier ? "argument" : Name.Type == ExpressionType.MethodIdentifier ? "method" : Name.Type == ExpressionType.BoundIdentifier ? "bound" : "unknown";

        if (PactType == null)
            return $"[Untyped {typeHuman} identifier \"{Name.Contents}\"]";

        return $"[{typeHuman} identifier \"{Name.Contents}\" with type \"{PactType.Contents}\"]";
    }
}