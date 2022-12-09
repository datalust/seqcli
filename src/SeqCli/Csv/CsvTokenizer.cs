using System.Collections.Generic;
using System.Globalization;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.Csv
{
    class CsvTokenizer : Tokenizer<CsvToken>
    {
        static readonly TextParser<TextSpan> Content = Span.WithoutAny(ch => ch == '"');
        
        protected override IEnumerable<Result<CsvToken>> Tokenize(TextSpan span)
        {
            var next = SkipInsignificant(span);
            if (!next.HasValue)
                yield break;

            do
            {
                // Here we're always either at the beginning of a line, or behind a comma.
                if (next.Value == '"')
                {
                    yield return Result.Value(CsvToken.DoubleQuote, next.Location, next.Remainder);
                    next = next.Remainder.ConsumeChar();
                    if (!next.HasValue) yield break;

                    var text = Content(next.Location);
                    while (text.HasValue || !text.Remainder.IsAtEnd)
                    {
                        if (text.HasValue)
                        {
                            if (TryMatchSpecialContent(text.Value, out var specialTokenType) &&
                                !IsEscapedDoubleQuote(text.Remainder))
                                yield return Result.Value(specialTokenType, text.Location, text.Remainder);
                            else
                                yield return Result.Value(CsvToken.Text, text.Location, text.Remainder);
                        }

                        next = text.Remainder.ConsumeChar();
                        if (!next.HasValue) yield break;

                        if (next.Value != '"')
                        {
                            yield return Result.Empty<CsvToken>(next.Location, new[] {"double-quote"});
                            yield break;
                        }

                        var lookahead = next.Remainder.ConsumeChar();
                        if (lookahead.HasValue && lookahead.Value == '"')
                        {
                            yield return Result.Value(CsvToken.EscapedDoubleQuote, next.Location, lookahead.Remainder);
                            next = lookahead.Remainder.ConsumeChar();
                            if (!next.HasValue) yield break;
                        }
                        else
                        {
                            yield return Result.Value(CsvToken.DoubleQuote, next.Location, next.Remainder);
                            next = next.Remainder.ConsumeChar();
                            if (!next.HasValue) yield break;
                            break; // Done with the content
                        }
                        
                        text = Content(next.Location);
                    }
                    
                    next = SkipInsignificant(next.Location);
                    if (next.Value == ',')
                    {                        
                        yield return Result.Value(CsvToken.Comma, next.Location, next.Remainder);
                        next = next.Remainder.ConsumeChar();
                        if (!next.HasValue) yield break;
                    }
                    else if (next.Value == '\n')
                    {
                        yield return Result.Value(CsvToken.Newline, next.Location, next.Remainder);
                        next = next.Remainder.ConsumeChar();
                        if (!next.HasValue) yield break;
                    }
                    else
                    {
                        yield return Result.Empty<CsvToken>(next.Location, new[] {"comma", "newline"});
                        yield break;
                    }
                }
                else
                {
                    yield return Result.Empty<CsvToken>(next.Location, new[] {"double-quote"});
                    yield break;
                }
                
                next = SkipInsignificant(next.Location);
            } while (next.HasValue);
        }

        static bool IsEscapedDoubleQuote(TextSpan span)
        {
            return span.Length >= 2 &&
                   span.Source![span.Position.Absolute] == '"' &&
                   span.Source[span.Position.Absolute + 1] == '"';
        }

        static bool TryMatchSpecialContent(TextSpan text, out CsvToken specialTokenType)
        {       
            // Planning a switch from "True" to "true" for CSV Booleans
            if (text.EqualsValueIgnoreCase("true") ||
                text.EqualsValueIgnoreCase("false"))
            {
                specialTokenType = CsvToken.Boolean;
                return true;
            }
            
            if (text.EqualsValue("null"))
            {
                specialTokenType = CsvToken.Null;
                return true;
            }

            // Just a quick temp job here until Superpower `Numerics` gets `Decimal` and `HexNumber`, plus
            // an `IsMatch(TextSpan)` on `TextParser`.
            var s = text.ToStringValue();
            if (text.Length > 0 
                && text.Length < 16
                && (decimal.TryParse(text.ToStringValue(), out _) ||
                    s.StartsWith("0x") && ulong.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)))
            {
                specialTokenType = CsvToken.Number;
                return true;
            }
            
            specialTokenType = CsvToken.None;
            return false;
        }

        static Result<char> SkipInsignificant(TextSpan span)
        {
            var result = span.ConsumeChar();
            while (result.HasValue && result.Value != '\n' && char.IsWhiteSpace(result.Value))
                result = result.Remainder.ConsumeChar();
            return result;
        }
    }
}