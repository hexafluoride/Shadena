namespace PactSharp;

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
    BoundIdentifier = 0x100000 | Identifier,
    ArgumentList = 0x200000,
    Comment = 0x400000,
    Model = 0x800000,
    Assignment = 0x1000000,
    Binding = 0x2000000,
    LetBlock = 0x4000000
}