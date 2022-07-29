using System.Text.Json.Serialization;

namespace PactSharp;


public struct RangeContext
{
    public int Start { get; set; }
    public int End { get; set; }
    public PactExpression[] Tokens { get; set; }
}

public class RangeMap
{
    public PactExpression[] ExpressionIndex { get; set; }
    public TextReference Backing { get; set; }
    public List<RangeContext> Ranges { get; set; } = new();
    public List<Symbol> AvailableSymbols { get; set; } = new();

    public RangeMap(TextReference backing)
    {
        Backing = backing;
    }

    public RangeContext? GetContext(int position)
    {
        foreach (var range in Ranges)
        {
            if (range.End < position)
                continue;

            if (range.Start > position)
                break;

            if (range.Start <= position && position <= range.End)
                return range;
        }

        return null;
    }

    public void Index(PactExpression root)
    {
        Ranges.Clear();
        
        var tokens = root.TraverseTree().ToArray();
        ExpressionIndex = tokens;

        for (int i = 0; i < tokens.Length; i++)
            tokens[i].Index = i;

        foreach (var token in tokens)
        {
            if (token.EnumerateChildren().Any())
                continue;
            
            // token is leaf
            var leafStart = token.Backing.Offset;
            var leafEnd = leafStart + token.Backing.Length;
            var stack = token.TraverseParents().ToArray();
            
            Ranges.Add(new RangeContext()
            {
                Start = leafStart,
                End = leafEnd,
                Tokens = stack
            });
        }
        
        Ranges = Ranges.OrderBy(r => r.Start).ToList();

        foreach (var token in tokens)
        {
            Symbol symbol = new Symbol();
            switch (token)
            {
                case FunctionPactExpression functionToken:
                    var parentModule = functionToken.TraverseParents().OfType<ModulePactExpression>().First();
                    
                    symbol = new Symbol()
                    {
                        Type = SymbolType.Function,
                        Backing = token.Backing.Slice(0),
                        Description = functionToken.Documentation?.Contents,
                        Name = functionToken.MethodIdentifier.Name.Contents,
                        Scope = new Scope() { Token = parentModule.Index }
                    };

                    symbol.FullyQualifiedName =
                        $"{parentModule.ModuleIdentifier.Contents}.{symbol.Name}";
                    AvailableSymbols.Add(symbol);
                    break;
                case ArgumentListPactExpression argumentList:
                    var parentFunction = argumentList.Parent;

                    foreach (var argument in argumentList.Children)
                    {
                        var argumentSymbol = new Symbol()
                        {
                            Type = SymbolType.Variable,
                            Backing = argument.Backing.Slice(0),
                            Name = argument.Name.Contents,
                            Scope = new Scope() {Token = parentFunction.Index}
                        };

                        argumentSymbol.FullyQualifiedName =
                            $"function_{parentFunction.Index}${argumentSymbol.Name}";
                        AvailableSymbols.Add(argumentSymbol);
                    }
                    break;
                case LetPactExpression letBlock:
                    foreach (var bound in letBlock.Bindings)
                    {
                        var bindingSymbol = new Symbol()
                        {
                            Type = SymbolType.Variable,
                            Backing = bound.Backing.Slice(0),
                            Name = (bound.Left as TypedIdentifierPactExpression).Name.Contents,
                            Scope = new Scope() {Token = letBlock.Index}
                        };

                        bindingSymbol.FullyQualifiedName =
                            $"let_{letBlock.Parent.Index}${bindingSymbol.Name}";
                        AvailableSymbols.Add(bindingSymbol);
                    }
                    break;
            }
        }
    }
}

public class Symbol
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SymbolType Type { get; set; }
    public string Name { get; set; }
    public string FullyQualifiedName { get; set; }
    public Scope Scope { get; set; }
    public string Description { get; set; }
    public string PactType { get; set; }
    
    [JsonIgnore]
    public TextReference Backing { get; set; }
}

public class Scope
{
    public int Token { get; set; }
}

public enum SymbolType
{
    Unknown = 0,
    Module = 1,
    Function = 2,
    Capability = 3,
    Type = 4,
    Variable = 5
}