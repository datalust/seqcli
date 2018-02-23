using Xunit;

namespace SeqCli.Tests.PlainText
{
    public class PatternCompilerTests
    {
        [Fact]
        public void TheMatchingPatternCanExtractDefaultSerilogFileOutput()
        {
            // This is the default format: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            // See: https://github.com/serilog/serilog-sinks-file#controlling-event-formatting

            var pattern = "{@t:timestamp} [{@l:ident}] {@m:*}{:n}{@x:lines}";
        }

        [Fact]
        public void TheMatchingPatternCanExtractDefaultSerilogConsoleOutput()
        {
            // This is the default format: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            // See: https://github.com/serilog/serilog-sinks-console#output-templates

            var pattern = "[{@t:localtime} {@l:ident}] {@m:*}{:n}{@x:lines}";
        }
        
        [Fact]
        public void OptionalSourceContextCanBeExtracted()
        {
            var pattern = "[{@t} {@l:ident}] ({SourceContext:*}) {@m:*}{:n}{@x:lines}";
        }
    }
}