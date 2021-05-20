using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Seq.Api.Model;
using SeqCli.Templates.Export;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CollectionNeverQueried.Global

namespace SeqCli.Tests.Templates
{
    class TestEntity : Entity
    {
        public string Name { get; set; }
        public string ReferencedId { get; set; }
        public List<int> Numbers { get; set; }
    }
    
    public class TemplateWriterTests
    {
        [Fact]
        public async Task WritesTemplates()
        {
            var entity = new TestEntity
            {
                Id = "test-stuff",
                Name = "Test Stuff",
                ReferencedId = "test-ref",
                Numbers = new List<int> { 1, 2, 3 }
            };

            const string referencedTemplateName = "Referenced";
            
            var tvm = new TemplateValueMap();
            tvm.AddReferencedTemplate(entity.ReferencedId, referencedTemplateName);
            tvm.MapAsReference<TestEntity>(nameof(TestEntity.ReferencedId));

            var content = new StringWriter();
            await TemplateWriter.WriteTemplateAsync(content, entity, tvm);

            var expected = (await File.ReadAllTextAsync("Templates/test-Expected.template")).Replace("\r\n", "\n");
            var actual = content.ToString().Replace("\r\n", "\n");
            Assert.Equal(expected, actual);
        }
    }
}
