using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace PactSharp;

public class PactExpression
{
    public ReadOnlyMemory<char> Backing { get; set; }
    public ReadOnlySpan<char> Span => Backing.Span;
    public string Contents => Backing.ToString();
    
    public ExpressionType Type { get; set; }

    internal PactExpression()
    {
    }

    public PactExpression(ReadOnlyMemory<char> backing)
    {
        Backing = backing;
    }
    public PactExpression(string document, int start, int length)
    {
        Backing = document.AsMemory(start, length);
    }

    public static IEnumerable<PactExpression> ConsumeAll(ReadOnlyMemory<char> memory)
    {
        var expr = default(PactExpression);

        do
        {
            (expr, memory) = PactExpression.Consume(memory);
            if (expr != null && (expr.Type != ExpressionType.Unknown || expr.Backing.Length > 0))
            {
                yield return expr;
            }
        } while (expr != default);
    }

    public static (PactExpression, ReadOnlyMemory<char>) Consume(ReadOnlyMemory<char> memory)
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

        if (patchOutSections.Any())
        {
            var tempStr = newBacking.ToString();
            var removed = 0;
            foreach (var (pStart, pLength) in patchOutSections)
            {
                var offsetStart = pStart - (removed + start);
                //Console.WriteLine($"Patching out {pLength} chars starting with {offsetStart}: \"{tempStr.Substring(offsetStart, pLength)}\"");
                tempStr = tempStr.Remove(offsetStart, pLength);
                removed += pLength;
            }

            newBacking = tempStr.AsMemory();
        }

        //Console.WriteLine($"Consumed {index - start} characters");

        var expr = new PactExpression(newBacking) {Type = currentType};

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

        return (expr, memory.Slice(index));
    }

    public override string ToString()
    {
        return $"[Expression {Type}: {Contents}]";
    }
}

public class TypedIdentifierPactExpression : PactExpression
{    
    public PactExpression Name { get; set; }
    public PactExpression PactType { get; set; }

    internal TypedIdentifierPactExpression(ReadOnlyMemory<char> backing) : base(backing)
    {
        
    }
    
    internal TypedIdentifierPactExpression(PactExpression expr) : base(expr.Backing)
    {
        Type = ExpressionType.TypedIdentifier;
    }

    public static TypedIdentifierPactExpression Parse(ReadOnlyMemory<char> memory, ExpressionType identifierType = ExpressionType.Identifier)
    {
        var str = memory.ToString();
        var parts = str.Split(':');

        var ret = new TypedIdentifierPactExpression(memory)
        {
            Name = new PactExpression(parts[0].AsMemory()) {Type = identifierType}
        };

        if (parts.Length > 1)
            ret.PactType = new PactExpression(string.Join(':', parts.Skip(1)).AsMemory()) {Type = ExpressionType.TypeIdentifier};
        return ret;
    }

    public override string ToString()
    {
        var typeHuman = Name.Type == ExpressionType.ArgumentIdentifier ? "argument" : Name.Type == ExpressionType.MethodIdentifier ? "method" : "unknown";

        if (PactType == null)
            return $"Untyped {typeHuman} identifier \"{Name.Contents}\"";

        return $"{typeHuman} identifier \"{Name.Contents}\" with type {PactType.Contents}";
    }
}

public class ArgumentListPactExpression : PactExpression
{    
    public PactExpression[] Children { get; set; } = Array.Empty<PactExpression>();

    internal ArgumentListPactExpression(PactExpression expr) : base(expr.Backing)
    {
        Type = ExpressionType.ArgumentList;
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
                
            parts.Add(TypedIdentifierPactExpression.Parse(debracedMemory.Slice(0, length), ExpressionType.ArgumentIdentifier));

            debracedSpan = debracedSpan.Slice(length);
            debracedMemory = debracedMemory.Slice(length);
        }

        ret.Children = parts.ToArray();
        return ret;
    }

    public override string ToString()
    {
        return $"[{Children.Length} arguments: [{string.Join(", ", Children.Select(c => c.ToString()))}]]";
    }
}

public class BodyPactExpression : PactExpression
{
    public PactExpression[] Children { get; set; }
    
    internal BodyPactExpression(ReadOnlyMemory<char> backing)
    {
        Type = ExpressionType.Body;

        Backing = backing;
        Children = ConsumeAll(backing).ToArray();
    }

