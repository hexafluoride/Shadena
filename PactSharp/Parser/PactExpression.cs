using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace PactSharp;

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
    
    public static (PactExpression, TextReference) ConsumeUntil(TextReference memory,
        Func<PactExpression, bool> predicate, PactExpression parent = null)
    {
        (var expr, var reference) = Consume(memory, parent);

        while (!predicate(expr) && reference.Length > 0)
            (expr, reference) = Consume(reference, parent);

        return (expr, reference);
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
            case ExpressionType.ObjectLiteral:
                expr = ObjectPactExpression.Parse(expr);
                break;
            case ExpressionType.ListLiteral:
                expr = new ListPactExpression(newBacking, parent);
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
        return $"[{Type} expression with contents \"{Contents}\"]";
    }
}