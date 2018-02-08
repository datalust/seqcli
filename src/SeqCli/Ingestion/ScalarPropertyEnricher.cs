using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Ingestion
{
    class ScalarPropertyEnricher : ILogEventEnricher
    {
        readonly LogEventProperty _property;

        public ScalarPropertyEnricher(string name, object scalarValue)
        {
            _property = new LogEventProperty(name, new ScalarValue(scalarValue));
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(_property);
        }
    }
}
