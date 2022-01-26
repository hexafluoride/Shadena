using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace PactSharp;

public readonly struct TextReference
{
    public ReadOnlySpan<char> Span => Backing.Span;
    public int Length => Backing.Length;
    public ReadOnlyMemory<char> Backing { get; }
    public int Offset { get; }
    public TextReference(ReadOnlyMemory<char> backing)
    {
        Backing = backing;
        Offset = 0;
    }
    
    public TextReference(ReadOnlyMemory<char> newBacking, int offset)
    {
        Backing = newBacking;
        Offset = offset;
    }
    
    public TextReference Slice(int start, int length)
    {
        return new TextReference(Backing.Slice(start, length), Offset + start);
    }

    public TextReference Slice(int start)
    {
        return new TextReference(Backing.Slice(start), Offset + start);
    }
}

public class PactExpression
{
    public int Index { get; set; }
    public TextReference Backing { get; set; }
    public ReadOnlySpan<char> Span => Backing.Span;
    public string Contents => Span.ToString();
    public PactExpression Parent { get; set; }
    public ExpressionType Type { get; set; }

    internal PactExpression()
    {
    }

    public PactExpression(TextReference reference)
    {
        Backing = reference;
    }

    public PactExpression(TextReference reference, PactExpression parent) : this(reference)
    {
        Parent = parent;
    }

    public virtual IEnumerable<PactExpression> EnumerateChildren()
    {
        return Enumerable.Empty<PactExpression>();
    }

    public static IEnumerable<PactExpression> ConsumeAll(TextReference reference, PactExpression parent = null)
    {
        var expr = default(PactExpression);
        var backing = reference;
        //var backing = new TextReference(memory);

        do
        {
            (expr, backing) = PactExpression.Consume(backing, parent);
            if (expr != null && (expr.Type != ExpressionType.Unknown || expr.Backing.Length > 0))
            {
                yield return expr;
            }
        } while (expr != default);
    }

    public IEnumerable<PactExpression> TraverseTree()
    {
        var toVisit = new List<PactExpression>() {this};

        while (toVisit.Any())
        {
            var expr = toVisit[0];
            toVisit.RemoveAt(0);

            yield return expr;
            
            //Console.Write($"Visiting expression at {expr.Backing.Offset}: ");

            foreach (var child in expr.EnumerateChildren())
            {
                toVisit.Add(child);
                //Console.Write($"{child.Backing.Offset}, ");
                //yield return child;
            }

            //Console.WriteLine();
        }
    }

    public IEnumerable<PactExpression> EnumerateLeaves()
    {
        var toVisit = new List<PactExpression>() {this};

        while (toVisit.Any())
        {
            var expr = toVisit[0];
            toVisit.RemoveAt(0);

            var children = expr.EnumerateChildren().ToList();
            if (!children.Any())
                yield return expr;

            toVisit.AddRange(children);
        }
    }

    public IEnumerable<PactExpression> TraverseParents()
    {
        var node = this;
        yield return node;

        while (node.Parent != null)
        {
            node = node.Parent;
            yield return node;
        }
    }

