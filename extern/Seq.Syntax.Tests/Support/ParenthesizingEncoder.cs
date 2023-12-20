using Seq.Syntax.Templates.Encoding;

namespace Seq.Syntax.Tests.Support;

public class ParenthesizingEncoder : TemplateOutputEncoder
{
    public override string Encode(string value)
    {
        return $"({value})";
    }
}