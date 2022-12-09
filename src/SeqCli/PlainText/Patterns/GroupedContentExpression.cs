using System;

namespace SeqCli.PlainText.Patterns;

class GroupedContentExpression : CaptureContentExpression
{
    public ExtractionPattern ExtractionPattern { get; }

    public GroupedContentExpression(ExtractionPattern extractionPattern)
    {
        ExtractionPattern = extractionPattern ?? throw new ArgumentNullException(nameof(extractionPattern));
    }
}