    public static (PactExpression, TextReference) Consume(TextReference memory, PactExpression parent = null)
    {
        if (memory.Length == 0)
            return (null, memory);
        
        var span = memory.Span;
        ExpressionType currentType = ExpressionType.Unknown;

        int start = 0;
        int index = 0;
        int depth = 0;
        char depthIncrease = '\0', depthDecrease = '\0';
        bool trackingDepth = false;
        bool ignoringString = false;
        bool ignoringComment = false;
        bool escapingSpecial = false;
        bool escapingMultiline = false;

        var patchOutSections = new List<(int, int)>();

        int patchOutStart = 0;
        int patchOutLength = 0;

        for (; index < span.Length; index++)
        {
            var currentChar = span[index];

            if (escapingSpecial)
            {
                patchOutLength++;
                switch (currentChar)
                {
                    case '\n':
                        escapingMultiline = true;
                        escapingSpecial = false;
                        break;
                    default:
                        escapingSpecial = false;
                        patchOutSections.Add((patchOutStart, patchOutLength));
                        patchOutLength = 0;
                        break;
                }
                
                continue;
            }

            if (escapingMultiline)
            {
                patchOutLength++;
                if (currentChar != '\\')
                    continue;
                else
                {
                    escapingMultiline = false;
                    patchOutSections.Add((patchOutStart, patchOutLength + 1));
                    patchOutLength = 0;
                    continue;
                }
            }

            if (trackingDepth && !ignoringString)
            {
                if (currentChar == depthIncrease)
                    depth++;
                if (currentChar == depthDecrease)
                    depth--;

                if (depth == 0)
                {
                    index++;
                    break;
                }
            }

            bool breakNow = false;

            switch (currentType)
            {
                case ExpressionType.StringLiteral:
                    if (currentChar == '"')
                    {
                        index++;
                        breakNow = true;
                    }

                    if (currentChar == '\\')
                    {
                        patchOutStart = index;
                        patchOutLength = 1;
                        escapingSpecial = true;
                    }
                    break;
                case ExpressionType.AtomLiteral:
                    if (char.IsWhiteSpace(currentChar) || 
                        (currentChar != '-' && char.IsPunctuation(currentChar)))
                        breakNow = true;
                    break;
                case ExpressionType.Identifier:
                case ExpressionType.NumericLiteral:
                case ExpressionType.BooleanLiteral:
                case ExpressionType.Literal:
                    if (char.IsWhiteSpace(currentChar))
                        breakNow = true;
                    break;
                case ExpressionType.Body:
                case ExpressionType.ObjectLiteral:
                case ExpressionType.CallLike:
                    if (currentChar == '"')
                        ignoringString = !ignoringString;
                    if (currentChar == ';' && !ignoringString)
                        ignoringComment = true;
                    break;
                case ExpressionType.Comment:
                    if (currentChar == '\n')
                        breakNow = true;
                    break;
                case ExpressionType.Unknown:
                    switch (currentChar)
                    {
                        case '(':
                            currentType = ExpressionType.CallLike;
                            depthIncrease = '('; depthDecrease = ')';
                            depth++;
                            trackingDepth = true;
                            break;
                        case '\"':
                            currentType = ExpressionType.StringLiteral;
                            trackingDepth = false;
                            break;
                        case '\'':
                            currentType = ExpressionType.AtomLiteral;
                            break;
                        case '-':
                            currentType = ExpressionType.NumericLiteral;
                            break;
                        case '{':
                            currentType = ExpressionType.ObjectLiteral;
                            depthIncrease = '{'; depthDecrease = '}';
                            depth++;
                            trackingDepth = true;
                            break;
                        case '[':
                            currentType = ExpressionType.ListLiteral;
                            depthIncrease = '['; depthDecrease = ']';
                            depth++;
                            trackingDepth = true;
                            break;
                        case ';':
                            currentType = ExpressionType.Comment;
                            ignoringComment = true;
                            break;
                        default:
                            if (char.IsDigit(currentChar))
                                currentType = ExpressionType.NumericLiteral;
                            else if (char.IsWhiteSpace(currentChar))
                            {
                                start++;
                                break;
                            }
                            else
                                currentType = ExpressionType.Identifier;
                            break;
                    }
                    break;
            }

            if (breakNow)
                break;
        }

        if (patchOutLength > 0)
        {
            patchOutSections.Add((patchOutStart, patchOutLength));
            patchOutLength = 0;
        }

        var newBacking = memory.Slice(start, index - start);
        int followingOffset = newBacking.Offset + newBacking.Length;

        if (patchOutSections.Any())
        {
            var tempStr = span.Slice(start, index - start).ToString();
            var removed = 0;
            foreach (var (pStart, pLength) in patchOutSections)
            {
                var offsetStart = pStart - (removed + start);
                //Console.WriteLine($"Patching out {pLength} chars starting with {offsetStart}: \"{tempStr.Substring(offsetStart, pLength)}\"");
                tempStr = tempStr.Remove(offsetStart, pLength);
                removed += pLength;
            }

            newBacking = new TextReference(tempStr.AsMemory(), memory.Offset);
        }

        var expr = new PactExpression(newBacking, parent) {Type = currentType};

        switch (currentType)
        {
            case ExpressionType.CallLike:
                expr = CallLikePactExpression.Parse(expr);
                break;
            case ExpressionType.Identifier:
                if ((expr.Backing.Length == 4 || expr.Backing.Length == 5) &&
                    (expr.Contents.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
                    expr.Contents.Equals("false", StringComparison.InvariantCultureIgnoreCase)))
                    expr.Type = ExpressionType.BooleanLiteral;
                break;
        }

        var returnMemory = new TextReference(memory.Backing.Slice(index), followingOffset);
        return (expr, returnMemory);
    }

    public override string ToString()
    {
        return $"[Expression {Type} at offset {Backing.Offset}: {Contents}]";
    }
}

public class TypedIdentifierPactExpression : PactExpression
{    
    public PactExpression Name { get; set; }
    public PactExpression PactType { get; set; }

    internal TypedIdentifierPactExpression(TextReference nameBacking, PactExpression parent) : base(nameBacking, parent)
    {
        Type = ExpressionType.TypedIdentifier;
        Name = new PactExpression(nameBacking, this);
    }
    
    internal TypedIdentifierPactExpression(TextReference nameBacking, TextReference typeBacking, PactExpression parent) : this(nameBacking, parent)
    {
        PactType = new PactExpression(typeBacking, this);
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
            return new TypedIdentifierPactExpression(backing, parent);
        }
        else
        {
            var nameBacking = backing.Slice(0, colonIndex);
            var typeBacking = backing.Slice(colonIndex + 1);
            return new TypedIdentifierPactExpression(nameBacking, typeBacking, parent);
        }
    }

    public override string ToString()
    {
        var typeHuman = Name.Type == ExpressionType.ArgumentIdentifier ? "argument" : Name.Type == ExpressionType.MethodIdentifier ? "method" : "unknown";

        if (PactType == null)
            return $"Untyped {typeHuman} identifier \"{Name.Contents}\"";

        return $"{typeHuman} identifier \"{Name.Contents}\" at offset {Backing.Offset} with type {PactType.Contents}";
    }
}

