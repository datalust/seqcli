using System.Collections.Generic;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Evaluator;
using SeqCli.Templates.Parser;
using Xunit;

namespace SeqCli.Tests.Templates
{
    public class JsonTemplateEvaluatorTests
    {
        [Fact]
        public void TemplatesAreEvaluated()
        {
            const string template = "42";
            Assert.True(JsonTemplateParser.TryParse(template, out var root, out _, out _));
            
            Assert.True(JsonTemplateEvaluator.TryEvaluate(root, new Dictionary<string, JsonTemplateFunction>(), out var r, out var err));
            Assert.Null(err);
            Assert.True(r is JsonTemplateNumber {Value: 42m});
        }

        [Fact]
        public void FunctionNamesAreResolved()
        {
            const string template = "add_1(42)";
            Assert.True(JsonTemplateParser.TryParse(template, out var root, out _, out _));

            static bool Add1(JsonTemplate[] args, out JsonTemplate r, out string err)
            {
                r = new JsonTemplateNumber(1m + ((JsonTemplateNumber) args[0]).Value);
                err = null;
                return true;
            }
            
            var functions = new Dictionary<string, JsonTemplateFunction>
            {
                ["add_1"] = Add1
            };
            
            Assert.True(JsonTemplateEvaluator.TryEvaluate(root, functions, out var r, out var err));
            Assert.Null(err);
            Assert.True(r is JsonTemplateNumber {Value: 43m});
        }

        [Fact]
        public void MissingFunctionsAreReported()
        {
            const string template = "add_1(42)";
            Assert.True(JsonTemplateParser.TryParse(template, out var root, out _, out _));

            Assert.False(JsonTemplateEvaluator.TryEvaluate(root, new Dictionary<string, JsonTemplateFunction>(), out var r, out var err));
            Assert.Equal("The function name `add_1` was not recognized.", err);
            Assert.Null(r);
        }
    }
}