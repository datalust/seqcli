using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SeqCli.PlainText;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Xunit;

namespace SeqCli.Tests.PlainText
{
    public class FrameReaderTests
    {
        static TextParser<TextSpan> SpanMatchedBy<T>(TextParser<T> parser)
        {
            return i =>
            {
                var result = parser(i);
                
                if (!result.HasValue)
                    return Result.CastEmpty<T, TextSpan>(result);
              
                return Result.Value(
                    i.Until(result.Remainder),
                    i,
                    result.Remainder);
            };
        }
        
        [Fact]
        public async Task SplitsLinesIntoFrames()
        {
            var source = new StringBuilder();
            source.AppendLine("first");
            source.AppendLine("second");
            
            var reader = new FrameReader(
                new StringReader(source.ToString()),
                SpanMatchedBy(Character.Letter),
                TimeSpan.FromMilliseconds(1));
            
            var first = await reader.TryReadAsync();
            Assert.True(first.HasValue);
            Assert.Equal("first" + Environment.NewLine, first.Value);
            
            var second = await reader.TryReadAsync();
            Assert.True(second.HasValue);
            Assert.Equal("second" + Environment.NewLine, second.Value);

            var empty = await reader.TryReadAsync();
            Assert.False(empty.HasValue);
        }
        
        [Fact]
        public async Task TerminatesWhenNoLinesArePresent()
        {
            var reader = new FrameReader(
                new StringReader(""),
                SpanMatchedBy(Character.Letter),
                TimeSpan.FromMilliseconds(1));
            
            var none = await reader.TryReadAsync();
            Assert.False(none.HasValue);
        }

        [Fact]
        public async Task CollectsTrailingLines()
        {
            var source = new StringBuilder();
            source.AppendLine("first");
            source.AppendLine(" some more");
            source.AppendLine(" and more");
            source.AppendLine("second");
            source.AppendLine("third");
            source.AppendLine(" and yet more");

            var frames = await ReadAllFrames(source.ToString(), SpanMatchedBy(Character.Letter));
            Assert.Equal(3, frames.Length);
            Assert.StartsWith("first", frames[0].Value);
            Assert.EndsWith("and more" + Environment.NewLine, frames[0].Value);
        }

        static async Task<Frame[]> ReadAllFrames(string source, TextParser<TextSpan> frameStart)
        {
            var reader = new FrameReader(
                new StringReader(source),
                frameStart,
                TimeSpan.FromMilliseconds(1));

            var result = new List<Frame>();
            
            var frame = await reader.TryReadAsync();
            while (frame.HasValue)
            {
                result.Add(frame);
                frame = await reader.TryReadAsync();
            }

            return result.ToArray();
        }
    }
}