public class ArgumentListPactExpression : PactExpression
{    
    public PactExpression[] Children { get; set; } = Array.Empty<PactExpression>();

    internal ArgumentListPactExpression(PactExpression expr) : base(expr.Backing)
    {
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

        var parts = new List<PactExpression>();

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
        return $"[Expression body at offset {Backing.Offset} with {Children.Length} statements: [{string.Join(", \n", Children.Select(c => c.ToString()))}]]";
    }
}

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
        return $"[Function at offset {Backing.Offset} {MethodIdentifier} with arguments {Arguments} and body: [{Body}]]";
    }
}

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
        return $"[Module {ModuleIdentifier.Contents} at offset {Backing.Offset} with governance capability {Governance.Contents} and body: [{Body}]]";
    }
}

public class CallLikePactExpression : PactExpression
{
    public PactExpression First { get; set; }
    public PactExpression[] Arguments { get; set; } = Array.Empty<PactExpression>();
    
    internal CallLikePactExpression(PactExpression expr) : base(expr.Backing)
    {
        Parent = expr.Parent;
        Type = ExpressionType.CallLike;
    }
    
    public override IEnumerable<PactExpression> EnumerateChildren()
    {
        yield return First;
        if (Arguments != null)
            foreach (var arg in Arguments)
                yield return arg;
    }

    public static PactExpression Parse(PactExpression root)
    {
        if (root.Type != ExpressionType.CallLike)
            return null;

        var ret = new CallLikePactExpression(root);
        var debracedSpan = root.Span.Slice(1, root.Span.Length - 2);
        var debracedMemory = root.Backing.Slice(1, debracedSpan.Length);
        
        var first = debracedSpan;

        for (int index = 0; index < first.Length; index++)
        {
            var currentChar = first[index];

            if (char.IsWhiteSpace(currentChar))
            {
                first = first.Slice(0, index);
                break;
            }
        }

        var firstRef = debracedMemory.Slice(0, first.Length);
        var firstStr = firstRef.Span.ToString();
        var rest = debracedMemory.Slice(first.Length);

        ret.First = new PactExpression(firstRef, ret) { Type = ExpressionType.Identifier };
        if (rest.Length > 1)
        {
            if (firstStr == "module")
            {
                return new ModulePactExpression(root);
            }
            else if (firstStr == "defun")
            {
                return new FunctionPactExpression(root);
            }
            else
            {
                var argsList = PactExpression.ConsumeAll(rest, ret).ToList();
                if (firstStr == "defconst")
                {
                    argsList[0] =
                        TypedIdentifierPactExpression.Parse(argsList[0].Backing, ret, ExpressionType.ConstantIdentifier);
                }

                if (firstStr == "defun" || firstStr == "defcap")
                {
                    if (firstStr == "defun")
                        argsList[0] =
                            TypedIdentifierPactExpression.Parse(argsList[0].Backing, ret, ExpressionType.MethodIdentifier);
                    else
                    {
                        argsList[0] =
                            TypedIdentifierPactExpression.Parse(argsList[0].Backing, ret,
                                ExpressionType.CapabilityIdentifier);
                    }

                    argsList[1] = ArgumentListPactExpression.Parse(argsList[1]);

                    // if (argsList.Count >= 2)
                    // {
                    //     var additionalBody = argsList.Skip(2).ToArray();
                    //     argsList.RemoveRange(2, additionalBody.Length);
                    //     argsList.Add(new BodyPactExpression(additionalBody));
                    // }
                }

                ret.Arguments = argsList.ToArray();
            }
        }

        return ret;
    }
    
    public override string ToString()
    {
        return $"[Call at offset {Backing.Offset} to {First} with args [{string.Join(", ", Arguments.Select(expr => expr.ToString()))}]]";
    }
}

public enum ExpressionType
{
    Unknown,
    Literal = 0x1,
    StringLiteral = 0x2 | Literal,
    AtomLiteral = 0x4 | Literal,
    NumericLiteral = 0x8 | Literal,
    BooleanLiteral = 0x10 | Literal,
    ObjectLiteral = 0x20 | Literal,
    ListLiteral = 0x40 | Literal,
    Body = 0x80,
    CallLike = 0x100,
    Module = 0x200,
    Function = 0x400,
    Capability = 0x800,
    Identifier = 0x1000,
    TypedIdentifier = 0x2000 | Identifier,
    TypeIdentifier = 0x4000 | Identifier,
    ArgumentIdentifier = 0x8000 | Identifier,
    MethodIdentifier = 0x10000 | Identifier,
    ConstantIdentifier = 0x20000 | Identifier,
    CapabilityIdentifier = 0x40000 | Identifier,
    ModuleIdentifier = 0x80000 | Identifier,
    ArgumentList = 0x100000,
    Comment = 0x200000,
    Model = 0x400000
}