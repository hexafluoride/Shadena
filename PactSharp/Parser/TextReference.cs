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

    public TextReference Debrace()
    {
        if (Span[0] != '(' || Span[Span.Length - 1] != ')')
            throw new Exception("Asked to debrace invalid backing");

        return Slice(1, Span.Length - 2);
    }
}