using System.Linq;
using Serilog.Core;
using Serilog.Data;
using Serilog.Events;

namespace SeqCli.Output
{
    public class StripStructureTypeEnricher : LogEventPropertyValueRewriter<object>, ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var property in logEvent.Properties)
            {
                var updated = new LogEventProperty(property.Key, Visit(null, property.Value));
                logEvent.AddOrUpdateProperty(updated);
            }
        }

        protected override LogEventPropertyValue VisitStructureValue(object state, StructureValue structure)
        {
            return new StructureValue(structure.Properties.Select(p =>
                new LogEventProperty(p.Name, Visit(null, p.Value))));
        }
    }
}