    public override string ToString()
    {
        return $"[Expression body with {Children.Length} statements: [{string.Join(", \n", Children.Select(c => c.ToString()))}]]";
    }
}

public class BodiedPactExpression : PactExpression
{
    public BodyPactExpression Body { get; set; }
    internal BodiedPactExpression(PactExpression root, BodyPactExpression body) : base(root.Backing)
    {
        Body = body;
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

        var rest = root.Backing.Slice(1, root.Backing.Length - 2);
        rest = rest.Slice("defun".Length);
        
        var nextRest = rest;
        PactExpression methodId = null, argList = null;
        
        while (methodId == null)
            (methodId, nextRest) = Consume(nextRest);
        
        while (argList == null)
            (argList, nextRest) = Consume(nextRest);
        
        MethodIdentifier = TypedIdentifierPactExpression.Parse(methodId.Backing, ExpressionType.MethodIdentifier);
        Arguments = ArgumentListPactExpression.Parse(argList);
        
        var bodyStart = nextRest;

        (var maybeModel, nextRest) = Consume(nextRest);
        while (maybeModel == null)
            (maybeModel, nextRest) = Consume(nextRest);
        
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

        Body = new BodyPactExpression(bodyStart);
    }
    public override string ToString()
    {
        return $"[Function {MethodIdentifier} with arguments {Arguments} and body: [{Body}]]";
    }
}

public class ModulePactExpression : BodiedPactExpression
{
    public PactExpression ModuleIdentifier { get; set; }
    public PactExpression Governance { get; set; }
    public PactExpression Model { get; set; }
    
    internal ModulePactExpression(PactExpression root) : base(root)
    {
        Backing = root.Backing;
        Type = ExpressionType.Module;

        var rest = root.Backing.Slice(1, root.Backing.Length - 2);
        rest = rest.Slice("module".Length);
        
        var nextRest = rest;
        while (ModuleIdentifier == null)
            (ModuleIdentifier, nextRest) = Consume(nextRest);
        while (Governance == null)
            (Governance, nextRest) = Consume(nextRest);
        
        ModuleIdentifier.Type = ExpressionType.ModuleIdentifier;
        Governance.Type = ExpressionType.CapabilityIdentifier;

        var bodyStart = nextRest;

        (var maybeModel, nextRest) = Consume(nextRest);
        while (maybeModel == null)
            (maybeModel, nextRest) = Consume(nextRest);
        
        if (maybeModel.Type == ExpressionType.Model)
        {
            Model = maybeModel;
            bodyStart = nextRest;
        }

        Body = new BodyPactExpression(bodyStart);
    }
    public override string ToString()
    {
        return $"[Module {ModuleIdentifier.Contents} with governance capability {Governance.Contents} and body: [{Body}]]";
    }
}

public class CallLikePactExpression : PactExpression
{
    public string First { get; set; }
    public PactExpression[] Arguments { get; set; } = Array.Empty<PactExpression>();
    
    public CallLikePactExpression(string document, int start, int length) : base(document, start, length)
    {
        Type = ExpressionType.CallLike;
        
        if (Backing.Span[0] != '(' || Backing.Span[Backing.Length - 1] != ')')
            throw new InvalidDataException();
    }

    internal CallLikePactExpression(PactExpression expr) : base(expr.Backing)
    {
        Type = ExpressionType.CallLike;
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

        var rest = debracedMemory.Slice(first.Length);

        ret.First = first.ToString();
        if (rest.Length > 1)
        {
            if (ret.First == "module")
            {
                return new ModulePactExpression(root);
            }
            else if (ret.First == "defun")
            {
                return new FunctionPactExpression(root);
            }
            else
            {
                var argsList = PactExpression.ConsumeAll(rest).ToList();
                if (ret.First == "defconst")
                {
                    argsList[0] =
                        TypedIdentifierPactExpression.Parse(argsList[0].Backing, ExpressionType.ConstantIdentifier);
                }

                if (ret.First == "defun" || ret.First == "defcap")
                {
                    if (ret.First == "defun")
                        argsList[0] =
                            TypedIdentifierPactExpression.Parse(argsList[0].Backing, ExpressionType.MethodIdentifier);
                    else
                    {
                        argsList[0] =
                            TypedIdentifierPactExpression.Parse(argsList[0].Backing,
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
        return $"[Call to {First} with args [{string.Join(", ", Arguments.Select(expr => expr.ToString()))}]]";
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