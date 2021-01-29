using Superpower.Display;

namespace SeqCli.Templates.Parser
{
    enum JsonTemplateToken
    {
        [Token(Example = "{")]
        LBracket,

        [Token(Example = "}")]
        RBracket,
        
        [Token(Example = "[")]
        LSquareBracket,
        
        [Token(Example = "]")]
        RSquareBracket,
        
        [Token(Example = ":")]
        Colon,
        
        [Token(Example = ",")]
        Comma,
        
        String,
        
        Number,
        
        [Token(Example = "(")]
        LParen,

        [Token(Example = ")")]
        RParen,
        
        Identifier,
    }
}
