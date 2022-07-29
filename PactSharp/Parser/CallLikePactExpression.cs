namespace PactSharp;

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
            else if (firstStr == "let" || firstStr == "let*")
            {
                return new LetPactExpression(root);
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
        return $"[Call to \"{First.Contents}\" with {Arguments.Length} args]";
    }
}