﻿using System.IO;
using Seq.Syntax.Expressions;
using Seq.Syntax.Templates.Compilation;

namespace Seq.Syntax.Templates.Encoding;

class EncodedCompiledTemplate : CompiledTemplate
{
    readonly CompiledTemplate _inner;
    readonly TemplateOutputEncoder _encoder;

    public EncodedCompiledTemplate(CompiledTemplate inner, TemplateOutputEncoder encoder)
    {
        _inner = inner;
        _encoder = encoder;
    }

    public override void Evaluate(EvaluationContext ctx, TextWriter output)
    {
        var buffer = new StringWriter(output.FormatProvider);
        _inner.Evaluate(ctx, buffer);
        var encoded = _encoder.Encode(buffer.ToString());
        output.Write(encoded);
    }
}