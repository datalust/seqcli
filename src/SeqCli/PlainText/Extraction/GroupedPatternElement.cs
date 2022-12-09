using System;
using System.Collections.Generic;
using System.Linq;
using Superpower;
using Superpower.Model;

namespace SeqCli.PlainText.Extraction;

class GroupedPatternElement : PatternElement
{
    readonly PatternElement[] _content;

    public GroupedPatternElement(IEnumerable<PatternElement> content, string? name = null)
        : base(name)
    {
        _content = content?.ToArray() ?? throw new ArgumentNullException(nameof(content));
        if (_content.Length == 0) throw new ArgumentException("A grouped pattern must include at least one element.");

        Match = _content.Select(c => c.Match).Aggregate((a, b) => a.IgnoreThen(b));            
    }

    public override TextParser<Unit> Match { get; }
        
    public override bool TryExtract(
        TextSpan input, 
        Dictionary<string, object?> result,
        out TextSpan remainder)
    {
        var temp = new Dictionary<string, object?>();

        var rem = input;
        foreach (var element in _content)
        {
            if (!element.TryExtract(rem, temp, out rem))
            {
                remainder = input;
                return false;
            }
        }

        foreach (var pair in temp)
            result.Add(pair.Key, pair.Value);

        var value = input.Until(rem);
        remainder = rem;
        CollectResult(result, value);

        return true;
    }
}